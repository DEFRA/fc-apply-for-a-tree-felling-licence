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
                        IsRestocking = fellingDetails.IsRestocking ?? fellingDetails.ConfirmedRestockingDetails.Count() > 0,
                        NoRestockingReason = fellingDetails.NoRestockingReason,
                        AmendedProperties =
                            GetAmendedFellingDetailProperties(
                                application.LinkedPropertyProfile!.ProposedFellingDetails!
                                    .FirstOrDefault(x => x.LinkedPropertyProfile.PropertyProfileId == fellingDetails.SubmittedFlaPropertyCompartment.SubmittedFlaPropertyDetail.PropertyProfileId)!,
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
                            CompartmentNumber = restockingDetails.ConfirmedFellingDetail?.SubmittedFlaPropertyCompartment?
                                .SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
                                .Where(x => x.Id == restockingDetails.SubmittedFlaPropertyCompartmentId).FirstOrDefault()?.CompartmentNumber ?? string.Empty,
                            SubCompartmentName = restockingDetails.ConfirmedFellingDetail?.SubmittedFlaPropertyCompartment?
                                .SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?
                                .Where(x => x.Id == restockingDetails.SubmittedFlaPropertyCompartmentId).FirstOrDefault()?.SubCompartmentName ?? string.Empty
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
                        ConfirmedFellingDetailId = confirmedFellingDetail.Id
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

    public Dictionary<string, string> GetAmendedFellingDetailProperties(ProposedFellingDetail proposed, ConfirmedFellingDetail confirmed)
    {
        var amended = new Dictionary<string, string>();

        void AddIfChanged<T>(string name, T? proposedValue, T? confirmedValue)
        {
            if (!EqualityComparer<T>.Default.Equals(proposedValue, confirmedValue))
                amended[name] = proposedValue?.ToString() ?? string.Empty;
        }

        AddIfChanged(nameof(proposed.AreaToBeFelled), proposed.AreaToBeFelled, confirmed.AreaToBeFelled);
        AddIfChanged(nameof(proposed.IsPartOfTreePreservationOrder), proposed.IsPartOfTreePreservationOrder, confirmed.IsPartOfTreePreservationOrder);
        AddIfChanged(nameof(proposed.TreePreservationOrderReference), proposed.TreePreservationOrderReference, confirmed.TreePreservationOrderReference);
        AddIfChanged(nameof(proposed.IsWithinConservationArea), proposed.IsWithinConservationArea, confirmed.IsWithinConservationArea);
        AddIfChanged(nameof(proposed.IsRestocking), proposed.IsRestocking, confirmed.IsRestocking ?? confirmed.ConfirmedRestockingDetails.Count() > 0);
        AddIfChanged(nameof(proposed.ConservationAreaReference), proposed.ConservationAreaReference, confirmed.ConservationAreaReference);
        AddIfChanged(nameof(proposed.NumberOfTrees), GetIntProp(proposed, "NumberOfTrees"), GetIntProp(confirmed, "NumberOfTrees"));
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
        // Compare ProposedRestockingDetails and ConfirmedRestockingDetails
        if (proposed.ProposedRestockingDetails != null && proposed.ProposedRestockingDetails.Count() > 0)
        {
            var proposedRestocking = proposed.ProposedRestockingDetails.OrderBy(x => x.RestockingProposal).ToList();
            var confirmedRestocking = confirmed.ConfirmedRestockingDetails.OrderBy(x => x.RestockingProposal).ToList();
            if (proposedRestocking == null && confirmedRestocking != null && confirmedRestocking.Count > 0)
            {
                amended[nameof(proposed.ProposedRestockingDetails)] = string.Empty;
            }
            else if (proposedRestocking != null && confirmedRestocking != null)
            {
                if (proposedRestocking.Count != confirmedRestocking.Count)
                {
                    amended[nameof(proposed.ProposedRestockingDetails)] = string.Join(",", proposedRestocking.Select(r => r?.ToString() ?? ""));
                }
                else
                {
                    for (int i = 0; i < proposedRestocking.Count; i++)
                    {
                        var propRestock = proposedRestocking[i];
                        var confRestock = confirmedRestocking[i];

                        AddIfChanged($"{nameof(proposed.ProposedRestockingDetails)}[{i}].RestockArea",
                            propRestock.Area, confRestock.Area);
                        AddIfChanged($"{nameof(proposed.ProposedRestockingDetails)}[{i}].PercentageOfRestockArea",
                            propRestock.PercentageOfRestockArea, confRestock.PercentageOfRestockArea);
                        AddIfChanged($"{nameof(proposed.ProposedRestockingDetails)}[{i}].RestockingDensity",
                            propRestock.RestockingDensity, confRestock.RestockingDensity);
                        AddIfChanged($"{nameof(proposed.ProposedRestockingDetails)}[{i}].NumberOfTrees",
                            propRestock.NumberOfTrees, confRestock.NumberOfTrees);
                        AddIfChanged($"{nameof(proposed.ProposedRestockingDetails)}[{i}].RestockingProposal",
                            propRestock.RestockingProposal, confRestock.RestockingProposal);

                        // Compare RestockingSpecies
                        if (propRestock.RestockingSpecies != null && confRestock.ConfirmedRestockingSpecies != null)
                        {
                            var propSpeciesList = propRestock.RestockingSpecies.OrderBy(x => x.Species).ToList();
                            var confSpeciesList = confRestock.ConfirmedRestockingSpecies.OrderBy(x => x.Species).ToList();
                            bool anySpeciesOrPercentDifferent = false;
                            if (propSpeciesList.Count == confSpeciesList.Count)
                            {
                                for (int j = 0; j < propSpeciesList.Count; j++)
                                {
                                    var propSpecies = propSpeciesList[j].Species;
                                    var confSpecies = confSpeciesList[j].Species;
                                    var propPercent = propSpeciesList[j].Percentage;
                                    var confPercent = confSpeciesList[j].Percentage;
                                    if (propSpecies != confSpecies || propPercent != confPercent)
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
                                amended[$"{nameof(proposed.ProposedRestockingDetails)}[{i}].RestockingSpecies"] = string.Join(
                                    ", ",
                                    propSpeciesList
                                        .Where(s =>
                                        {
                                            var deletedProp = s?.GetType().GetProperty("Deleted")?.GetValue(s) as bool?;
                                            var speciesStr = s?.GetType().GetProperty("Species")?.GetValue(s) as string;
                                            return (deletedProp == null || deletedProp == false) && !string.IsNullOrWhiteSpace(speciesStr);
                                        })
                                        .Select(s =>
                                        {
                                            var speciesKey = s?.GetType().GetProperty("Species")?.GetValue(s) as string;
                                            var percent = s?.GetType().GetProperty("Percentage")?.GetValue(s) as double?;
                                            var name = TreeSpeciesFactory.SpeciesDictionary.TryGetValue(speciesKey ?? "", out var speciesModel)
                                                ? speciesModel.Name
                                                : speciesKey;
                                            var percentStr = percent.HasValue ? $"{percent.Value}%" : "";
                                            return string.IsNullOrEmpty(percentStr) ? name : $"{name}: {percentStr}";
                                        })
                                );
                            }
                        }
                    }
                }
            }

        }

        return amended;

        // Helper to get property value by name using reflection
        static T? GetProp<T>(object? obj, string propName)
        {
            if (obj == null) return default;
            var prop = obj.GetType().GetProperty(propName);
            if (prop == null) return default;
            var value = prop.GetValue(obj);
            if (value == null) return default;
            if (value is T tValue) return tValue;
            try
            {
                // Handle numeric conversions (e.g., double to int?)
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return default;
            }
        }

        // Helper to safely get int? from possibly double or int property
        static int? GetIntProp(object? obj, string propName)
        {
            if (obj == null) return null;
            var prop = obj.GetType().GetProperty(propName);
            if (prop == null) return null;
            var value = prop.GetValue(obj);
            if (value == null) return null;
            if (value is int i) return i;
            if (value is double d) return (int?)Convert.ToInt32(d);
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }
    }
}