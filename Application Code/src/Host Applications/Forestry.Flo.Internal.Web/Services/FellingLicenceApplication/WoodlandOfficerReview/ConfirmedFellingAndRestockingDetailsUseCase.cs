using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Newtonsoft.Json;
using ProposedFellingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel;
using ProposedRestockingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class ConfirmedFellingAndRestockingDetailsUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    IGetFellingLicenceApplicationForInternalUsers getFellingLicenceService,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IUpdateConfirmedFellingAndRestockingDetailsService updateConfirmedFellingAndRestockingDetailsService,
    IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
    IAgentAuthorityService agentAuthorityService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    IAuditService<ConfirmedFellingAndRestockingDetailsUseCase> auditService,
    IActivityFeedItemProvider activityFeedItemProvider,
    RequestContext requestContext,
    ILogger<ConfirmedFellingAndRestockingDetailsUseCase> logger) 
    : FellingLicenceApplicationUseCaseBase(internalUserAccountService,
        externalUserAccountService,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        agentAuthorityService,
        getConfiguredFcAreasService)
{
    private readonly IUpdateConfirmedFellingAndRestockingDetailsService _updateConfirmedFellingAndRestockingDetailsService = Guard.Against.Null(updateConfirmedFellingAndRestockingDetailsService);
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);

    /// <summary>
    /// Retrieves the set of confirmed felling and restocking details for a given application, along with
    /// the proposed details in order to show differences on the UI, and activity feed items related to the application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve the data for.</param>
    /// <param name="user">The user viewing the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="pageName">The name of the page viewing the data, for the breadcrumbs.</param>
    /// <returns>A <see cref="ConfirmedFellingRestockingDetailsModel"/> representing the application data,
    /// or <see cref="Result.Failure"/> if an error occurs.</returns>
    public async Task<Result<ConfirmedFellingRestockingDetailsModel>> GetConfirmedFellingAndRestockingDetailsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken,
        string? pageName = null)
    {
        var (_, isFailureSummary, summaryResult, summaryError) = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (isFailureSummary)
        {
            logger.LogError("Failed to retrieve application summary for application {ApplicationId} with error {Error}", applicationId, summaryError);
            return Result.Failure<ConfirmedFellingRestockingDetailsModel>(
                $"Unable to retrieve application summary for application id {applicationId}");
        }
        var (_, isFailure, retrievalResult, error) = await _updateConfirmedFellingAndRestockingDetailsService
            .RetrieveConfirmedFellingAndRestockingDetailModelAsync(
                applicationId,
                cancellationToken);

        if (isFailure)
        {
            logger.LogError("Failed to retrieve confirmed felling and restocking details for application {ApplicationId} with error {Error}", applicationId, error);
            return Result.Failure<ConfirmedFellingRestockingDetailsModel>($"Unable to retrieve felling and restocking details for application id {applicationId}");
        }

        var activityFeedItems = await activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            new ActivityFeedItemProviderModel
            {
                FellingLicenceId = applicationId,
                FellingLicenceReference = summaryResult.ApplicationReference,
                ItemTypes = [ActivityFeedItemType.WoodlandOfficerReviewComment],
            },
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            logger.LogError("Failed to retrieve activity feed items for application {ApplicationId} with error {Error}", applicationId, activityFeedItems.Error);
            return Result.Failure<ConfirmedFellingRestockingDetailsModel>($"Unable to retrieve activity feed items for application id {applicationId}");
        }

        var result = new ConfirmedFellingRestockingDetailsModel
        {
            FellingLicenceApplicationSummary = summaryResult,
            ActivityFeedItems = activityFeedItems.Value.ToList(),
            ApplicationId = applicationId,
            PotentialRestockingCompartments = retrievalResult.SubmittedFlaPropertyCompartments
                .Select(x => new PotentialRestockingCompartments(x.Id, Math.Round(x.TotalHectares!.Value, 2), x.DisplayName, x.CompartmentId))
                .ToList(),
            CanCurrentUserAmend = 
                summaryResult.StatusHistories.MaxBy(x => x.Created)?.Status is FellingLicenceStatus.WoodlandOfficerReview &&
                summaryResult.AssigneeHistories.Any(y => 
                    y.Role is AssignedUserRole.WoodlandOfficer && 
                    y.UserAccount!.Id == user.UserAccountId && 
                    y.TimestampUnassigned is null) &&
                !retrievalResult.ConfirmedFellingAndRestockingComplete,
            Compartments = retrievalResult.ConfirmedFellingAndRestockingDetailModels.Select(x =>
                    new CompartmentConfirmedFellingRestockingDetailsModel
                    {
                        CompartmentId = x.CompartmentId,
                        CompartmentNumber = x.CompartmentNumber,
                        SubCompartmentName = x.SubCompartmentName,
                        TotalHectares = x.TotalHectares,
                        SubmittedFlaPropertyCompartmentId = x.SubmittedFlaPropertyCompartmentId,
                        Designation = x.Designation,
                        ConfirmedTotalHectares = x.ConfirmedTotalHectares,
                        NearestTown = x.NearestTown,
                        GISData = summaryResult.DetailsList?.FirstOrDefault(y => y.CompartmentId == x.CompartmentId)?.GISData,

                        //felling details
                        ConfirmedFellingDetails = x.ConfirmedFellingDetailModels.Select(f =>
                                new ConfirmedFellingDetailViewModel
                                {
                                    ConfirmedFellingDetailsId = f.ConfirmedFellingDetailsId,
                                    ProposedFellingDetailsId = f.ProposedFellingDetailsId,
                                    AreaToBeFelled = f.AreaToBeFelled,
                                    OperationType = f.OperationType,
                                    NumberOfTrees = f.NumberOfTrees,
                                    TreeMarking = f.TreeMarking,
                                    IsTreeMarkingUsed = !string.IsNullOrWhiteSpace(f.TreeMarking),
                                    IsPartOfTreePreservationOrder = f.IsPartOfTreePreservationOrder,
                                    TreePreservationOrderReference = f.IsPartOfTreePreservationOrder == true
                                        ? f.TreePreservationOrderReference
                                        : null,
                                    IsWithinConservationArea = f.IsWithinConservationArea,
                                    ConservationAreaReference = f.IsWithinConservationArea == true
                                        ? f.ConservationAreaReference
                                        : null,
                                    EstimatedTotalFellingVolume = f.EstimatedTotalFellingVolume,
                                    IsRestocking = f.IsRestocking,
                                    NoRestockingReason = f.NoRestockingReason,
                                    AmendedProperties = f.AmendedProperties,
                                    ConfirmedFellingSpecies = f.ConfirmedFellingSpecies.Select(static s =>
                                            new ConfirmedFellingSpeciesModel
                                            {
                                                Species = s.Species,
                                                Deleted = false,
                                                Id = s.Id
                                            })
                                        .ToArray(),
                                    //restocking details
                                    ConfirmedRestockingDetails = f.ConfirmedRestockingDetailModels.Any()
                                        ? f.ConfirmedRestockingDetailModels.Select(r =>
                                                new ConfirmedRestockingDetailViewModel
                                                {
                                                    ConfirmedRestockingDetailsId = r.ConfirmedRestockingDetailsId,
                                                    ProposedRestockingDetailsId = r.ProposedRestockingDetailsId,
                                                    RestockArea = r.Area,
                                                    PercentOpenSpace = r.PercentOpenSpace,
                                                    RestockingProposal = r.RestockingProposal,
                                                    RestockingDensity = r.RestockingDensity,
                                                    NumberOfTrees = r.NumberOfTrees,
                                                    PercentNaturalRegeneration = r.PercentNaturalRegeneration,
                                                    RestockingCompartmentId = r.CompartmentId,
                                                    RestockingCompartmentNumber = r.CompartmentNumber,
                                                    RestockingCompartmentTotalHectares = r.RestockingCompartmentTotalHectares,
                                                    AmendedProperties = r.AmendedProperties,
                                                    ConfirmedRestockingSpecies = r.ConfirmedRestockingSpecies.Select(
                                                                s => new ConfirmedRestockingSpeciesModel
                                                                {
                                                                    Deleted = false,
                                                                    Id = s.Id,
                                                                    Percentage = (int?)s.Percentage!,
                                                                    Species = s.Species
                                                                })
                                                            .ToArray() ??
                                                        Array.Empty<ConfirmedRestockingSpeciesModel>(),
                                                    ConfirmedFellingDetailsId = r.ConfirmedFellingDetailsId,
                                                    OperationType = f.OperationType
                                                })
                                            .ToArray()
                                        : [],
                                })
                            .ToArray(),
                    })
                .OrderBy(x => x.CompartmentNumber)
                .ThenBy(x => x.SubCompartmentName)
                .ToArray(),
            ProposedFellingDetails = retrievalResult.ConfirmedFellingAndRestockingDetailModels.Select(x =>
                    new CompartmentProposedFellingRestockingDetailsModel
                    {
                        CompartmentId = x.CompartmentId,
                        SubmittedFlaPropertyCompartmentId = x.SubmittedFlaPropertyCompartmentId,
                        CompartmentNumber = x.CompartmentNumber,
                        SubCompartmentName = x.SubCompartmentName,
                        TotalHectares = x.TotalHectares,
                        Designation = x.Designation,
                        ProposedFellingDetails = x.ProposedFellingDetailModels.Select(f =>
                                new ProposedFellingDetailModel
                                {
                                    Id = f.Id,
                                    AreaToBeFelled = f.AreaToBeFelled,
                                    OperationType = f.OperationType,
                                    NumberOfTrees = f.NumberOfTrees,
                                    TreeMarking = f.TreeMarking,
                                    IsTreeMarkingUsed = !string.IsNullOrWhiteSpace(f.TreeMarking),
                                    IsPartOfTreePreservationOrder = f.IsPartOfTreePreservationOrder,
                                    TreePreservationOrderReference = f.IsPartOfTreePreservationOrder == true
                                        ? f.TreePreservationOrderReference
                                        : null,
                                    IsWithinConservationArea = f.IsWithinConservationArea,
                                    ConservationAreaReference = f.IsWithinConservationArea == true
                                        ? f.ConservationAreaReference
                                        : null,
                                    EstimatedTotalFellingVolume = f.EstimatedTotalFellingVolume,
                                    IsRestocking = f.IsRestocking,
                                    NoRestockingReason = f.NoRestockingReason,
                                    Species = f.Species,
                                    ProposedRestockingDetails = f.ProposedRestockingDetails.Select(r =>
                                            new ProposedRestockingDetailModel
                                            {
                                                Id = r.Id,
                                                RestockingProposal = r.RestockingProposal,
                                                RestockingDensity = r.RestockingDensity,
                                                NumberOfTrees = r.NumberOfTrees,
                                                Species = r.Species,
                                                CompartmentId = r.CompartmentId,
                                                CompartmentNumber = r.CompartmentNumber,
                                                SubCompartmentName = r.SubCompartmentName,
                                                OperationType = f.OperationType,
                                                Area = r.Area,
                                                CompartmentTotalHectares = r.CompartmentTotalHectares,
                                                PercentageOfRestockArea = r.PercentageOfRestockArea
                                            })
                                        .ToList(),
                                })
                            .ToArray(),
                    })
                .OrderBy(x => x.CompartmentNumber)
                .ThenBy(x => x.SubCompartmentName)
                .ToArray(),
            ConfirmedFellingAndRestockingComplete = retrievalResult.ConfirmedFellingAndRestockingComplete,
            // todo: set this based on amendment acceptance state implemented in FLOV2-2361
            RelevantButton = retrievalResult.ConfirmedFellingAndRestockingComplete
                ? RelevantFellingAndRestockingButton.None
                : RelevantFellingAndRestockingButton.ConfirmFellingAndRestocking,
        };

        SetBreadcrumbs(result, pageName);

        return Result.Success(result);
    }

    /// <summary>
    /// Saves amendments made to confirmed felling details for a specific compartment during woodland officer review.
    /// </summary>
    /// <param name="model">The view model containing amended confirmed felling details.</param>
    /// <param name="user">The internal user performing the save operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    public async Task<Result> SaveConfirmedFellingDetailsAsync(
        AmendConfirmedFellingDetailsViewModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        logger.LogDebug("Saving confirmed felling details for application {ApplicationId}", model.ApplicationId);

        // reset restocking selection if the operation type does not require restocking
        if (model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.OperationType is 
            FellingOperationType.Thinning)
        {
            model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.NoRestockingReason = null;
            model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsRestocking = null;
        }

        await using var transaction = await _updateConfirmedFellingAndRestockingDetailsService.BeginTransactionAsync(cancellationToken);

        var confirmedFellingDetailsModel = PrepareFellingDetailForSaveAsync(model);

        var saveResult = await _updateConfirmedFellingAndRestockingDetailsService.SaveChangesToConfirmedFellingDetailsAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            confirmedFellingDetailsModel,
            model.Species,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            logger.LogError( "Failed to save confirmed felling details for application {ApplicationId} with error {Error}", model.ApplicationId, saveResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(model.ApplicationId, user, saveResult.Error, cancellationToken);
            return saveResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        var woReviewUpdateResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            model.ConfirmedFellingAndRestockingComplete,
            cancellationToken);

        if (woReviewUpdateResult.IsFailure)
        {
            logger.LogError("Failed to update woodland officer review for application {ApplicationId} with error {Error}", model.ApplicationId, woReviewUpdateResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(model.ApplicationId, user, woReviewUpdateResult.Error, cancellationToken);
            return woReviewUpdateResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        logger.LogInformation("Successfully saved confirmed felling details for application {ApplicationId}", model.ApplicationId);
        await transaction.CommitAsync(cancellationToken);
        await AuditConfirmedFellingDetailsUpdateAsync(model.ApplicationId, user, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Adds a new confirmed felling details to a specific compartment during woodland officer review.
    /// </summary>
    /// <param name="model">The view model containing the new confirmed felling details.</param>
    /// <param name="user">The internal user performing the save operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    public async Task<Result> SaveConfirmedFellingDetailsAsync(
        AddNewConfirmedFellingDetailsViewModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        logger.LogDebug("Adding new confirmed felling details for application {ApplicationId}", model.ApplicationId);

        // reset restocking selection if the operation type does not require restocking
        if (model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.OperationType is
            FellingOperationType.Thinning)
        {
            model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.NoRestockingReason = null;
            model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsRestocking = null;
        }

        await using var transaction = await _updateConfirmedFellingAndRestockingDetailsService.BeginTransactionAsync(cancellationToken);

        var transformedModel = PrepareFellingDetailForSaveAsync(model);

        var saveResult = await _updateConfirmedFellingAndRestockingDetailsService.AddNewConfirmedFellingDetailsAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            transformedModel,
            model.Species,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            logger.LogError("Failed to save new confirmed felling details for application {ApplicationId} with error {Error}", model.ApplicationId, saveResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(model.ApplicationId, user, saveResult.Error, cancellationToken);
            return saveResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        var woReviewUpdateResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            false,
            cancellationToken);

        if (woReviewUpdateResult.IsFailure)
        {
            logger.LogError("Failed to update woodland officer review for application {ApplicationId} with error {Error}", model.ApplicationId, woReviewUpdateResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(model.ApplicationId, user, woReviewUpdateResult.Error, cancellationToken);
            return woReviewUpdateResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        logger.LogInformation("Successfully saved confirmed felling details for application {ApplicationId}", model.ApplicationId);
        await transaction.CommitAsync(cancellationToken);
        await AuditConfirmedFellingDetailsUpdateAsync(model.ApplicationId, user, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Retrieves a list of selectable compartments for a given felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a list of <see cref="SelectableCompartment"/> if successful,
    /// or a failure result if retrieval was unsuccessful.
    /// </returns>
    public async Task<Result<SelectFellingCompartmentModel>> GetSelectableFellingCompartmentsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var summary = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (summary.IsFailure)
        {
            logger.LogError("Failed to retrieve application summary for application {ApplicationId} with error {Error}", applicationId, summary.Error);
            return Result.Failure<SelectFellingCompartmentModel>(
                $"Unable to retrieve application summary for application {applicationId}");
        }

        var compartments = await getFellingLicenceService.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(
            applicationId,
            cancellationToken);

        if (compartments.IsFailure)
        {
            logger.LogError("Failed to retrieve compartments for application {ApplicationId} with error {Error}", applicationId, compartments.Error);
            return Result.Failure<SelectFellingCompartmentModel>(
                $"Unable to retrieve compartments for application {applicationId}");
        }

        var gisCompartment = compartments.Value.Select(c => new
        {
            c.Id,
            c.GISData,
            c.DisplayName,
            Selected = false
        }).ToList();

        var model = new SelectFellingCompartmentModel
        {
            ApplicationId = applicationId,
            SelectableCompartments = compartments.Value
                .Select(x => new SelectableCompartment(x.CompartmentId, x.CompartmentNumber))
                .ToList(),
            FellingLicenceApplicationSummary = summary.Value,
            GisData = JsonConvert.SerializeObject(gisCompartment),
        };

        SetBreadcrumbs(model, "Select felling compartment");
        return model;
    }

    /// <summary>
    /// Reverts amendments made to a confirmed felling detail for a specific application and proposed felling details.
    /// This operation reimports the proposed felling details, updates the woodland officer review, and commits the transaction.
    /// If any step fails, the transaction is rolled back and the error is returned.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="proposedFellingDetailsId">The unique identifier of the proposed felling details to revert.</param>
    /// <param name="user">The internal user performing the revert operation.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the revert operation.
    /// </returns>
    public async Task<Result> RevertConfirmedFellingDetailAmendmentsAsync(
        Guid applicationId,
        Guid proposedFellingDetailsId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        logger.LogDebug("Reverting deleted confirmed felling detail amendments for application {ApplicationId}, proposed felling details id {ProposedFellingDetailsId}",
            applicationId, proposedFellingDetailsId);

        await using var transaction = await _updateConfirmedFellingAndRestockingDetailsService.BeginTransactionAsync(cancellationToken);
        try
        {
            var revertResult = await _updateConfirmedFellingAndRestockingDetailsService
                .RevertConfirmedFellingDetailAmendmentsAsync(
                    applicationId,
                    proposedFellingDetailsId,
                    user.UserAccountId!.Value,
                    cancellationToken);

            if (revertResult.IsFailure)
            {
                await auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.RevertConfirmedFellingDetailsFailure,
                        proposedFellingDetailsId,
                        user.UserAccountId,
                        requestContext,
                        new
                        {
                            Error = revertResult.Error,
                        }),
                    cancellationToken);

                logger.LogError("Failed to revert deleted confirmed felling detail amendments for application {ApplicationId} with error {Error}", applicationId, revertResult.Error);
                await transaction.RollbackAsync(cancellationToken);
                return revertResult;
            }

            var updateEntityResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                user.UserAccountId!.Value,
                false,
                cancellationToken);

            if (updateEntityResult.IsFailure)
            {
                await auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.RevertConfirmedFellingDetailsFailure,
                        proposedFellingDetailsId,
                        user.UserAccountId,
                        requestContext,
                        new
                        {
                            Error = updateEntityResult.Error,
                        }),
                    cancellationToken);

                logger.LogError("Failed to update woodland officer review for application {ApplicationId} with error {Error}", applicationId, updateEntityResult.Error);
                await transaction.RollbackAsync(cancellationToken);
                return updateEntityResult;
            }

            await auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.RevertConfirmedFellingDetails,
                    proposedFellingDetailsId,
                    user.UserAccountId,
                    requestContext,
                    new { }),
                cancellationToken);

            logger.LogInformation("Successfully reverted deleted confirmed felling detail amendments for application {ApplicationId}", applicationId);
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.RevertConfirmedFellingDetailsFailure,
                    proposedFellingDetailsId,
                    user.UserAccountId,
                    requestContext,
                    new
                    {
                        Error = ex.Message,
                    }),
                cancellationToken);

            logger.LogError(ex, "An error occurred while reverting deleted confirmed felling detail amendments for application {ApplicationId}", applicationId);
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure($"An error occurred while reverting deleted confirmed felling detail amendments: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a confirmed felling detail from an application, including all associated restocking details.
    /// </summary>
    /// <param name="applicationId">The id of the application containing the confirmed felling detail.</param>
    /// <param name="confirmedFellingDetailId">The id of the confirmed felling detail to delete.</param>
    /// <param name="user">The internal user performing the deletion.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the confirmed felling detail has been deleted.</returns>
    public async Task<Result> DeleteConfirmedFellingDetailAsync(
        Guid applicationId,
        Guid confirmedFellingDetailId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(applicationId);
        ArgumentNullException.ThrowIfNull(confirmedFellingDetailId);
        ArgumentNullException.ThrowIfNull(user);

        logger.LogDebug("Attempting to delete confirmed felling detail {ConfirmedFellingDetailId} for application {ApplicationId}", confirmedFellingDetailId, applicationId);

        await using var transaction = await _updateConfirmedFellingAndRestockingDetailsService.BeginTransactionAsync(cancellationToken);

        var result = await _updateConfirmedFellingAndRestockingDetailsService.DeleteConfirmedFellingDetailAsync(
            applicationId,
            confirmedFellingDetailId,
            user.UserAccountId!.Value,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Unable to delete confirmed felling detail {ConfirmedFellingDetailId} for application {ApplicationId}: {Error}", confirmedFellingDetailId, applicationId, result.Error);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(applicationId, user, result.Error, cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure($"Unable to delete confirmed felling detail for application {applicationId}, error {result.Error}");
        }

        var woReviewUpdateResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            applicationId,
            user.UserAccountId!.Value,
            false,
            cancellationToken);

        if (woReviewUpdateResult.IsFailure)
        {
            logger.LogError("Failed to update woodland officer review for application {ApplicationId} with error {Error}", applicationId, woReviewUpdateResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(applicationId, user, woReviewUpdateResult.Error, cancellationToken);
            return woReviewUpdateResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        logger.LogInformation("Successfully deleted confirmed felling detail {ConfirmedFellingDetailId} for application {ApplicationId}", confirmedFellingDetailId, applicationId);
        await transaction.CommitAsync(cancellationToken);
        await AuditConfirmedFellingDetailsUpdateAsync(applicationId, user, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Deletes a confirmed restocking detail from an application.
    /// </summary>
    /// <param name="applicationId">The id of the application containing the confirmed restocking detail.</param>
    /// <param name="confirmedRestockingDetailId">The id of the confirmed restocking detail to delete.</param>
    /// <param name="user">The internal user performing the deletion.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the confirmed restocking detail has been deleted.</returns>
    public async Task<Result> DeleteConfirmedRestockingDetailAsync(
        Guid applicationId,
        Guid confirmedRestockingDetailId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(applicationId);
        ArgumentNullException.ThrowIfNull(confirmedRestockingDetailId);
        ArgumentNullException.ThrowIfNull(user);

        logger.LogDebug("Attempting to delete confirmed restocking detail {ConfirmedRestockingDetailId} for application {ApplicationId}", confirmedRestockingDetailId, applicationId);

        await using var transaction = await _updateConfirmedFellingAndRestockingDetailsService.BeginTransactionAsync(cancellationToken);

        var result = await _updateConfirmedFellingAndRestockingDetailsService.DeleteConfirmedRestockingDetailAsync(
            applicationId,
            confirmedRestockingDetailId,
            user.UserAccountId!.Value,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Unable to delete confirmed restocking detail {ConfirmedRestockingDetailId} for application {ApplicationId}: {Error}", confirmedRestockingDetailId, applicationId, result.Error);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(applicationId, user, result.Error, cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure($"Unable to delete confirmed restocking detail for application {applicationId}, error {result.Error}");
        }

        var woReviewUpdateResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            applicationId,
            user.UserAccountId!.Value,
            false,
            cancellationToken);

        if (woReviewUpdateResult.IsFailure)
        {
            logger.LogError("Failed to update woodland officer review for application {ApplicationId} with error {Error}", applicationId, woReviewUpdateResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(applicationId, user, woReviewUpdateResult.Error, cancellationToken);
            return woReviewUpdateResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        logger.LogInformation("Successfully deleted confirmed restocking detail {ConfirmedRestockingDetailId} for application {ApplicationId}", confirmedRestockingDetailId, applicationId);
        await transaction.CommitAsync(cancellationToken);
        await AuditConfirmedFellingDetailsUpdateAsync(applicationId, user, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Saves an amended restocking details model back to the data store.
    /// </summary>
    /// <param name="model">A <see cref="AmendConfirmedRestockingDetailsViewModel"/> model of the restocking data to save.</param>
    /// <param name="user">The user amending the data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the data saved successfully.</returns>
    public async Task<Result> SaveConfirmedRestockingDetailsAsync(
        AmendConfirmedRestockingDetailsViewModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        logger.LogDebug("Saving confirmed restocking details for application {ApplicationId}", model.ApplicationId);

        await using var transaction = await _updateConfirmedFellingAndRestockingDetailsService.BeginTransactionAsync(cancellationToken);

        var confirmedRestockingDetailsModel = PrepareRestockingDetailForSaveAsync(model);

        var saveResult = await _updateConfirmedFellingAndRestockingDetailsService.SaveChangesToConfirmedRestockingDetailsAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            confirmedRestockingDetailsModel,
            model.Species,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            logger.LogError("Failed to save confirmed restocking details for application {ApplicationId} with error {Error}", model.ApplicationId, saveResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedRestockingDetailsUpdateFailureAsync(model.ApplicationId, user, saveResult.Error, cancellationToken);
            return saveResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        var woReviewUpdateResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            false,
            cancellationToken);

        if (woReviewUpdateResult.IsFailure)
        {
            logger.LogError("Failed to update woodland officer review for application {ApplicationId} with error {Error}", model.ApplicationId, woReviewUpdateResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedRestockingDetailsUpdateFailureAsync(model.ApplicationId, user, woReviewUpdateResult.Error, cancellationToken);
            return woReviewUpdateResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        logger.LogInformation("Successfully saved confirmed felling details for application {ApplicationId}", model.ApplicationId);
        await transaction.CommitAsync(cancellationToken);
        await AuditConfirmedRestockingDetailsUpdateAsync(model.ApplicationId, user, cancellationToken);
        return Result.Success();
    }

    private static IndividualFellingRestockingDetailModel PrepareFellingDetailForSaveAsync(AmendConfirmedFellingDetailsViewModel model)
    {
        Guard.Against.Null(model);
        var fellingDetails = model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails;

        return new IndividualFellingRestockingDetailModel
        {
            CompartmentId = model.ConfirmedFellingRestockingDetails.CompartmentId!.Value,
            CompartmentNumber = model.ConfirmedFellingRestockingDetails.CompartmentNumber,
            Designation = model.ConfirmedFellingRestockingDetails.Designation,
            TotalHectares = model.ConfirmedFellingRestockingDetails.TotalHectares,
            SubCompartmentName = model.ConfirmedFellingRestockingDetails.SubCompartmentName,
            ConfirmedFellingDetailModel = new ConfirmedFellingDetailModel
            {
                AreaToBeFelled = model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.AreaToBeFelled ?? 0,
                ConfirmedFellingDetailsId = fellingDetails.ConfirmedFellingDetailsId,
                IsPartOfTreePreservationOrder = fellingDetails.IsPartOfTreePreservationOrder is true,
                TreePreservationOrderReference = fellingDetails.TreePreservationOrderReference,
                IsWithinConservationArea = fellingDetails.IsWithinConservationArea is true,
                ConservationAreaReference = fellingDetails.ConservationAreaReference,
                NumberOfTrees = fellingDetails.NumberOfTrees,
                OperationType = fellingDetails.OperationType ?? FellingOperationType.None,
                IsTreeMarkingUsed = fellingDetails.IsTreeMarkingUsed,
                TreeMarking = fellingDetails.IsTreeMarkingUsed??false ? fellingDetails.TreeMarking : string.Empty,
                EstimatedTotalFellingVolume = fellingDetails.EstimatedTotalFellingVolume ?? 0,
                IsRestocking = fellingDetails.IsRestocking,
                NoRestockingReason = fellingDetails.NoRestockingReason,
                ConfirmedFellingSpecies = fellingDetails.ConfirmedFellingSpecies.Select(s => new ConfirmedFellingSpecies
                {
                    ConfirmedFellingDetailId = fellingDetails.ConfirmedFellingDetailsId,
                    Species = s.Species ?? string.Empty
                }).ToList(),
            }
        };
    }

    private static IndividualRestockingDetailModel PrepareRestockingDetailForSaveAsync(AmendConfirmedRestockingDetailsViewModel model)
    {
        Guard.Against.Null(model);
        var restockingDetails = model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails;

        Guid compartmentId;
        if (restockingDetails.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType())
        {
            compartmentId = model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.RestockingCompartmentId;
        }
        else
        {
            compartmentId = model.ConfirmedFellingRestockingDetails.SubmittedFlaPropertyCompartmentId!.Value;
        }

        return new IndividualRestockingDetailModel
        {
            SubmittedFlaPropertyCompartmentId = compartmentId,
            Designation = model.ConfirmedFellingRestockingDetails.Designation,
            TotalHectares = model.ConfirmedFellingRestockingDetails.TotalHectares,
            SubCompartmentName = model.ConfirmedFellingRestockingDetails.SubCompartmentName,
            ConfirmedRestockingDetailModel = new ConfirmedRestockingDetailModel
            {
                Area = restockingDetails.RestockArea ?? 0,
                RestockingDensity = restockingDetails.RestockingDensity,
                ConfirmedFellingDetailsId = restockingDetails.ConfirmedFellingDetailsId,
                NumberOfTrees = restockingDetails.NumberOfTrees,
                RestockingProposal = restockingDetails.RestockingProposal ?? TypeOfProposal.None,
                ConfirmedRestockingSpecies = restockingDetails.ConfirmedRestockingSpecies.Select(s => new ConfirmedRestockingSpecies
                {
                    ConfirmedRestockingDetailsId = restockingDetails.ConfirmedRestockingDetailsId,
                    Species = s.Species ?? string.Empty,
                    Percentage = s.Percentage
                }).ToList(),
                ConfirmedRestockingDetailsId = restockingDetails.ConfirmedRestockingDetailsId,
                CompartmentId = compartmentId,
            }
        };
    }

    private static NewConfirmedFellingDetailWithCompartmentId PrepareFellingDetailForSaveAsync(
        AddNewConfirmedFellingDetailsViewModel model)
    {
        Guard.Against.Null(model);
        var fellingDetails = model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails;

        return new NewConfirmedFellingDetailWithCompartmentId
        (
            model.ConfirmedFellingRestockingDetails.CompartmentId!.Value,
            new ConfirmedFellingDetailModel
            {
                AreaToBeFelled = model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.AreaToBeFelled ?? 0,
                IsPartOfTreePreservationOrder = fellingDetails.IsPartOfTreePreservationOrder is true,
                TreePreservationOrderReference = fellingDetails.TreePreservationOrderReference,
                IsWithinConservationArea = fellingDetails.IsWithinConservationArea is true,
                ConservationAreaReference = fellingDetails.ConservationAreaReference,
                NumberOfTrees = fellingDetails.NumberOfTrees,
                OperationType = fellingDetails.OperationType ?? FellingOperationType.None,
                TreeMarking = fellingDetails.TreeMarking,
                EstimatedTotalFellingVolume = fellingDetails.EstimatedTotalFellingVolume ?? 0,
                IsRestocking = fellingDetails.IsRestocking,
                NoRestockingReason = fellingDetails.NoRestockingReason,
                ConfirmedFellingSpecies = fellingDetails.ConfirmedFellingSpecies.Select(s => new ConfirmedFellingSpecies
                {
                    Species = s.Species ?? string.Empty
                }).ToList()
            });
    }

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string? pageName)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Home", "Home", "Index", null),
            new BreadCrumb(model.FellingLicenceApplicationSummary.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary.Id.ToString()),
            new BreadCrumb("Woodland officer review", "WoodlandOfficerReview", "Index", model.FellingLicenceApplicationSummary.Id.ToString()),
        };

        if (pageName is not null)
        {
            breadCrumbs.Add(new BreadCrumb("Confirmed felling and restocking", "WoodlandOfficerReview", "ConfirmedFellingAndRestocking", model.FellingLicenceApplicationSummary.Id.ToString()));
            model.Breadcrumbs = new BreadcrumbsModel
            {
                Breadcrumbs = breadCrumbs,
                CurrentPage = pageName
            };
        }
        else
        {
            model.Breadcrumbs = new BreadcrumbsModel
            {
                Breadcrumbs = breadCrumbs,
                CurrentPage = "Confirmed felling and restocking"
            };
        }
    }

    private async Task AuditConfirmedFellingDetailsUpdateAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        await auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateConfirmedFellingDetails,
                applicationId,
                user.UserAccountId,
                requestContext,
                new {}), 
            cancellationToken);
    }

    private async Task AuditConfirmedFellingDetailsUpdateFailureAsync(
        Guid applicationId,
        InternalUser user,
        string error,
        CancellationToken cancellationToken)
    {
        await auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateConfirmedFellingDetailsFailure,
                applicationId,
                user.UserAccountId,
                requestContext,
                new
                {
                    Error = error
                }),
            cancellationToken);
    }

    private async Task AuditConfirmedRestockingDetailsUpdateAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        await auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateConfirmedRestockingDetails,
                applicationId,
                user.UserAccountId,
                requestContext,
                new {}), 
            cancellationToken);
    }

    private async Task AuditConfirmedRestockingDetailsUpdateFailureAsync(
        Guid applicationId,
        InternalUser user,
        string error,
        CancellationToken cancellationToken)
    {
        await auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateConfirmedRestockingDetailsFailure,
                applicationId,
                user.UserAccountId,
                requestContext,
                new
                {
                    Error = error
                }),
            cancellationToken);
    }
}