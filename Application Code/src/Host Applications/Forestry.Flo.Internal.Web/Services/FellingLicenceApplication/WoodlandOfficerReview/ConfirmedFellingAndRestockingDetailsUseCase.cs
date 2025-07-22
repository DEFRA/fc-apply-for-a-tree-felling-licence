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
using IndividualConfirmedFellingRestockingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualConfirmedFellingRestockingDetailModel;
using NewConfirmedFellingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.NewConfirmedFellingDetailModel;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class ConfirmedFellingAndRestockingDetailsUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
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
        string pageName = "Confirmed felling and restocking")
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
                    AreaToBeFelled = f.AreaToBeFelled,
                    OperationType = f.OperationType,
                    NumberOfTrees = f.NumberOfTrees,
                    TreeMarking = f.TreeMarking,
                    IsTreeMarkingUsed = f.TreeMarking is not null,
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
                    ConfirmedRestockingDetails = f.ConfirmedRestockingDetailModels.Any() ? f.ConfirmedRestockingDetailModels.Select(static r => new ConfirmedRestockingDetailViewModel
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
                        ConfirmedRestockingSpecies = r.ConfirmedRestockingSpecies.Select(s => new ConfirmedRestockingSpeciesModel
                        {
                            Deleted = false,
                            Id = s.Id,
                            Percentage = (int)s.Percentage!,
                            Species = s.Species
                        }).ToArray() ?? Array.Empty<ConfirmedRestockingSpeciesModel>(),
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

    private static IndividualConfirmedFellingRestockingDetailModel PrepareFellingDetailForSaveAsync(AmendConfirmedFellingDetailsViewModel model)
    {
        Guard.Against.Null(model);
        var fellingDetails = model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails;

        return new IndividualConfirmedFellingRestockingDetailModel
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
                TreeMarking = fellingDetails.TreeMarking,
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

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string pageName)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Home", "Home", "Index", null),
            new BreadCrumb(model.FellingLicenceApplicationSummary.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary.Id.ToString()),
            new BreadCrumb("Woodland officer review", "WoodlandOfficerReview", "Index", model.FellingLicenceApplicationSummary.Id.ToString())
        };
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = pageName
        };
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