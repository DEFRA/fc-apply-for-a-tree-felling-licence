using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using LinqKit;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// An implementation of <see cref="IUpdateConfirmedFellingAndRestockingDetailsService"/> that updates confirmed felling/restocking details
/// using an <see cref="IFellingLicenceApplicationInternalRepository"/>
/// instance.
/// </summary>
public class UpdateConfirmedFellingAndRestockingDetailsService : IUpdateConfirmedFellingAndRestockingDetailsService
{
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository;
    private readonly ILogger<UpdateConfirmedFellingAndRestockingDetailsService> _logger;
    private readonly IAuditService<UpdateConfirmedFellingAndRestockingDetailsService> _audit;
    private readonly RequestContext _requestContext;

    public UpdateConfirmedFellingAndRestockingDetailsService(
        IFellingLicenceApplicationInternalRepository internalFlaRepository,
        ILogger<UpdateConfirmedFellingAndRestockingDetailsService> logger,
        IAuditService<UpdateConfirmedFellingAndRestockingDetailsService> audit,
        RequestContext requestContext)
    {
        _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
        _logger = Guard.Against.Null(logger);
        _audit = Guard.Against.Null(audit);
        _requestContext = Guard.Against.Null(requestContext);
    }

    /// <inheritdoc/>
    public async Task<Result<CombinedConfirmedFellingAndRestockingDetailRecord>> RetrieveConfirmedFellingAndRestockingDetailModelAsync(
    Guid applicationId,
    CancellationToken cancellationToken)
    {
        var applicationMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);

        if (applicationMaybe.HasNoValue)
        {
            _logger.LogError("Unable to retrieve felling licence application {appId}", applicationId);

            return Result.Failure<CombinedConfirmedFellingAndRestockingDetailRecord>(
                $"Unable to retrieve felling licence application {applicationId}");
        }

        var application = applicationMaybe.Value;

        // Check if there are no ConfirmedFellingDetails in all compartments
        var compartments = application.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments;
        if (compartments == null || !compartments.Any() || compartments.All(c => c.ConfirmedFellingDetails == null || !c.ConfirmedFellingDetails.Any()))
        {
            _logger.LogError("No confirmed felling details found for application {appId}", applicationId);
            return Result.Failure<CombinedConfirmedFellingAndRestockingDetailRecord>(
                $"No confirmed felling details found");
        }

