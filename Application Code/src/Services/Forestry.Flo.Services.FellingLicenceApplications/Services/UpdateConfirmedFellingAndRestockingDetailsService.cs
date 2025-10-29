using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// An implementation of <see cref="IUpdateConfirmedFellingAndRestockingDetailsService"/> that updates confirmed felling/restocking details
/// using an <see cref="IFellingLicenceApplicationInternalRepository"/>
/// instance.
/// </summary>
public class UpdateConfirmedFellingAndRestockingDetailsService(
    IFellingLicenceApplicationInternalRepository internalFlaRepository,
    IFellingLicenceApplicationExternalRepository externalFlaRepository,
    ILogger<UpdateConfirmedFellingAndRestockingDetailsService> logger,
    IAuditService<UpdateConfirmedFellingAndRestockingDetailsService> audit,
    RequestContext requestContext)
    : IUpdateConfirmedFellingAndRestockingDetailsService
{
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
    private readonly ILogger<UpdateConfirmedFellingAndRestockingDetailsService> _logger = Guard.Against.Null(logger);
    private readonly IAuditService<UpdateConfirmedFellingAndRestockingDetailsService> _audit = Guard.Against.Null(audit);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);

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

        var woodlandOfficerReviewMaybe = await _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);
        // Add both Id and Proposed CompartmentId as keys for compartmentNames
        var compartmentNames = application.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
            .ToDictionary(x => x.Id, x => x.CompartmentNumber) ?? [];
        // Add CompartmentId as key as well, if not already present
        foreach (var compartment in application.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments ?? [])
        {
            if (!compartmentNames.ContainsKey(compartment.CompartmentId))
            {
                compartmentNames.Add(compartment.CompartmentId, compartment.CompartmentNumber);
            }
        }

        var treeSpeciesDict = TreeSpeciesFactory.SpeciesDictionary;

        var compartmentList =
            (from compartment in application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
             select new FellingAndRestockingDetailModel
             {
                 CompartmentId = compartment.CompartmentId,
                 SubmittedFlaPropertyCompartmentId = compartment.Id,
                 TotalHectares = Math.Round(compartment.TotalHectares??0, 2),
                 CompartmentNumber = compartment.CompartmentNumber,
                 SubCompartmentName = compartment.SubCompartmentName,
                 NearestTown = application.SubmittedFlaPropertyDetail.NearestTown,

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
                        IsTreeMarkingUsed = !string.IsNullOrEmpty(fellingDetails.TreeMarking),
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
                            ProposedRestockingDetailsId = restockingDetails.ProposedRestockingDetailId,
                            Area = restockingDetails.Area,
                            ConfirmedRestockingSpecies = restockingDetails.ConfirmedRestockingSpecies,
                            RestockingCompartmentTotalHectares = Math.Round(restockingDetails.ConfirmedFellingDetail?.SubmittedFlaPropertyCompartment?
                                .SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
                                .Where(x => x.Id == restockingDetails.SubmittedFlaPropertyCompartmentId).FirstOrDefault()?.TotalHectares ?? 0, 2),
                            PercentageOfRestockArea = restockingDetails.PercentageOfRestockArea,
                            PercentNaturalRegeneration = restockingDetails.PercentNaturalRegeneration,
                            PercentOpenSpace = restockingDetails.PercentOpenSpace,
                            RestockingDensity = restockingDetails.RestockingDensity,
                            NumberOfTrees = restockingDetails.NumberOfTrees,
                            RestockingProposal = restockingDetails.RestockingProposal,
                            CompartmentId = restockingDetails.SubmittedFlaPropertyCompartmentId,
                            AmendedProperties =
                                GetAmendedRestockingProperties(
                                    application.LinkedPropertyProfile.ProposedFellingDetails!
                                        .SelectMany(x => x.ProposedRestockingDetails)
                                        .FirstOrDefault(x => x.Id == restockingDetails.ProposedRestockingDetailId), restockingDetails, compartmentNames),
                            CompartmentNumber = application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments?
                                .Where(x => x.Id == restockingDetails.SubmittedFlaPropertyCompartmentId).FirstOrDefault()?.CompartmentNumber ?? string.Empty,
                            ConfirmedFellingDetailsId = restockingDetails.ConfirmedFellingDetailId
                        })
                    }),
                 ProposedFellingDetailModels = application.LinkedPropertyProfile.ProposedFellingDetails
                     .Where(x => x.PropertyProfileCompartmentId == compartment.CompartmentId)
                     .Select(proposedFellingDetail =>
                    new ProposedFellingDetailModel
                    {
                        Id = proposedFellingDetail.Id,
                        AreaToBeFelled = proposedFellingDetail.AreaToBeFelled,
                        IsPartOfTreePreservationOrder = proposedFellingDetail.IsPartOfTreePreservationOrder,
                        TreePreservationOrderReference = proposedFellingDetail.TreePreservationOrderReference,
                        IsWithinConservationArea = proposedFellingDetail.IsWithinConservationArea,
                        ConservationAreaReference = proposedFellingDetail.ConservationAreaReference,
                        NumberOfTrees = proposedFellingDetail.NumberOfTrees,
                        OperationType = proposedFellingDetail.OperationType,
                        IsTreeMarkingUsed = !string.IsNullOrEmpty(proposedFellingDetail.TreeMarking),
                        TreeMarking = proposedFellingDetail.TreeMarking,
                        EstimatedTotalFellingVolume = proposedFellingDetail.EstimatedTotalFellingVolume,
                        Species = proposedFellingDetail.FellingSpecies
                            .ToDictionary(
                                x => x.Species,
                                x => new SpeciesModel
                                {
                                    Id = x.Id,
                                    Species = x.Species,
                                    SpeciesName = treeSpeciesDict[x.Species].Name
                                }),
                        IsRestocking = proposedFellingDetail.IsRestocking,
                        NoRestockingReason = proposedFellingDetail.NoRestockingReason,
                        ProposedRestockingDetails = proposedFellingDetail.ProposedRestockingDetails.Select(
                            restockingDetails =>
                            {
                                var restockingCompartment = application.SubmittedFlaPropertyDetail
                                    .SubmittedFlaPropertyCompartments
                                    .First(x => x.CompartmentId == restockingDetails.PropertyProfileCompartmentId);
                                return new ProposedRestockingDetailModel
                                {
                                    Id = restockingDetails.Id,
                                    Area = restockingDetails.Area,
                                    CompartmentTotalHectares = Math.Round(restockingCompartment.TotalHectares??0, 2),
                                    Species = restockingDetails.RestockingSpecies
                                        .ToDictionary(
                                            x => x.Species,
                                            x => new SpeciesModel
                                            {
                                                Id = x.Id,
                                                Species = x.Species,
                                                SpeciesName = treeSpeciesDict[x.Species].Name,
                                                Percentage = x.Percentage
                                            }),
                                    PercentageOfRestockArea = restockingDetails.PercentageOfRestockArea,
                                    RestockingDensity = restockingDetails.RestockingDensity,
                                    NumberOfTrees = restockingDetails.NumberOfTrees,
                                    RestockingProposal = restockingDetails.RestockingProposal,
                                    OperationType = restockingDetails.ProposedFellingDetail.OperationType,
                                    CompartmentId = restockingDetails.PropertyProfileCompartmentId,
                                    CompartmentNumber = restockingCompartment.CompartmentNumber,
                                    SubCompartmentName = restockingCompartment.SubCompartmentName,
                                };
                            }).ToList()
                    }),
             }).ToList();

        return Result.Success(
            new CombinedConfirmedFellingAndRestockingDetailRecord(
                compartmentList,
                application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!.ToList(),
                woodlandOfficerReviewMaybe.HasValue && woodlandOfficerReviewMaybe.Value.ConfirmedFellingAndRestockingComplete)
            );
    }

    /// <inheritdoc/>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => await _internalFlaRepository.BeginTransactionAsync(cancellationToken);

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
    public async Task<Result> DeleteConfirmedRestockingDetailAsync(
        Guid applicationId,
        Guid confirmedRestockingDetailId,
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

        var felling = fla.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
            .SelectMany(x => x.ConfirmedFellingDetails)
            .FirstOrDefault(f => f.ConfirmedRestockingDetails.Any(y => y.Id == confirmedRestockingDetailId));
        if (felling is null)
        {
            _logger.LogError("Confirmed felling detail not found for application {appId}", applicationId);
            return Result.Failure("Confirmed felling detail not found");
        }

        var confirmedRestockingDetail = felling.ConfirmedRestockingDetails.FirstOrDefault(f => f.Id == confirmedRestockingDetailId);
        if (confirmedRestockingDetail is null)
        {
            _logger.LogError("Confirmed restocking detail with id {id} not found for application {appId}", confirmedRestockingDetailId, applicationId);
            return Result.Failure("Confirmed restocking detail not found");
        }

        confirmedRestockingDetail.ConfirmedRestockingSpecies.Clear();
        felling.ConfirmedRestockingDetails.Remove(confirmedRestockingDetail);

        _internalFlaRepository.Update(fla);

        var dbResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return dbResult.IsFailure
            ? Result.Failure(dbResult.Error.ToString())
            : Result.Success();
    }

    /// <inheritdoc/>
    public Task<Maybe<SubmittedFlaPropertyDetail>> GetExistingSubmittedFlaPropertyDetailAsync(
        Guid applicationId, 
        CancellationToken cancellationToken) 
        => externalFlaRepository.GetExistingSubmittedFlaPropertyDetailAsync(applicationId, cancellationToken);

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
                IsRestocking = proposedFellingDetail.IsRestocking,
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
                        ConfirmedFellingDetailId = newConfirmedFellingDetail.Id,
                        ProposedRestockingDetailId = proposedRestock.Id
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

    /// <inheritdoc />
    public async Task<Result> SaveChangesToConfirmedFellingDetailsAsync(
        Guid applicationId,
        Guid userId,
        IndividualFellingRestockingDetailModel confirmedFellingDetailsModel,
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
            var dbConfirmedFell = updatedCompartment.ConfirmedFellingDetails.FirstOrDefault(x => x.Id == newConfirmedFell.ConfirmedFellingDetailsId);

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
        IndividualRestockingDetailModel confirmedRestockingDetailsModel,
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

            var newConfirmedRestock = confirmedRestockingDetailsModel.ConfirmedRestockingDetailModel;

            var dbConfirmedRestock = updatedFla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
                .SelectMany(x => x.ConfirmedFellingDetails)
                .SelectMany(x => x.ConfirmedRestockingDetails)
                .FirstOrDefault(x => x.Id == newConfirmedRestock.ConfirmedRestockingDetailsId);

            // Add New Restocking record if it does not exist
            if (dbConfirmedRestock is null)
            {
                dbConfirmedRestock ??= new ConfirmedRestockingDetail();
                updatedFla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
                    .SelectMany(x => x.ConfirmedFellingDetails)
                    .First(x => x.Id == newConfirmedRestock.ConfirmedFellingDetailsId).ConfirmedRestockingDetails
                    .Add(dbConfirmedRestock);
            }

            dbConfirmedRestock.SubmittedFlaPropertyCompartmentId = newConfirmedRestock.CompartmentId;
            dbConfirmedRestock.Area = newConfirmedRestock.Area;
            dbConfirmedRestock.RestockingProposal = newConfirmedRestock.RestockingProposal;
            dbConfirmedRestock.NumberOfTrees = newConfirmedRestock.NumberOfTrees;
            dbConfirmedRestock.PercentOpenSpace = newConfirmedRestock.PercentOpenSpace;
            dbConfirmedRestock.PercentNaturalRegeneration = newConfirmedRestock.PercentNaturalRegeneration;
            dbConfirmedRestock.PercentageOfRestockArea = newConfirmedRestock.PercentageOfRestockArea;
            dbConfirmedRestock.RestockingDensity = newConfirmedRestock.RestockingDensity;
            dbConfirmedRestock.ConfirmedRestockingSpecies = newConfirmedRestock.ConfirmedRestockingSpecies!.ToList();
            dbConfirmedRestock.ConfirmedFellingDetailId = newConfirmedRestock.ConfirmedFellingDetailsId;

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

    //TODO make this method private and refactor the unit test calling it directly to test RetrieveConfirmedFellingAndRestockingDetailModelAsync instead
    public Dictionary<string, string?> GetAmendedFellingDetailProperties(
        ProposedFellingDetail? proposed, 
        ConfirmedFellingDetail confirmed)
    {
        var amended = new Dictionary<string, string?>();
        if (proposed is null)
            return amended;


        AddIfChanged(nameof(proposed.AreaToBeFelled), proposed.AreaToBeFelled, confirmed.AreaToBeFelled);
        AddIfChanged(nameof(proposed.IsPartOfTreePreservationOrder), proposed.IsPartOfTreePreservationOrder, confirmed.IsPartOfTreePreservationOrder);
        AddIfChanged(nameof(proposed.TreePreservationOrderReference), proposed.TreePreservationOrderReference, confirmed.TreePreservationOrderReference);
        AddIfChanged(nameof(proposed.IsWithinConservationArea), proposed.IsWithinConservationArea, confirmed.IsWithinConservationArea);
        AddIfChanged(nameof(proposed.IsRestocking), proposed.IsRestocking, confirmed.IsRestocking);
        AddIfChanged(nameof(proposed.NoRestockingReason), proposed.NoRestockingReason, confirmed.NoRestockingReason);
        AddIfChanged(nameof(proposed.ConservationAreaReference), proposed.ConservationAreaReference, confirmed.ConservationAreaReference);
        AddIfChanged(nameof(proposed.NumberOfTrees), proposed.NumberOfTrees, confirmed.NumberOfTrees);
        AddIfChanged(nameof(proposed.OperationType), proposed.OperationType.GetDisplayName(), confirmed.OperationType.GetDisplayName());
        AddIfChanged(nameof(proposed.TreeMarking), proposed.TreeMarking, confirmed.TreeMarking);
        AddIfChanged(nameof(proposed.EstimatedTotalFellingVolume), proposed.EstimatedTotalFellingVolume, confirmed.EstimatedTotalFellingVolume);

        // Compare FellingSpecies
        var proposedSpecies = proposed.FellingSpecies;
        var confirmedSpecies = confirmed.ConfirmedFellingSpecies;

        if (proposedSpecies is not null)
        {
            if (proposedSpecies.Count != confirmedSpecies.Count ||
                proposedSpecies.Any(ps => confirmedSpecies.All(cs => cs.Species != ps.Species)) ||
                confirmedSpecies.Any(cs => proposedSpecies.All(ps => ps.Species != cs.Species)))
            {
                amended[nameof(proposed.FellingSpecies)] = string.Join(",", proposedSpecies.Select(s =>
                        TreeSpeciesFactory.SpeciesDictionary.TryGetValue(s.Species ?? "", out var speciesModel)
                            ? speciesModel.Name
                            : s.Species
                        ));
            }
        }

        return amended;

        void AddIfChanged<T>(string name, T? proposedValue, T? confirmedValue)
        {
            var valuesDiffer = !EqualityComparer<T>.Default.Equals(proposedValue, confirmedValue);

            if (valuesDiffer)
            {
                if (typeof(T) == typeof(string))
                {
                    var proposedStr = string.IsNullOrWhiteSpace(proposedValue?.ToString()) ? "N/A" : proposedValue?.ToString();
                    var confirmedStr = string.IsNullOrWhiteSpace(confirmedValue?.ToString()) ? "N/A" : confirmedValue?.ToString();

                    if (!string.Equals(proposedStr, confirmedStr, StringComparison.Ordinal))
                        amended[name] = proposedStr;
                }
                else
                {
                    amended[name] = proposedValue == null ? "N/A" : proposedValue.ToString();
                }
            }
        }
    }

    //TODO make this method private and refactor the unit test calling it directly to test RetrieveConfirmedFellingAndRestockingDetailModelAsync instead
    public Dictionary<string, string> GetAmendedRestockingProperties(
        ProposedRestockingDetail? proposed, 
        ConfirmedRestockingDetail? confirmed, 
        Dictionary<Guid, string?> compartmentNames)
    {
        var amended = new Dictionary<string, string>();
        if (proposed == null || confirmed == null)
            return amended;

        AddIfChanged(nameof(proposed.Area), proposed.Area, confirmed.Area);
        AddIfChanged(nameof(proposed.PercentageOfRestockArea), proposed.PercentageOfRestockArea, confirmed.PercentageOfRestockArea);
        AddIfChanged(nameof(proposed.RestockingDensity), proposed.RestockingDensity, confirmed.RestockingDensity);
        AddIfChanged(nameof(proposed.NumberOfTrees), proposed.NumberOfTrees, confirmed.NumberOfTrees);
        AddIfChanged(nameof(proposed.RestockingProposal), proposed.RestockingProposal.GetDisplayName(), confirmed.RestockingProposal.GetDisplayName());

        // Compare RestockingSpecies
        if (proposed.RestockingSpecies != null && confirmed?.ConfirmedRestockingSpecies != null)
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

        if (proposed.PropertyProfileCompartmentId != confirmed!.SubmittedFlaPropertyCompartmentId)
        {
            compartmentNames.TryGetValue(proposed.PropertyProfileCompartmentId, out var proposedCompartmentName);
            compartmentNames.TryGetValue(confirmed.SubmittedFlaPropertyCompartmentId, out var confirmedCompartmentName);
            AddIfChanged("CompartmentName", proposedCompartmentName ?? string.Empty, confirmedCompartmentName ?? string.Empty);
        }

        return amended;

        void AddIfChanged<T>(string name, T? proposedValue, T? confirmedValue)
        {
            var valuesDiffer = !EqualityComparer<T>.Default.Equals(proposedValue, confirmedValue);

            if (valuesDiffer)
            {
                if (typeof(T) == typeof(string))
                {
                    var proposedStr = string.IsNullOrWhiteSpace(proposedValue?.ToString()) ? "N/A" : proposedValue?.ToString();
                    var confirmedStr = string.IsNullOrWhiteSpace(confirmedValue?.ToString()) ? "N/A" : confirmedValue?.ToString();

                    if (!string.Equals(proposedStr, confirmedStr, StringComparison.Ordinal))
                        amended[name] = proposedStr;
                }
                else
                {
                    amended[name] = proposedValue?.ToString() ?? "N/A";
                }
            }
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
        confirmedFellingDetail.IsRestocking = proposedFellingDetail.IsRestocking;
        confirmedFellingDetail.NoRestockingReason = proposedFellingDetail.NoRestockingReason;
        confirmedFellingDetail.ProposedFellingDetailId = proposedFellingDetail.Id;

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
                    ConfirmedFellingDetailId = confirmedFellingDetail.Id,
                    ProposedRestockingDetailId = proposedRestock.Id
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

    private Result CheckUserIsPermittedToAmend(
        FellingLicenceApplication fla,
        Guid userId,
        [CallerMemberName] string? methodName = null,
        bool allowAdminOfficer = false)
    {
        // check the application is in the Woodland Officer review stage

        var currentStatus = fla.GetCurrentStatus();

        List<FellingLicenceStatus> allowedStates = [FellingLicenceStatus.WoodlandOfficerReview];
        List<AssignedUserRole> permittedRoles = [AssignedUserRole.WoodlandOfficer];

        if (allowAdminOfficer)
        {
            allowedStates.Add(FellingLicenceStatus.AdminOfficerReview);
            permittedRoles.Add(AssignedUserRole.AdminOfficer);
        }

        if (allowedStates.Contains(currentStatus) is false)
        {
            _logger.LogError("Application is not in the correct stage in {methodName}, id {id}", methodName, fla.Id);
            return Result.Failure($"Application is not in the correct stage in {methodName}, id {fla.Id}");
        }

        // check that the user is permitted to change details

        if (fla.AssigneeHistories.FirstOrDefault(x =>
                permittedRoles.Contains(x.Role) &&
                x.AssignedUserId == userId &&
                !x.TimestampUnassigned.HasValue) is null)
        {
            _logger.LogError("User {userId} is not permitted to amend felling licence application {applicationId} in {methodName}", userId, fla.Id, methodName);
            return Result.Failure($"User {userId} is not permitted to amend felling licence application {fla.Id} in {methodName}");
        }

        return Result.Success();
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
}