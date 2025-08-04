using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Newtonsoft.Json;
using IndividualConfirmedFellingRestockingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualConfirmedFellingRestockingDetailModel;

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
    RequestContext requestContext,
    ILogger<ConfirmedFellingAndRestockingDetailsUseCase> logger) 
    : FellingLicenceApplicationUseCaseBase(internalUserAccountService,
        externalUserAccountService,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        agentAuthorityService,
        getConfiguredFcAreasService)
{
    private const string ERROR_NO_CONFIRMED_DETAILS = "No confirmed felling details found";
    private readonly IUpdateConfirmedFellingAndRestockingDetailsService _updateConfirmedFellingAndRestockingDetailsService = Guard.Against.Null(updateConfirmedFellingAndRestockingDetailsService);
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);

    public async Task<Result<ConfirmedFellingRestockingDetailsModel>> GetConfirmedFellingAndRestockingDetailsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken,
        string pageName = null)
    {
        var (_, isFailureSummary, summaryResult) = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (isFailureSummary)
        {
            return Result.Failure<ConfirmedFellingRestockingDetailsModel>(
                $"Unable to retrieve application summary for application id {applicationId}");
        }
        var retryRetrieval = false;

        var fla = await _updateConfirmedFellingAndRestockingDetailsService
            .RetrieveConfirmedFellingAndRestockingDetailModelAsync(
                applicationId,
                cancellationToken);

        if (fla.IsFailure)
        {
            if( fla.Error == ERROR_NO_CONFIRMED_DETAILS)
            {
                var userHasPermissions = summaryResult.AssigneeHistories.Any(x =>
                        x.Role == AssignedUserRole.WoodlandOfficer
                        && x.UserAccount?.Id == user.UserAccountId
                        && x.TimestampUnassigned.HasValue == false)
                        && summaryResult.Status == FellingLicenceStatus.WoodlandOfficerReview;
                if (!userHasPermissions)
                {
                    fla = Result.Success(new CombinedConfirmedFellingAndRestockingDetailRecord
                    (
                        new List<ConfirmedFellingAndRestockingDetailModel>(),
                        false
                    ));
                } else
                {
                    var importResult =
                        await _updateConfirmedFellingAndRestockingDetailsService.ConvertProposedFellingAndRestockingToConfirmedAsync(
                            applicationId,
                            user.UserAccountId!.Value,
                            cancellationToken);

                    if (importResult.IsFailure)
                    {
                        return Result.Failure<ConfirmedFellingRestockingDetailsModel>($"Unable to copy proposed felling details for application {applicationId}, error {importResult.Error}");
                    }
                    retryRetrieval = true;
                }
            }
            else
            {
                return Result.Failure<ConfirmedFellingRestockingDetailsModel>(
                    $"Unable to retrieve felling and restocking details for application id {applicationId}");
            }
        }
        if (retryRetrieval)
        {
            fla = await _updateConfirmedFellingAndRestockingDetailsService
                .RetrieveConfirmedFellingAndRestockingDetailModelAsync(
                    applicationId,
                    cancellationToken);
            if (fla.IsFailure)
            {
                return Result.Failure<ConfirmedFellingRestockingDetailsModel>(
                    $"Unable to retrieve felling and restocking details for application id {applicationId} after import");
            }
        }
        var retrievalResult = fla.Value;
        var result = new ConfirmedFellingRestockingDetailsModel
        {
            FellingLicenceApplicationSummary = summaryResult,
            ApplicationId = applicationId,
            Compartments = retrievalResult.ConfirmedFellingAndRestockingDetailModels.Select(x => new CompartmentConfirmedFellingRestockingDetailsModel
            {
                CompartmentId = x.CompartmentId,
                CompartmentNumber = x.CompartmentNumber,
                SubCompartmentName = x.SubCompartmentName,
                TotalHectares = x.TotalHectares,
                Designation = x.Designation,
                ConfirmedTotalHectares = x.ConfirmedTotalHectares,

                //felling details
                ConfirmedFellingDetails = x.ConfirmedFellingDetailModels.Select(f => new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = f.ConfirmedFellingDetailsId,
                    ProposedFellingDetailsId = f.ProposedFellingDetailsId,
                    AreaToBeFelled = f.AreaToBeFelled,
                    OperationType = f.OperationType,
                    NumberOfTrees = f.NumberOfTrees,
                    TreeMarking = f.TreeMarking,
                    IsTreeMarkingUsed = !string.IsNullOrWhiteSpace(f.TreeMarking),
                    IsPartOfTreePreservationOrder = f.IsPartOfTreePreservationOrder,
                    TreePreservationOrderReference = f.IsPartOfTreePreservationOrder == true ? f.TreePreservationOrderReference : null,
                    IsWithinConservationArea = f.IsWithinConservationArea,
                    ConservationAreaReference = f.IsWithinConservationArea == true ? f.ConservationAreaReference : null,
                    EstimatedTotalFellingVolume = f.EstimatedTotalFellingVolume,
                    IsRestocking = f.IsRestocking,
                    NoRestockingReason = f.NoRestockingReason,
                    AmendedProperties = f.AmendedProperties,
                    ConfirmedFellingSpecies = f.ConfirmedFellingSpecies.Select(static s => new ConfirmedFellingSpeciesModel
                    {
                        Species = s.Species,
                        Deleted = false,
                        Id = s.Id
                    }).ToArray(),
                    //restocking details
                    ConfirmedRestockingDetails = f.ConfirmedRestockingDetailModels.Any() ? f.ConfirmedRestockingDetailModels.Select(r => new ConfirmedRestockingDetailViewModel
                    {
                        ConfirmedRestockingDetailsId = r.ConfirmedRestockingDetailsId,
                        RestockArea = r.Area,
                        PercentOpenSpace = r.PercentOpenSpace,
                        RestockingProposal = r.RestockingProposal,
                        RestockingDensity = r.RestockingDensity,
                        NumberOfTrees = r.NumberOfTrees,
                        PercentNaturalRegeneration = r.PercentNaturalRegeneration,
                        RestockingCompartmentId = r.CompartmentId,
                        RestockingCompartmentNumber = r.CompartmentNumber,
                        AmendedProperties = r.AmendedProperties,
                        ConfirmedRestockingSpecies = r.ConfirmedRestockingSpecies.Select(s => new ConfirmedRestockingSpeciesModel
                        {
                            Deleted = false,
                            Id = s.Id,
                            Percentage = (int?)s.Percentage!,
                            Species = s.Species
                        }).ToArray() ?? Array.Empty<ConfirmedRestockingSpeciesModel>(),
                        ConfirmedFellingDetailsId = r.ConfirmedFellingDetailsId,
                        OperationType = f.OperationType
                    }).ToArray() : [],
                }).ToArray(),
            })
            .OrderBy(x => x.CompartmentNumber)
            .ThenBy(x => x.SubCompartmentName)
            .ToArray(),
            ConfirmedFellingAndRestockingComplete = retrievalResult.ConfirmedFellingAndRestockingComplete
        };

        SetBreadcrumbs(result, pageName);

        return Result.Success(result);
    }


    public async Task<Result<ConfirmedFellingRestockingDetailsModel>> SaveConfirmedFellingAndRestockingDetailsAsync(
        ConfirmedFellingRestockingDetailsModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);

        var confirmedFellingAndRestockingModels = PrepareFellingAndRestockingDetailForSaveAsync(model);

        var saveResult = await _updateConfirmedFellingAndRestockingDetailsService.SaveChangesToConfirmedFellingAndRestockingAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            confirmedFellingAndRestockingModels,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            return saveResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        var woReviewUpdateResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            model.ConfirmedFellingAndRestockingComplete,
            cancellationToken);

        if (woReviewUpdateResult.IsFailure)
        {
            return woReviewUpdateResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        return await GetConfirmedFellingAndRestockingDetailsAsync(model.ApplicationId, user, cancellationToken);
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
    /// Imports proposed felling and restocking details into confirmed details for a given application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="user">The internal user performing the import.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the import operation.
    /// </returns>
    public async Task<Result> ImportProposedFellingAndRestockingAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);

        var importResult =
            await _updateConfirmedFellingAndRestockingDetailsService.ConvertProposedFellingAndRestockingToConfirmedAsync(
                applicationId,
                user.UserAccountId!.Value,
                cancellationToken);

        if (importResult.IsFailure)
        {
            return Result.Failure($"Unable to copy proposed felling details for application {applicationId}, error {importResult.Error}");
        }

        await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            applicationId,
            user.UserAccountId!.Value,
            false,
            cancellationToken);

        return Result.Success();
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
            logger.LogError("Failed to save confirmed felling details for application {ApplicationId} with error {Error}", model.ApplicationId, saveResult.Error);
            await transaction.RollbackAsync(cancellationToken);
            await AuditConfirmedFellingDetailsUpdateFailureAsync(model.ApplicationId, user, saveResult.Error, cancellationToken);
            return saveResult.ConvertFailure<ConfirmedFellingRestockingDetailsModel>();
        }

        var woReviewUpdateResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            model.ConfirmedRestockingComplete,
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

    private static List<ConfirmedFellingAndRestockingDetailModel> PrepareFellingAndRestockingDetailForSaveAsync(ConfirmedFellingRestockingDetailsModel model)
    {
        Guard.Against.Null(model);

        return model.Compartments.Select(x => new ConfirmedFellingAndRestockingDetailModel
        {
            CompartmentId = x.CompartmentId!.Value,
            CompartmentNumber = x.CompartmentNumber,
            ConfirmedTotalHectares = x.ConfirmedTotalHectares,
            Designation = x.Designation,
            TotalHectares = x.TotalHectares,
            SubCompartmentName = x.SubCompartmentName,

            ConfirmedFellingDetailModels = x.ConfirmedFellingDetails.Select(f => new ConfirmedFellingDetailModel
            {
                AreaToBeFelled = f.AreaToBeFelled ?? 0,
                ConfirmedFellingDetailsId = f.ConfirmedFellingDetailsId,
                IsPartOfTreePreservationOrder = f.IsPartOfTreePreservationOrder is true,
                TreePreservationOrderReference = f.TreePreservationOrderReference,
                IsWithinConservationArea = f.IsWithinConservationArea is true,
                ConservationAreaReference = f.ConservationAreaReference,
                NumberOfTrees = f.NumberOfTrees,
                OperationType = f.OperationType ?? FellingOperationType.None,
                IsTreeMarkingUsed = f.IsTreeMarkingUsed,
                TreeMarking = f.TreeMarking,
                IsRestocking = f.IsRestocking,
                NoRestockingReason = f.NoRestockingReason,
                EstimatedTotalFellingVolume = f.EstimatedTotalFellingVolume ?? 0,
                ConfirmedFellingSpecies = f.ConfirmedFellingSpecies.Select(s => new ConfirmedFellingSpecies
                {
                    ConfirmedFellingDetailId = f.ConfirmedFellingDetailsId,
                    Species = s.Species ?? string.Empty
                }).ToList(),
                ConfirmedRestockingDetailModels = f.ConfirmedRestockingDetails.Where(x => x.RestockingProposal is null || x.RestockingProposal != TypeOfProposal.None).Select(r => new ConfirmedRestockingDetailModel
                {
                    Area = r.RestockArea,
                    ConfirmedRestockingDetailsId = r.ConfirmedRestockingDetailsId,
                    PercentNaturalRegeneration = r.PercentNaturalRegeneration,
                    PercentOpenSpace = r.PercentOpenSpace,
                    CompartmentId = r.RestockingCompartmentId,
                    RestockingDensity = r.RestockingDensity,
                    NumberOfTrees = r.NumberOfTrees,
                    RestockingProposal = r.RestockingProposal ?? TypeOfProposal.None,
                    ConfirmedRestockingSpecies = r.ConfirmedRestockingSpecies.Select(s => new ConfirmedRestockingSpecies
                    {
                        ConfirmedRestockingDetailsId = r.ConfirmedRestockingDetailsId,
                        Percentage = s.Percentage,
                        Species = s.Species
                    }).ToList()
                }).ToList()
            })
        }).ToList();
    }

    private static Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualConfirmedFellingRestockingDetailModel PrepareFellingDetailForSaveAsync(AmendConfirmedFellingDetailsViewModel model)
    {
        Guard.Against.Null(model);
        var fellingDetails = model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails;

        return new Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualConfirmedFellingRestockingDetailModel
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

    private static Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualConfirmedRestockingDetailModel PrepareRestockingDetailForSaveAsync(AmendConfirmedRestockingDetailsViewModel model)
    {
        Guard.Against.Null(model);
        var restockingDetails = model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails;

        // Determine compartment id/number based on proposal type
        Guid compartmentId;
        string? compartmentNumber;
        if (restockingDetails.RestockingProposal == TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees || restockingDetails.RestockingProposal == TypeOfProposal.PlantAnAlternativeArea)
        {
            compartmentId = restockingDetails.RestockingCompartmentId;
            compartmentNumber = restockingDetails.RestockingCompartmentNumber;
        }
        else
        {
            compartmentId = model.ConfirmedFellingRestockingDetails.CompartmentId!.Value;
            compartmentNumber = model.ConfirmedFellingRestockingDetails.CompartmentNumber;
        }

        return new Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualConfirmedRestockingDetailModel
        {
            CompartmentId = compartmentId,
            CompartmentNumber = compartmentNumber,
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
                CompartmentNumber = compartmentNumber
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

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string pageName)
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
}