        var woodlandOfficerReviewMaybe = await _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);
        var compartmentNames = application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments
            .ToDictionary(x => x.CompartmentId, x => x.CompartmentNumber);

        var compartmentList =
            (from compartment in application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
             select new ConfirmedFellingAndRestockingDetailModel
             {
                 CompartmentId = compartment.CompartmentId,
                 TotalHectares = compartment.TotalHectares,
                 ConfirmedTotalHectares = compartment.ConfirmedTotalHectares,
                 CompartmentNumber = compartment.CompartmentNumber,
                 SubCompartmentName = compartment.SubCompartmentName,

                 ConfirmedFellingDetailModels = compartment.ConfirmedFellingDetails.Select(fellingDetails =>
                    new ConfirmedFellingDetailModel
                    {
                        ConfirmedFellingDetailsId = fellingDetails.Id,
                        ProposedFellingDetailsId = fellingDetails.ProposedFellingDetailId,
                        AreaToBeFelled = fellingDetails.AreaToBeFelled,
                        IsPartOfTreePreservationOrder = fellingDetails.IsPartOfTreePreservationOrder,
                        TreePreservationOrderReference = fellingDetails.TreePreservationOrderReference,
                        IsWithinConservationArea = fellingDetails.IsWithinConservationArea,
                        ConservationAreaReference = fellingDetails.ConservationAreaReference,
                        NumberOfTrees = fellingDetails.NumberOfTrees,
                        OperationType = fellingDetails.OperationType,
                        IsTreeMarkingUsed = string.IsNullOrEmpty(fellingDetails.TreeMarking) ? false : true,
                        TreeMarking = fellingDetails.TreeMarking,
                        EstimatedTotalFellingVolume = fellingDetails.EstimatedTotalFellingVolume,
                        ConfirmedFellingSpecies = fellingDetails.ConfirmedFellingSpecies,
                        IsRestocking = fellingDetails.IsRestocking,
                        NoRestockingReason = fellingDetails.NoRestockingReason,
                        AmendedProperties =
                            GetAmendedFellingDetailProperties(
                                application.LinkedPropertyProfile!.ProposedFellingDetails!
                                    .FirstOrDefault(x => x.Id == fellingDetails.ProposedFellingDetailId),
                                fellingDetails),
                        ConfirmedRestockingDetailModels = fellingDetails.ConfirmedRestockingDetails.Select(restockingDetails =>
                        new ConfirmedRestockingDetailModel
                        {
                            ConfirmedRestockingDetailsId = restockingDetails.Id,
                            Area = restockingDetails.Area,
                            ConfirmedRestockingSpecies = restockingDetails.ConfirmedRestockingSpecies,
                            PercentageOfRestockArea = restockingDetails.PercentageOfRestockArea,
                            PercentNaturalRegeneration = restockingDetails.PercentNaturalRegeneration,
                            PercentOpenSpace = restockingDetails.PercentOpenSpace,
                            RestockingDensity = restockingDetails.RestockingDensity,
                            NumberOfTrees = restockingDetails.NumberOfTrees,
                            RestockingProposal = restockingDetails.RestockingProposal,
                            CompartmentId = restockingDetails.SubmittedFlaPropertyCompartmentId,
                            AmendedProperties =
                                GetAmendedRestockingProperties(
                                    application.LinkedPropertyProfile.ProposedFellingDetails
                                        .SelectMany(x => x.ProposedRestockingDetails)
                                        .FirstOrDefault(x => x.Id == restockingDetails.ProposedRestockingDetailId), restockingDetails, compartmentNames),
                            CompartmentNumber = application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments?
                                .Where(x => x.CompartmentId == restockingDetails.SubmittedFlaPropertyCompartmentId).FirstOrDefault()?.CompartmentNumber ?? string.Empty,
                            SubCompartmentName = restockingDetails.ConfirmedFellingDetail?.SubmittedFlaPropertyCompartment?
                                .SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
                                .Where(x => x.CompartmentId == restockingDetails.SubmittedFlaPropertyCompartmentId).FirstOrDefault()?.SubCompartmentName ?? string.Empty,
                            ConfirmedFellingDetailsId = restockingDetails.ConfirmedFellingDetailId
                        })
                    })
             }).ToList();

        return Result.Success(
            new CombinedConfirmedFellingAndRestockingDetailRecord(
                compartmentList,
                woodlandOfficerReviewMaybe.HasValue && woodlandOfficerReviewMaybe.Value.ConfirmedFellingAndRestockingComplete
                )
            );
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteConfirmedFellingDetailAsync(
        Guid applicationId,
        Guid confirmedFellingDetailId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var flaMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);
        if (flaMaybe.HasNoValue)
        {
            _logger.LogError("Unable to retrieve felling licence application {appId}", applicationId);
            return Result.Failure("Unable to retrieve felling licence application");
        }

        var fla = flaMaybe.Value;

        var userCheck = CheckUserIsPermittedToAmend(fla, userId);
        if (userCheck.IsFailure)
        {
            return userCheck;
        }

        var compartment = fla.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
            .FirstOrDefault(c => c.ConfirmedFellingDetails.Any(f => f.Id == confirmedFellingDetailId));
        if (compartment is null)
        {
            _logger.LogError("Confirmed felling detail not found in compartment for application {appId}", applicationId);
            return Result.Failure("Confirmed felling detail not found");
        }

        var confirmedFellingDetail = compartment.ConfirmedFellingDetails.FirstOrDefault(f => f.Id == confirmedFellingDetailId);
        if (confirmedFellingDetail is null)
        {
            _logger.LogError("Confirmed felling detail with id {id} not found in compartment for application {appId}", confirmedFellingDetailId, applicationId);
            return Result.Failure("Confirmed felling detail not found");
        }

        foreach (var restockingDetail in confirmedFellingDetail.ConfirmedRestockingDetails.ToList())
        {
            restockingDetail.ConfirmedRestockingSpecies.Clear();
            confirmedFellingDetail.ConfirmedRestockingDetails.Remove(restockingDetail);
        }

        compartment.ConfirmedFellingDetails.Remove(confirmedFellingDetail);

        _internalFlaRepository.Update(fla);

        var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return dbResult.IsFailure
            ? Result.Failure(dbResult.Error.ToString())
            : Result.Success();
    }

    /// <inheritdoc/>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => await _internalFlaRepository.BeginTransactionAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<Result> ConvertProposedFellingAndRestockingToConfirmedAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellation)
    {
        var flaMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellation);
        if (flaMaybe.HasNoValue)
        {
            await ImportDetailsFailureEvent(
                applicationId,
                userId,
                "Unable to retrieve felling licence application",
                cancellation);

            _logger.LogError("Unable to retrieve felling licence application in ConvertProposedFellingAndRestockingToConfirmedAsync, id {id}", applicationId);
            return Result.Failure($"Unable to retrieve felling licence application in ConvertProposedFellingAndRestockingToConfirmedAsync, id {applicationId}");
        }

        var userCheck = CheckUserIsPermittedToAmend(
            flaMaybe.Value,
            userId);

        if (userCheck.IsFailure)
        {
            await ImportDetailsFailureEvent(
                applicationId,
                userId,
                userCheck.Error,
                cancellation);

            return userCheck;
        }

        var fellingResult = ConvertProposedFellingDetailsToConfirmedAsync(flaMaybe.Value);

        if (fellingResult.IsFailure)
        {
            await ImportDetailsFailureEvent(
                applicationId,
                userId,
                fellingResult.Error,
                cancellation);

            return fellingResult;
        }

        _internalFlaRepository.Update(flaMaybe.Value);

        try
        {
            var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellation);

            if (dbResult.IsFailure)
            {
                await ImportDetailsFailureEvent(
                    applicationId,
                    userId,
                    dbResult.Error.ToString(),
                    cancellation);

                return Result.Failure(dbResult.Error.ToString() + $" in ConvertProposedFellingAndRestockingToConfirmedAsync, application id {flaMaybe.Value.Id}");
            }

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    userId,
                    _requestContext,
                    new
                    {
                        Section = "Import Confirmed Felling/Restocking Details"
                    }
                ), cancellation);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in ConvertProposedFellingAndRestockingToConfirmedAsync");
            return Result.Failure(ex.Message);
        }

    }

    /// <inheritdoc/>
    public async Task<Result> RevertConfirmedFellingDetailAmendmentsAsync(
        Guid applicationId,
        Guid proposedFellingDetailsId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var flaMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);
            if (flaMaybe.HasNoValue)
            {
                _logger.LogError("Unable to retrieve felling licence application in RevertConfirmedFellingDetailAmendmentsAsync, id {id}", applicationId);
                return Result.Failure($"Unable to retrieve felling licence application in RevertConfirmedFellingDetailAmendmentsAsync, id {applicationId}");
            }

            var fla = flaMaybe.Value;

            var userCheck = CheckUserIsPermittedToAmend(fla, userId);
            if (userCheck.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    userCheck.Error,
                    cancellationToken);

                _logger.LogError(
                    "User {userId} is not permitted to amend felling licence application {applicationId} in RevertConfirmedFellingDetailAmendmentsAsync",
                    userId,
                    applicationId);

                return userCheck;
            }

            var proposedFellingDetail = fla.LinkedPropertyProfile?.ProposedFellingDetails?
                .FirstOrDefault(p => p.Id == proposedFellingDetailsId);

            if (proposedFellingDetail is null)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    "Unable to find proposed felling detail for the given id",
                    cancellationToken);

                _logger.LogError("Unable to find proposed felling detail {proposedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}",
                    proposedFellingDetailsId, applicationId);
                return Result.Failure($"Unable to find proposed felling detail {proposedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}");
            }

            var compartment = fla.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
                .FirstOrDefault(c => c.CompartmentId == proposedFellingDetail.PropertyProfileCompartmentId);

            if (compartment is null)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    "Unable to find compartment for proposed felling detail",
                    cancellationToken);

                _logger.LogError("Unable to find compartment for proposed felling detail {proposedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}",
                    proposedFellingDetailsId, applicationId);
                return Result.Failure($"Unable to find compartment for proposed felling detail {proposedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}");
            }

            var confirmedFellingDetail = compartment.ConfirmedFellingDetails
                .FirstOrDefault(f => f.ProposedFellingDetailId == proposedFellingDetailsId);

            if (confirmedFellingDetail is not null)
            {
                _logger.LogInformation("Confirmed felling detail already exists for proposed felling detail {proposedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}",
                    proposedFellingDetailsId, applicationId);
                return await RevertConfirmedFellingDetailEntityAsync(
                        fla,
                        confirmedFellingDetail.Id,
                        userId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var newConfirmedFellingDetail = new ConfirmedFellingDetail
            {
                AreaToBeFelled = proposedFellingDetail.AreaToBeFelled,
                IsPartOfTreePreservationOrder = proposedFellingDetail.IsPartOfTreePreservationOrder,
                TreePreservationOrderReference = proposedFellingDetail.TreePreservationOrderReference,
                IsWithinConservationArea = proposedFellingDetail.IsWithinConservationArea,
                ConservationAreaReference = proposedFellingDetail.ConservationAreaReference,
                NumberOfTrees = proposedFellingDetail.NumberOfTrees,
                OperationType = proposedFellingDetail.OperationType,
                TreeMarking = proposedFellingDetail.TreeMarking,
                EstimatedTotalFellingVolume = proposedFellingDetail.EstimatedTotalFellingVolume,
                IsRestocking = proposedFellingDetail.IsRestocking ?? (proposedFellingDetail.ProposedRestockingDetails?.Any() == true),
                NoRestockingReason = proposedFellingDetail.NoRestockingReason,
                ProposedFellingDetailId = proposedFellingDetailsId,
                SubmittedFlaPropertyCompartment = compartment,
                SubmittedFlaPropertyCompartmentId = compartment.Id
            };

            if (proposedFellingDetail.FellingSpecies != null)
            {
                foreach (var species in proposedFellingDetail.FellingSpecies)
                {
                    newConfirmedFellingDetail.ConfirmedFellingSpecies.Add(new ConfirmedFellingSpecies
                    {
                        Species = species.Species,
                        ConfirmedFellingDetail = newConfirmedFellingDetail,
                        ConfirmedFellingDetailId = newConfirmedFellingDetail.Id
                    });
                }
            }

            if (proposedFellingDetail.ProposedRestockingDetails != null)
            {
                foreach (var proposedRestock in proposedFellingDetail.ProposedRestockingDetails)
                {
                    var restockCompartment = fla.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
                        .FirstOrDefault(x => x.CompartmentId == proposedRestock.PropertyProfileCompartmentId);

                    var confirmedRestock = new ConfirmedRestockingDetail
                    {
                        Area = proposedRestock.Area,
                        SubmittedFlaPropertyCompartmentId = restockCompartment?.Id ?? compartment.Id,
                        PercentageOfRestockArea = proposedRestock.PercentageOfRestockArea,
                        RestockingDensity = proposedRestock.RestockingDensity,
                        NumberOfTrees = proposedRestock.NumberOfTrees,
                        RestockingProposal = proposedRestock.RestockingProposal,
                        ConfirmedFellingDetail = newConfirmedFellingDetail,
                        ConfirmedFellingDetailId = newConfirmedFellingDetail.Id
                    };

                    if (proposedRestock.RestockingSpecies is not null)
                    {
                        foreach (var species in proposedRestock.RestockingSpecies)
                        {
                            confirmedRestock.ConfirmedRestockingSpecies.Add(new ConfirmedRestockingSpecies
                            {
                                Species = species.Species,
                                Percentage = species.Percentage,
                                ConfirmedRestockingDetail = confirmedRestock,
                                ConfirmedRestockingDetailsId = confirmedRestock.Id
                            });
                        }
                    }

                    newConfirmedFellingDetail.ConfirmedRestockingDetails.Add(confirmedRestock);
                }
            }

            compartment.ConfirmedFellingDetails.Add(newConfirmedFellingDetail);
            _internalFlaRepository.Update(fla);

            var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (dbResult.IsFailure)
            {
                _logger.LogError("Failed to save reverted deleted confirmed felling detail for application {applicationId}: {error}", applicationId, dbResult.Error);
                return Result.Failure(dbResult.Error.ToString());
            }

            _logger.LogInformation(
                "Reverted deleted confirmed felling detail for application {applicationId}, proposed felling detail {proposedFellingDetailsId}",
                applicationId,
                proposedFellingDetailsId);

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    null,
                    _requestContext,
                    new
                    {
                        Section = "Revert Deleted Confirmed Felling/Restocking Detail Amendments",
                        ProposedFellingDetailsId = proposedFellingDetailsId
                    }
                ), cancellationToken
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in RevertConfirmedFellingDetailAmendmentsAsync");
            return Result.Failure("Exception caught in RevertConfirmedFellingDetailAmendmentsAsync");
        }
    }

    private async Task<Result> RevertConfirmedFellingDetailEntityAsync(
        FellingLicenceApplication fla,
        Guid confirmedFellingDetailsId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var compartment = fla.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
            .FirstOrDefault(c => c.ConfirmedFellingDetails.Any(f => f.Id == confirmedFellingDetailsId));

        if (compartment is null)
        {
            await SaveDetailsFailureEvent(
                fla.Id,
                userId,
                $"Unable to find compartment for confirmed felling detail {confirmedFellingDetailsId}",
                cancellationToken);

            _logger.LogError("Unable to find compartment for confirmed felling detail {confirmedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}",
                confirmedFellingDetailsId, fla.Id);

            return Result.Failure($"Unable to find compartment for confirmed felling detail {confirmedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {fla.Id}");
        }

        var confirmedFellingDetail = compartment.ConfirmedFellingDetails.FirstOrDefault(f => f.Id == confirmedFellingDetailsId);
        if (confirmedFellingDetail is null)
        {
            await SaveDetailsFailureEvent(
                fla.Id,
                userId,
                $"Unable to find confirmed felling detail {confirmedFellingDetailsId}",
                cancellationToken);

            _logger.LogError("Unable to find confirmed felling detail {confirmedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}",
                confirmedFellingDetailsId, fla.Id);

            return Result.Failure($"Unable to find confirmed felling detail {confirmedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {fla.Id}");
        }

        if (confirmedFellingDetail.ProposedFellingDetailId is null || fla.LinkedPropertyProfile?.ProposedFellingDetails is null)
        {
            await SaveDetailsFailureEvent(
                fla.Id,
                userId,
                $"No linked proposed felling detail for confirmed felling detail {confirmedFellingDetailsId}",
                cancellationToken);

            _logger.LogError("No linked proposed felling detail for confirmed felling detail {confirmedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}",
                confirmedFellingDetailsId, fla.Id);

            return Result.Failure($"No linked proposed felling detail for confirmed felling detail {confirmedFellingDetailsId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {fla.Id}");
        }

        var proposedFellingDetail = fla.LinkedPropertyProfile.ProposedFellingDetails
            .FirstOrDefault(p => p.Id == confirmedFellingDetail.ProposedFellingDetailId);

        if (proposedFellingDetail is null)
        {
            await SaveDetailsFailureEvent(
                fla.Id,
                userId,
                $"Unable to find proposed felling detail {confirmedFellingDetail.ProposedFellingDetailId}",
                cancellationToken);

            _logger.LogError("Unable to find proposed felling detail {proposedFellingDetailId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {applicationId}",
                confirmedFellingDetail.ProposedFellingDetailId, fla.Id);

            return Result.Failure($"Unable to find proposed felling detail {confirmedFellingDetail.ProposedFellingDetailId} in RevertConfirmedFellingDetailAmendmentsAsync, application id {fla.Id}");
        }

        confirmedFellingDetail.AreaToBeFelled = proposedFellingDetail.AreaToBeFelled;
        confirmedFellingDetail.IsPartOfTreePreservationOrder = proposedFellingDetail.IsPartOfTreePreservationOrder;
        confirmedFellingDetail.TreePreservationOrderReference = proposedFellingDetail.TreePreservationOrderReference;
        confirmedFellingDetail.IsWithinConservationArea = proposedFellingDetail.IsWithinConservationArea;
        confirmedFellingDetail.ConservationAreaReference = proposedFellingDetail.ConservationAreaReference;
        confirmedFellingDetail.NumberOfTrees = proposedFellingDetail.NumberOfTrees;
        confirmedFellingDetail.OperationType = proposedFellingDetail.OperationType;
        confirmedFellingDetail.TreeMarking = proposedFellingDetail.TreeMarking;
        confirmedFellingDetail.EstimatedTotalFellingVolume = proposedFellingDetail.EstimatedTotalFellingVolume;
        confirmedFellingDetail.IsRestocking = proposedFellingDetail.IsRestocking ?? (proposedFellingDetail.ProposedRestockingDetails?.Any() == true);
        confirmedFellingDetail.NoRestockingReason = proposedFellingDetail.NoRestockingReason;

        confirmedFellingDetail.ConfirmedFellingSpecies.Clear();
        if (proposedFellingDetail.FellingSpecies != null)
        {
            foreach (var species in proposedFellingDetail.FellingSpecies)
            {
                confirmedFellingDetail.ConfirmedFellingSpecies.Add(new ConfirmedFellingSpecies
                {
                    Species = species.Species,
                    ConfirmedFellingDetail = confirmedFellingDetail,
                    ConfirmedFellingDetailId = confirmedFellingDetail.Id
                });
            }
        }

        confirmedFellingDetail.ConfirmedRestockingDetails.Clear();
        if (proposedFellingDetail.ProposedRestockingDetails is not null)
        {
            foreach (var proposedRestock in proposedFellingDetail.ProposedRestockingDetails)
            {
                var restockCompartment = fla.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
                    .FirstOrDefault(x => x.CompartmentId == proposedRestock.PropertyProfileCompartmentId);

                var confirmedRestock = new ConfirmedRestockingDetail
                {
                    Area = proposedRestock.Area,
                    SubmittedFlaPropertyCompartmentId = restockCompartment?.Id ?? compartment.Id,
                    PercentageOfRestockArea = proposedRestock.PercentageOfRestockArea,
                    RestockingDensity = proposedRestock.RestockingDensity,
                    NumberOfTrees = proposedRestock.NumberOfTrees,
                    RestockingProposal = proposedRestock.RestockingProposal,
                    ConfirmedFellingDetail = confirmedFellingDetail,
                    ConfirmedFellingDetailId = confirmedFellingDetail.Id
                };

                if (proposedRestock.RestockingSpecies is not null)
                {
                    foreach (var species in proposedRestock.RestockingSpecies)
                    {
                        confirmedRestock.ConfirmedRestockingSpecies.Add(new ConfirmedRestockingSpecies
                        {
                            Species = species.Species,
                            Percentage = species.Percentage,
                            ConfirmedRestockingDetail = confirmedRestock,
                            ConfirmedRestockingDetailsId = confirmedRestock.Id
                        });
                    }
                }

                confirmedFellingDetail.ConfirmedRestockingDetails.Add(confirmedRestock);
            }
        }

        _internalFlaRepository.Update(fla);

        var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (dbResult.IsFailure)
        {
            await SaveDetailsFailureEvent(
                fla.Id,
                userId,
                dbResult.Error.ToString(),
                cancellationToken);

            return Result.Failure(dbResult.Error.ToString());
        }

        _logger.LogInformation(
            "Reverted confirmed felling detail amendments for application {applicationId}, confirmed felling detail {confirmedFellingDetailsId}",
            fla.Id,
            confirmedFellingDetailsId);

        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                fla.Id,
                userId,
                _requestContext,
                new
                {
                    Section = "Revert Confirmed Felling/Restocking Detail Amendments",
                    ConfirmedFellingDetailsId = confirmedFellingDetailsId
                }
            ), cancellationToken
        );

        return Result.Success();
    }

    private Result ConvertProposedFellingDetailsToConfirmedAsync(FellingLicenceApplication fla)
    {
        try
        {
            var propertyProfile = fla.LinkedPropertyProfile;

            if (propertyProfile is null)
            {
                _logger.LogError("Unable to retrieve property profile in ConvertProposedFellingDetailsToConfirmed, id {id}", fla.Id);
                return Result.Failure($"Unable to retrieve property profile in ConvertProposedFellingDetailsToConfirmed, id {fla.Id}");
            }
            // Clear any previously stored fellings before converting the new proposed list
            fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments.ForEach(f => f.ConfirmedFellingDetails.Clear());
            foreach (var propFelling in propertyProfile.ProposedFellingDetails!)
            {
                var submittedFlaPropertyCompartment =
                    fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.FirstOrDefault(x =>
                        x.CompartmentId == propFelling.PropertyProfileCompartmentId);

                if (submittedFlaPropertyCompartment is null)
                {
                    _logger.LogError("Unable to determine submitted FLA property compartment with id {compartmentId}, FLA id {flaId}", propertyProfile.PropertyProfileId, fla.Id);
                    return Result.Failure($"Unable to determine submitted FLA property compartment with id {propFelling.PropertyProfileCompartmentId}, FLA id {fla.Id}");
                }

                var confirmedFellingDetail = new ConfirmedFellingDetail()
                {
                    AreaToBeFelled = propFelling.AreaToBeFelled,
                    IsPartOfTreePreservationOrder = propFelling.IsPartOfTreePreservationOrder,
                    TreePreservationOrderReference = propFelling.TreePreservationOrderReference,
                    IsWithinConservationArea = propFelling.IsWithinConservationArea,
                    ConservationAreaReference = propFelling.ConservationAreaReference,
                    NumberOfTrees = propFelling.NumberOfTrees,
                    OperationType = propFelling.OperationType,
                    SubmittedFlaPropertyCompartment = submittedFlaPropertyCompartment,
                    TreeMarking = propFelling.TreeMarking,
                    SubmittedFlaPropertyCompartmentId = submittedFlaPropertyCompartment.Id,
                    EstimatedTotalFellingVolume = propFelling.EstimatedTotalFellingVolume,
                    IsRestocking = propFelling.IsRestocking ?? propFelling.ProposedRestockingDetails?.Count() > 0,
                    NoRestockingReason = propFelling.NoRestockingReason,
                    ProposedFellingDetailId = propFelling.Id
                };

                var confirmedSpecies = confirmedFellingDetail.ConfirmedFellingSpecies;
                confirmedSpecies.Clear();

                foreach (var species in propFelling.FellingSpecies!)
                {
                    confirmedSpecies.Add(new ConfirmedFellingSpecies()
                    {
                        Species = species.Species,
                        ConfirmedFellingDetail = confirmedFellingDetail,
                        ConfirmedFellingDetailId = confirmedFellingDetail.Id
                    });
                }
                foreach (var proposedRestockingDetail in propFelling.ProposedRestockingDetails)
                {
                    var restockSubmittedFlaPropertyCompartment =
                        fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.FirstOrDefault(x =>
                            x.CompartmentId == proposedRestockingDetail.PropertyProfileCompartmentId);
                    var confirmedRestockingDetail = new ConfirmedRestockingDetail
                    {
                        Area = proposedRestockingDetail.Area,
                        SubmittedFlaPropertyCompartmentId = restockSubmittedFlaPropertyCompartment?.Id ?? submittedFlaPropertyCompartment.Id,
                        PercentageOfRestockArea = proposedRestockingDetail.PercentageOfRestockArea,
                        RestockingDensity = proposedRestockingDetail.RestockingDensity,
                        NumberOfTrees = proposedRestockingDetail.NumberOfTrees,
                        RestockingProposal = proposedRestockingDetail.RestockingProposal,
                        ConfirmedFellingDetail = confirmedFellingDetail,
                        ConfirmedFellingDetailId = confirmedFellingDetail.Id,
                        ProposedRestockingDetailId = proposedRestockingDetail.Id
                    };

                    foreach (var species in proposedRestockingDetail.RestockingSpecies)
                    {
                        confirmedRestockingDetail.ConfirmedRestockingSpecies.Add(new()
                        {
                            Species = species.Species,
                            Percentage = species.Percentage,
                            ConfirmedRestockingDetail = confirmedRestockingDetail,
                            ConfirmedRestockingDetailsId = confirmedRestockingDetail.Id,
                        });
                    }
                    confirmedFellingDetail.ConfirmedRestockingDetails.Add(confirmedRestockingDetail);
                }
                submittedFlaPropertyCompartment.ConfirmedFellingDetails.Add(confirmedFellingDetail);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in ConvertProposedFellingDetailsToConfirmed");
            return Result.Failure("Exception caught in ConvertProposedFellingDetailsToConfirmed");
        }
    }

    private Result CheckUserIsPermittedToAmend(
        FellingLicenceApplication fla,
        Guid userId,
        [CallerMemberName] string? methodName = null)
    {
        // check the application is in the Woodland Officer review stage

        if (fla.StatusHistories.MaxBy(x => x.Created)?.Status != FellingLicenceStatus.WoodlandOfficerReview &&
           fla.StatusHistories.MaxBy(x => x.Created)?.Status != FellingLicenceStatus.AdminOfficerReview)
        {
            _logger.LogError("Application is not in Woodland Officer review stage in {methodName}, id {id}", methodName, fla.Id);
            return Result.Failure($"Application is not in Woodland Officer review stage in {methodName}, id {fla.Id}");
        }

        // check that the user is permitted to change details

        if (fla.AssigneeHistories.FirstOrDefault(x =>
                (x.Role is AssignedUserRole.WoodlandOfficer || x.Role is AssignedUserRole.AdminOfficer) && x.AssignedUserId == userId && !x.TimestampUnassigned.HasValue) is null)
        {
            _logger.LogError("User is not assigned Woodland Officer for application in {methodName}, application id {id}, user id {userId}", methodName, fla.Id, userId);
            return Result.Failure($"User is not assigned Woodland Officer for application in {methodName}, application id {fla.Id}, user id {userId}");
        }

        return Result.Success();
    }

    private async Task ImportDetailsFailureEvent(
        Guid applicationId,
        Guid userId,
        string error,
        CancellationToken cancellationToken)
    {
        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReviewFailure,
                applicationId,
                userId,
                _requestContext,
                new
                {
                    Section = "Import Confirmed Felling/Restocking Details",
                    Error = error
                }
            ), cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<Result> SaveChangesToConfirmedFellingAndRestockingAsync(
        Guid applicationId,
        Guid userId,
        IList<ConfirmedFellingAndRestockingDetailModel> confirmedFellingAndRestockingDetailModels,
        CancellationToken cancellationToken)
    {
        try
        {
            var flaMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);
            if (flaMaybe.HasNoValue)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    "Unable to retrieve felling licence application",
                    cancellationToken);

                _logger.LogError("Unable to retrieve felling licence application in SaveChangesToConfirmedFellingAsync, id {id}", applicationId);
                return Result.Failure($"Unable to retrieve felling licence application in SaveChangesToConfirmedFellingAsync, id {applicationId}");
            }

            var updatedFla = flaMaybe.Value;

            var userCheck = CheckUserIsPermittedToAmend(flaMaybe.Value, userId);

            if (userCheck.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    userCheck.Error,
                    cancellationToken);

                return userCheck;
            }

            foreach (var newConfirmedFellRestock in confirmedFellingAndRestockingDetailModels)
            {
                var updatedCompartment =
                    updatedFla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.FirstOrDefault(x =>
                        x.CompartmentId == newConfirmedFellRestock.CompartmentId);

                if (updatedCompartment is null)
                {
                    await SaveDetailsFailureEvent(
                        applicationId,
                        userId,
                        $"Unable to retrieve submitted property compartment, compartment id {newConfirmedFellRestock.CompartmentId}",
                        cancellationToken);

                    _logger.LogError("Unable to retrieve submitted property compartment {compId} in SaveChangesToConfirmedFellingAsync, id {id}",
                        newConfirmedFellRestock.CompartmentId,
                        applicationId);

                    return Result.Failure($"Unable to retrieve submitted property compartment {newConfirmedFellRestock.CompartmentId} in SaveChangesToConfirmedFellingAsync, id {applicationId}");
                }

                updatedCompartment.ConfirmedTotalHectares = newConfirmedFellRestock.ConfirmedTotalHectares;

                foreach (var newConfirmedFell in newConfirmedFellRestock.ConfirmedFellingDetailModels)
                {
                    // Remove the deleted
                    updatedCompartment.ConfirmedFellingDetails.ToList().RemoveAll(x => x.Id != newConfirmedFell?.ConfirmedFellingDetailsId);
                    // update the edited
                    var dbConfirmedFell = updatedCompartment.ConfirmedFellingDetails.Where(x => x.Id == newConfirmedFell?.ConfirmedFellingDetailsId).FirstOrDefault();
                    // add the new
                    if (dbConfirmedFell == null)
                    {
                        dbConfirmedFell ??= new ConfirmedFellingDetail();
                        updatedCompartment.ConfirmedFellingDetails.Add(dbConfirmedFell);
                    }
                    dbConfirmedFell.AreaToBeFelled = newConfirmedFell.AreaToBeFelled;
                    dbConfirmedFell.IsPartOfTreePreservationOrder = newConfirmedFell.IsPartOfTreePreservationOrder;
                    dbConfirmedFell.TreePreservationOrderReference = newConfirmedFell.TreePreservationOrderReference;
                    dbConfirmedFell.OperationType = newConfirmedFell.OperationType;
                    dbConfirmedFell.NumberOfTrees = newConfirmedFell.NumberOfTrees;
                    dbConfirmedFell.IsWithinConservationArea = newConfirmedFell.IsWithinConservationArea;
                    dbConfirmedFell.ConservationAreaReference = newConfirmedFell.ConservationAreaReference;
                    dbConfirmedFell.TreeMarking = newConfirmedFell.IsTreeMarkingUsed ?? false ? newConfirmedFell.TreeMarking : null;
                    dbConfirmedFell.IsRestocking = newConfirmedFell.IsRestocking;
                    dbConfirmedFell.NoRestockingReason = newConfirmedFell.NoRestockingReason;
                    dbConfirmedFell.ConfirmedFellingSpecies = newConfirmedFell.ConfirmedFellingSpecies.ToList();
                    dbConfirmedFell.SubmittedFlaPropertyCompartmentId = updatedCompartment.Id;
                    dbConfirmedFell.EstimatedTotalFellingVolume = newConfirmedFell.EstimatedTotalFellingVolume;

                    foreach (var newConfirmedRestock in newConfirmedFell.ConfirmedRestockingDetailModels)
                    {
                        if (newConfirmedRestock != null)
                        {
                            dbConfirmedFell.ConfirmedRestockingDetails.ToList().RemoveAll(x => x.Id != newConfirmedRestock?.ConfirmedRestockingDetailsId);
                            var dbConfirmedRestock = dbConfirmedFell.ConfirmedRestockingDetails.Where(x => x.Id == newConfirmedRestock?.ConfirmedRestockingDetailsId).FirstOrDefault();
                            if (dbConfirmedRestock == null)
                            {
                                dbConfirmedRestock ??= new ConfirmedRestockingDetail();
                                dbConfirmedFell.ConfirmedRestockingDetails.Add(dbConfirmedRestock);
                            }
                            dbConfirmedRestock.SubmittedFlaPropertyCompartmentId = newConfirmedRestock.CompartmentId;
                            dbConfirmedRestock.Area = newConfirmedRestock.Area;
                            dbConfirmedRestock.PercentOpenSpace = newConfirmedRestock.PercentOpenSpace;
                            dbConfirmedRestock.PercentNaturalRegeneration = newConfirmedRestock.PercentNaturalRegeneration;
                            dbConfirmedRestock.PercentageOfRestockArea = newConfirmedRestock.PercentageOfRestockArea;
                            dbConfirmedRestock.RestockingDensity = newConfirmedRestock.RestockingDensity;
                            dbConfirmedRestock.NumberOfTrees = newConfirmedRestock.NumberOfTrees;
                            dbConfirmedRestock.RestockingProposal = newConfirmedRestock.RestockingProposal;
                            dbConfirmedRestock.ConfirmedRestockingSpecies = newConfirmedRestock.ConfirmedRestockingSpecies ?? new List<ConfirmedRestockingSpecies>();
                        }
                    }
                }
            }

            _internalFlaRepository.Update(updatedFla);

            var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    dbResult.Error.ToString(),
                    cancellationToken);

                return Result.Failure(dbResult.Error.ToString());
            }

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    userId,
                    _requestContext,
                    new
                    {
                        Section = "Save Changes Confirmed Felling/Restocking Details"
                    }
                ), cancellationToken
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in SaveChangesToConfirmedFellingAsync");
            return Result.Failure("Exception caught in SaveChangesToConfirmedFellingAsync");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveChangesToConfirmedFellingDetailsAsync(
        Guid applicationId,
        Guid userId,
        IndividualConfirmedFellingRestockingDetailModel confirmedFellingDetailsModel,
        Dictionary<string, SpeciesModel> speciesModel,
        CancellationToken cancellationToken)
    {
        try
        {
            var flaMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);
            if (flaMaybe.HasNoValue)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    "Unable to retrieve felling licence application",
                    cancellationToken);

                _logger.LogError("Unable to retrieve felling licence application in SaveChangesToConfirmedFellingAsync, id {id}", applicationId);
                return Result.Failure($"Unable to retrieve felling licence application in SaveChangesToConfirmedFellingAsync, id {applicationId}");
            }

            var updatedFla = flaMaybe.Value;

            var userCheck = CheckUserIsPermittedToAmend(flaMaybe.Value, userId);

            if (userCheck.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    userCheck.Error,
                    cancellationToken);

                return userCheck;
            }

            var updatedCompartment =
                updatedFla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.FirstOrDefault(x =>
                    x.CompartmentId == confirmedFellingDetailsModel.CompartmentId);

            if (updatedCompartment is null)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    $"Unable to retrieve submitted property compartment, compartment id {confirmedFellingDetailsModel.CompartmentId}",
                    cancellationToken);

                _logger.LogError("Unable to retrieve submitted property compartment {compId} in SaveChangesToConfirmedFellingAsync, id {id}",
                    confirmedFellingDetailsModel.CompartmentId,
                    applicationId);

                return Result.Failure($"Unable to retrieve submitted property compartment {confirmedFellingDetailsModel.CompartmentId} in SaveChangesToConfirmedFellingAsync, id {applicationId}");
            }

            var newConfirmedFell = confirmedFellingDetailsModel.ConfirmedFellingDetailModel;
            var dbConfirmedFell = updatedCompartment.ConfirmedFellingDetails.FirstOrDefault(x => x.Id == newConfirmedFell?.ConfirmedFellingDetailsId);

            if (dbConfirmedFell is null)
            {
                dbConfirmedFell ??= new ConfirmedFellingDetail();
                updatedCompartment.ConfirmedFellingDetails.Add(dbConfirmedFell);
            }
            dbConfirmedFell.AreaToBeFelled = newConfirmedFell.AreaToBeFelled;
            dbConfirmedFell.IsPartOfTreePreservationOrder = newConfirmedFell.IsPartOfTreePreservationOrder;
            dbConfirmedFell.TreePreservationOrderReference = newConfirmedFell.TreePreservationOrderReference;
            dbConfirmedFell.OperationType = newConfirmedFell.OperationType;
            dbConfirmedFell.NumberOfTrees = newConfirmedFell.NumberOfTrees;
            dbConfirmedFell.IsWithinConservationArea = newConfirmedFell.IsWithinConservationArea;
            dbConfirmedFell.ConservationAreaReference = newConfirmedFell.ConservationAreaReference;
            dbConfirmedFell.TreeMarking = newConfirmedFell.TreeMarking;
            dbConfirmedFell.SubmittedFlaPropertyCompartmentId = updatedCompartment.Id;
            dbConfirmedFell.EstimatedTotalFellingVolume = newConfirmedFell.EstimatedTotalFellingVolume;
            dbConfirmedFell.IsRestocking = newConfirmedFell.IsRestocking;
            dbConfirmedFell.NoRestockingReason = newConfirmedFell.NoRestockingReason;

            foreach (var fellingSpecies in dbConfirmedFell.ConfirmedFellingSpecies.ToList()
                         .Where(fellingSpecies => speciesModel.ContainsKey(fellingSpecies.Species) is false))
            {
                dbConfirmedFell.ConfirmedFellingSpecies.Remove(fellingSpecies);
            }

            foreach (var species in speciesModel
                         .Where(species => dbConfirmedFell.ConfirmedFellingSpecies.Any(x => x.Species == species.Key) is false))
            {
                dbConfirmedFell.ConfirmedFellingSpecies.Add(new ConfirmedFellingSpecies
                {
                    Species = species.Key,
                    ConfirmedFellingDetail = dbConfirmedFell,
                    ConfirmedFellingDetailId = dbConfirmedFell.Id
                });
            }

            if (dbConfirmedFell.IsRestocking is not true)
            {
                _logger.LogInformation("Clearing confirmed restocking details for confirmed felling details {confirmedFellingId}", dbConfirmedFell.Id);
                dbConfirmedFell.ConfirmedRestockingDetails.Clear();
            }

            _internalFlaRepository.Update(updatedFla);

            var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    dbResult.Error.ToString(),
                    cancellationToken);

                return Result.Failure(dbResult.Error.ToString());
            }

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    userId,
                    _requestContext,
                    new
                    {
                        Section = "Save Changes Confirmed Felling Details"
                    }
                ), cancellationToken
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in SaveChangesToConfirmedFellingAsync");
            return Result.Failure("Exception caught in SaveChangesToConfirmedFellingAsync");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveChangesToConfirmedRestockingDetailsAsync(
        Guid applicationId,
        Guid userId,
        IndividualConfirmedRestockingDetailModel confirmedRestockingDetailsModel,
        Dictionary<string, SpeciesModel> speciesModel,
        CancellationToken cancellationToken)
    {
        try
        {
            var flaMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);
            if (flaMaybe.HasNoValue)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    "Unable to retrieve felling licence application",
                    cancellationToken);

                _logger.LogError("Unable to retrieve felling licence application in SaveChangesToConfirmedFellingAsync, id {id}", applicationId);
                return Result.Failure($"Unable to retrieve felling licence application in SaveChangesToConfirmedFellingAsync, id {applicationId}");
            }

            var updatedFla = flaMaybe.Value;

            var userCheck = CheckUserIsPermittedToAmend(flaMaybe.Value, userId);

            if (userCheck.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    userCheck.Error,
                    cancellationToken);

                return userCheck;
            }

            var updatedCompartment = updatedFla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.FirstOrDefault(x =>
                x.CompartmentId == confirmedRestockingDetailsModel.CompartmentId);

            if (updatedCompartment is null)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    $"Unable to retrieve submitted property compartment, compartment id {confirmedRestockingDetailsModel.CompartmentId}",
                    cancellationToken);

                _logger.LogError("Unable to retrieve submitted property compartment {compId} in SaveChangesToConfirmedFellingAsync, id {id}",
                    confirmedRestockingDetailsModel.CompartmentId,
                    applicationId);

                return Result.Failure($"Unable to retrieve submitted property compartment {confirmedRestockingDetailsModel.CompartmentId} in SaveChangesToConfirmedFellingAsync, id {applicationId}");
            }

            var newConfirmedRestock = confirmedRestockingDetailsModel.ConfirmedRestockingDetailModel;
            var dbConfirmedRestock = updatedFla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments
                .SelectMany(x => x.ConfirmedFellingDetails)
                .SelectMany(x => x.ConfirmedRestockingDetails)
                .First(x => x.Id == newConfirmedRestock?.ConfirmedRestockingDetailsId);

            // TODO: update logic in FLOV2-2223
            //if (dbConfirmedRestock is null)
            //{
                //dbConfirmedRestock ??= new ConfirmedRestockingDetail();
                //updatedCompartment.ConfirmedFellingDetails
                //    .First(x => x.Id == confirmedRestockingDetailsModel.ConfirmedRestockingDetailModel.ConfirmedFellingDetailsId).ConfirmedRestockingDetails
                //    .Add(dbConfirmedRestock);
            //}

            dbConfirmedRestock.SubmittedFlaPropertyCompartmentId = updatedCompartment.Id;
            dbConfirmedRestock.Area = newConfirmedRestock.Area;
            dbConfirmedRestock.RestockingProposal = newConfirmedRestock.RestockingProposal;
            dbConfirmedRestock.NumberOfTrees = newConfirmedRestock.NumberOfTrees;
            dbConfirmedRestock.PercentOpenSpace = newConfirmedRestock.PercentOpenSpace;
            dbConfirmedRestock.PercentNaturalRegeneration = newConfirmedRestock.PercentNaturalRegeneration;
            dbConfirmedRestock.PercentageOfRestockArea = newConfirmedRestock.PercentageOfRestockArea;
            dbConfirmedRestock.RestockingDensity = newConfirmedRestock.RestockingDensity;
            dbConfirmedRestock.ConfirmedRestockingSpecies = newConfirmedRestock.ConfirmedRestockingSpecies.ToList();
            dbConfirmedRestock.ConfirmedFellingDetailId = confirmedRestockingDetailsModel.ConfirmedRestockingDetailModel.ConfirmedFellingDetailsId;
            dbConfirmedRestock.SubmittedFlaPropertyCompartmentId = newConfirmedRestock.CompartmentId;


            foreach (var fellingSpecies in dbConfirmedRestock.ConfirmedRestockingSpecies.ToList()
                         .Where(fellingSpecies => speciesModel.ContainsKey(fellingSpecies.Species) is false))
            {
                dbConfirmedRestock.ConfirmedRestockingSpecies.Remove(fellingSpecies);
            }

            foreach (var species in speciesModel
                         .Where(species => dbConfirmedRestock.ConfirmedRestockingSpecies.Any(x => x.Species == species.Key) is false))
            {
                dbConfirmedRestock.ConfirmedRestockingSpecies.Add(new ConfirmedRestockingSpecies
                {
                    Species = species.Key,
                    Percentage = species.Value.Percentage,
                    ConfirmedRestockingDetail = dbConfirmedRestock,
                });
            }

            _internalFlaRepository.Update(updatedFla);

            var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    dbResult.Error.ToString(),
                    cancellationToken);

                return Result.Failure(dbResult.Error.ToString());
            }

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    userId,
                    _requestContext,
                    new
                    {
                        Section = "Save Changes Confirmed Restocking Details"
                    }
                ), cancellationToken
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in SaveChangesToConfirmedRestockingAsync");
            return Result.Failure("Exception caught in SaveChangesToConfirmedRestockingAsync");
        }
    }

    /// <inheritdoc />
    public async Task<Result> AddNewConfirmedFellingDetailsAsync(
        Guid applicationId,
        Guid userId,
        NewConfirmedFellingDetailWithCompartmentId confirmedFellingDetailsModel,
        Dictionary<string, SpeciesModel> speciesModel,
        CancellationToken cancellationToken)
    {
        try
        {
            var flaMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);
            if (flaMaybe.HasNoValue)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    "Unable to retrieve felling licence application",
                    cancellationToken);

                _logger.LogError("Unable to retrieve felling licence application in SaveChangesToConfirmedFellingAsync, id {id}", applicationId);
                return Result.Failure($"Unable to retrieve felling licence application in SaveChangesToConfirmedFellingAsync, id {applicationId}");
            }

            var updatedFla = flaMaybe.Value;

            var userCheck = CheckUserIsPermittedToAmend(flaMaybe.Value, userId);

            if (userCheck.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    userCheck.Error,
                    cancellationToken);

                return userCheck;
            }

            var updatedCompartment =
                updatedFla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.FirstOrDefault(x =>
                    x.CompartmentId == confirmedFellingDetailsModel.CompartmentId);

            if (updatedCompartment is null)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    $"Unable to retrieve submitted property compartment, compartment id {confirmedFellingDetailsModel.CompartmentId}",
                    cancellationToken);

                _logger.LogError("Unable to retrieve submitted property compartment {compId} in SaveChangesToConfirmedFellingAsync, id {id}",
                    confirmedFellingDetailsModel.CompartmentId,
                    applicationId);

                return Result.Failure($"Unable to retrieve submitted property compartment {confirmedFellingDetailsModel.CompartmentId} in SaveChangesToConfirmedFellingAsync, id {applicationId}");
            }

            updatedCompartment.ConfirmedFellingDetails.Add(
                new ConfirmedFellingDetail
                {
                    SubmittedFlaPropertyCompartmentId = updatedCompartment.CompartmentId,
                    AreaToBeFelled = confirmedFellingDetailsModel.Model.AreaToBeFelled,
                    IsPartOfTreePreservationOrder = confirmedFellingDetailsModel.Model.IsPartOfTreePreservationOrder,
                    TreePreservationOrderReference = confirmedFellingDetailsModel.Model.TreePreservationOrderReference,
                    IsWithinConservationArea = confirmedFellingDetailsModel.Model.IsWithinConservationArea,
                    ConservationAreaReference = confirmedFellingDetailsModel.Model.ConservationAreaReference,
                    ConfirmedFellingSpecies = speciesModel.Select(x => new ConfirmedFellingSpecies
                    {
                        Species = x.Value.Species
                    }).ToList(),
                    NumberOfTrees = confirmedFellingDetailsModel.Model.NumberOfTrees,
                    OperationType = confirmedFellingDetailsModel.Model.OperationType,
                    TreeMarking = confirmedFellingDetailsModel.Model.TreeMarking,
                    EstimatedTotalFellingVolume = confirmedFellingDetailsModel.Model.EstimatedTotalFellingVolume,
                    IsRestocking = confirmedFellingDetailsModel.Model.IsRestocking,
                    NoRestockingReason = confirmedFellingDetailsModel.Model.NoRestockingReason,
                    ProposedFellingDetailId = null
                }
            );

            var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                await SaveDetailsFailureEvent(
                    applicationId,
                    userId,
                    dbResult.Error.ToString(),
                    cancellationToken);

                return Result.Failure(dbResult.Error.ToString());
            }

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    userId,
                    _requestContext,
                    new
                    {
                        Section = "Save Changes Confirmed Felling Details"
                    }
                ), cancellationToken
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in SaveChangesToConfirmedFellingAsync");
            return Result.Failure("Exception caught in SaveChangesToConfirmedFellingAsync");
        }
    }
    private async Task SaveDetailsFailureEvent(
        Guid applicationId,
        Guid userId,
        string error,
        CancellationToken cancellationToken)
    {
        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReviewFailure,
                applicationId,
                userId,
                _requestContext,
                new
                {
                    Section = "Save Changes Confirmed Felling/Restocking Details",
                    Error = error
                }
            ), cancellationToken
        );
    }

    public Dictionary<string, string?> GetAmendedFellingDetailProperties(ProposedFellingDetail? proposed, ConfirmedFellingDetail confirmed)
    {
        var amended = new Dictionary<string, string?>();
        if (proposed == null || confirmed == null)
            return amended;
        void AddIfChanged<T>(string name, T? proposedValue, T? confirmedValue)
        {
            if (!EqualityComparer<T>.Default.Equals(proposedValue, confirmedValue))
                amended[name] = proposedValue == null ? null : proposedValue.ToString();
        }


        AddIfChanged(nameof(proposed.AreaToBeFelled), proposed.AreaToBeFelled, confirmed.AreaToBeFelled);
        AddIfChanged(nameof(proposed.IsPartOfTreePreservationOrder), proposed.IsPartOfTreePreservationOrder, confirmed.IsPartOfTreePreservationOrder);
        AddIfChanged(nameof(proposed.TreePreservationOrderReference), proposed.TreePreservationOrderReference, confirmed.TreePreservationOrderReference);
        AddIfChanged(nameof(proposed.IsWithinConservationArea), proposed.IsWithinConservationArea, confirmed.IsWithinConservationArea);
        AddIfChanged(nameof(proposed.IsRestocking), proposed.IsRestocking, confirmed.IsRestocking);
        AddIfChanged(nameof(proposed.ConservationAreaReference), proposed.ConservationAreaReference, confirmed.ConservationAreaReference);
        AddIfChanged(nameof(proposed.NumberOfTrees), proposed.NumberOfTrees, confirmed.NumberOfTrees);
        AddIfChanged(nameof(proposed.OperationType), proposed.OperationType.GetDisplayName(), confirmed.OperationType.GetDisplayName());
        AddIfChanged(nameof(proposed.TreeMarking), proposed.TreeMarking, confirmed.TreeMarking);
        AddIfChanged(nameof(proposed.EstimatedTotalFellingVolume), proposed.EstimatedTotalFellingVolume, confirmed.EstimatedTotalFellingVolume);

        // Compare FellingSpecies
        var proposedSpecies = proposed.FellingSpecies;
        var confirmedSpecies = confirmed.ConfirmedFellingSpecies;

        if (proposedSpecies != null && confirmedSpecies != null)
        {
            if (proposedSpecies.Count != confirmedSpecies.Count ||
                proposedSpecies.Any(ps => !confirmedSpecies.Any(cs => cs.Species == ps.Species)) ||
                confirmedSpecies.Any(cs => !proposedSpecies.Any(ps => ps.Species == cs.Species)))
            {
                amended[nameof(proposed.FellingSpecies)] = string.Join(",", proposedSpecies.Select(s =>
                        Models.TreeSpeciesFactory.SpeciesDictionary.TryGetValue(s.Species ?? "", out var speciesModel)
                            ? speciesModel.Name
                            : s.Species
                        ));
            }
        }

        return amended;

    }
    public Dictionary<string, string> GetAmendedRestockingProperties(ProposedRestockingDetail proposed, ConfirmedRestockingDetail confirmed, Dictionary<Guid, string?> compartmentNames)
    {
        var amended = new Dictionary<string, string>();
        if (proposed == null || confirmed == null)
            return amended;
        void AddIfChanged<T>(string name, T? proposedValue, T? confirmedValue)
        {
            if (!EqualityComparer<T>.Default.Equals(proposedValue, confirmedValue))
                amended[name] = proposedValue?.ToString() ?? string.Empty;
        }

        AddIfChanged(nameof(proposed.Area), proposed.Area, confirmed.Area);
        AddIfChanged(nameof(proposed.PercentageOfRestockArea), proposed.PercentageOfRestockArea, confirmed.PercentageOfRestockArea);
        AddIfChanged(nameof(proposed.RestockingDensity), proposed.RestockingDensity, confirmed.RestockingDensity);
        AddIfChanged(nameof(proposed.NumberOfTrees), proposed.NumberOfTrees, confirmed.NumberOfTrees);
        AddIfChanged(nameof(proposed.RestockingProposal), proposed.RestockingProposal.GetDisplayName(), confirmed.RestockingProposal.GetDisplayName());

        // Compare RestockingSpecies
        if (proposed.RestockingSpecies != null && confirmed.ConfirmedRestockingSpecies != null)
        {
            var propList = proposed.RestockingSpecies.OrderBy(x => x.Species).ToList();
            var confList = confirmed.ConfirmedRestockingSpecies.OrderBy(x => x.Species).ToList();
            bool anySpeciesOrPercentDifferent = false;
            if (propList.Count == confList.Count)
            {
                for (int j = 0; j < propList.Count; j++)
                {
                    if (propList[j].Species != confList[j].Species || propList[j].Percentage != confList[j].Percentage)
                    {
                        anySpeciesOrPercentDifferent = true;
                        break;
                    }
                }
            }
            else
                anySpeciesOrPercentDifferent = true;

            if (anySpeciesOrPercentDifferent)
            {
                amended[nameof(proposed.RestockingSpecies)] = string.Join(
                    ", ",
                    propList
                        .Select(s =>
                        {
                            var name = TreeSpeciesFactory.SpeciesDictionary.TryGetValue(s.Species ?? "", out var speciesModel)
                                ? speciesModel.Name
                                : s.Species;
                            var percentStr = s.Percentage.HasValue ? $"{s.Percentage.Value}%" : "";
                            return string.IsNullOrEmpty(percentStr) ? name : $"{name}: {percentStr}";
                        })
                );
            }
        }

        if(proposed.PropertyProfileCompartmentId != confirmed.SubmittedFlaPropertyCompartmentId)
        {
            compartmentNames.TryGetValue(proposed.PropertyProfileCompartmentId, out var proposedCompartmentName);
            compartmentNames.TryGetValue(confirmed.SubmittedFlaPropertyCompartmentId, out var confirmedCompartmentName);
            AddIfChanged("RestockingCompartmentNumber", proposedCompartmentName ?? string.Empty, confirmedCompartmentName ?? string.Empty);
        }
        return amended;
    }
}