using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using ProposedFellingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel;
using ProposedRestockingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel;

namespace Forestry.Flo.External.Web.Services;

public class ReviewFellingAndRestockingAmendmentsUseCase(
    IUpdateConfirmedFellingAndRestockingDetailsService updateConfirmedFellingAndRestockingDetailsService,
    IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
    IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
    IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplicationService,
    IUserAccountService userAccountService,
    ISendNotifications sendNotifications,
    IGetConfiguredFcAreas getConfiguredFcAreas,
    IOptions<InternalUserSiteOptions> internalUserSiteOptions,
    IAuditService<ReviewFellingAndRestockingAmendmentsUseCase> auditService,
    RequestContext requestContext,
    ILogger<ReviewFellingAndRestockingAmendmentsUseCase> logger)
{
    /// <summary>
    /// Retrieves the set of confirmed felling and restocking details for a given application, along with
    /// the proposed details in order to show differences on the UI, and activity feed items related to the application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve the data for.</param>
    /// <param name="user">The user viewing the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="pageName">The name of the page viewing the data, for the breadcrumbs.</param>
    /// <returns>A <see cref="ReviewAmendmentsViewModel"/> representing the application data,
    /// or <see cref="Result.Failure"/> if an error occurs.</returns>
    public async Task<Result<ReviewAmendmentsViewModel>> GetReviewAmendmentsViewModelAsync(
    Guid applicationId,
    ExternalApplicant user,
    CancellationToken cancellationToken,
    string? pageName = null)
    {
        var (_, isFailure, retrievalResult, error) = await updateConfirmedFellingAndRestockingDetailsService
            .RetrieveConfirmedFellingAndRestockingDetailModelAsync(
                applicationId,
                cancellationToken);

        if (isFailure)
        {
            logger.LogError("Failed to retrieve confirmed felling and restocking details for application {ApplicationId} with error {Error}", applicationId, error);
            return Result.Failure<ReviewAmendmentsViewModel>($"Unable to retrieve felling and restocking details for application id {applicationId}");
        }

        var (_, currentReviewRetrieved, currentReview, currentReviewRetrievalError) = 
            await getWoodlandOfficerReviewService.GetCurrentFellingAndRestockingAmendmentReviewAsync(
                applicationId,
                cancellationToken);

        if (currentReviewRetrieved)
        {
            logger.LogError("Failed to retrieve current amendment review for application {ApplicationId} with error {Error}", applicationId, currentReviewRetrievalError);
            return Result.Failure<ReviewAmendmentsViewModel>($"Unable to retrieve current amendment review for application id {applicationId}");
        }

        if (currentReview.HasNoValue)
        {
            logger.LogWarning("No current amendment review found for application {ApplicationId}", applicationId);
            return Result.Failure<ReviewAmendmentsViewModel>($"No current amendment review found for application id {applicationId}");
        }

        var compartmentIdToSubmittedId =
            retrievalResult.SubmittedFlaPropertyCompartments!.ToDictionary(x => x.CompartmentId, x => x.Id);

        var compartmentGisDictionary =
            retrievalResult.SubmittedFlaPropertyCompartments!.ToDictionary(x => x.Id, x => (x.GISData, x.DisplayName));

        foreach (var confirmedFelling in retrievalResult.ConfirmedFellingAndRestockingDetailModels)
        {
            if (compartmentGisDictionary.ContainsKey(confirmedFelling.SubmittedFlaPropertyCompartmentId) is false)
            {
                logger.LogWarning("No GIS data found for compartment {CompartmentId} in application {ApplicationId}", confirmedFelling.SubmittedFlaPropertyCompartmentId, applicationId);
                return Result.Failure<ReviewAmendmentsViewModel>($"No GIS data found for compartment {confirmedFelling.SubmittedFlaPropertyCompartmentId} in application {applicationId}");
            }

            foreach (var restocking in confirmedFelling.ProposedFellingDetailModels.SelectMany(x =>
                         x.ProposedRestockingDetails))
            {
                if (!compartmentIdToSubmittedId.TryGetValue(restocking.CompartmentId, out var submittedId) ||
                    !compartmentGisDictionary.ContainsKey(submittedId))
                {
                    logger.LogWarning("No GIS data found for compartment {CompartmentId} in application {ApplicationId}", confirmedFelling.SubmittedFlaPropertyCompartmentId, applicationId);
                    return Result.Failure<ReviewAmendmentsViewModel>($"No GIS data found for compartment {confirmedFelling.SubmittedFlaPropertyCompartmentId} in application {applicationId}");
                }
            }
        }

        var model = new AmendedFellingRestockingDetailsModel
        {
            CompartmentIdToSubmittedCompartmentId = compartmentIdToSubmittedId,
            CompartmentGisLookup = compartmentGisDictionary,
            Compartments = retrievalResult.ConfirmedFellingAndRestockingDetailModels.Select(x =>
                    new CompartmentConfirmedFellingRestockingDetailsModel
                    {
                        CompartmentId = x.CompartmentId,
                        CompartmentNumber = x.CompartmentNumber,
                        SubCompartmentName = x.SubCompartmentName,
                        TotalHectares = x.TotalHectares,
                        SubmittedFlaPropertyCompartmentId = x.SubmittedFlaPropertyCompartmentId,
                        NearestTown = x.NearestTown,
                        GISData = compartmentGisDictionary[x.SubmittedFlaPropertyCompartmentId].GISData,

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
                                                            .ToArray() ?? [],
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
                        GISData = compartmentGisDictionary[x.SubmittedFlaPropertyCompartmentId].GISData,
                        ProposedFellingDetails = x.ProposedFellingDetailModels.Select(f =>
                                new ProposedFellingDetailModel()
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
                                            new ProposedRestockingDetailModel()
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
        };

        var viewModel = new ReviewAmendmentsViewModel
        {
            ApplicationId = applicationId,
            AmendedFellingAndRestockingDetails = model,
            AmendmentsSentDate = currentReview.Value.AmendmentsSentDate,
            ResponseReceivedDate = currentReview.Value.ResponseReceivedDate,
            ApplicantAgreed = currentReview.Value.ApplicantAgreed,
            ApplicantDisagreementReason = currentReview.Value.ApplicantDisagreementReason,
            ReviewDeadline = currentReview.Value.ResponseDeadline,
            AmendmentReviewCompleted = currentReview.Value.AmendmentReviewCompleted,
        };

        return Result.Success(viewModel);
    }

    /// <summary>
    /// Completes the amendment review process for a felling licence application by updating the review details,
    /// logging the outcome, and publishing an audit event. If the update fails, an audit event for the failure is also published.
    /// </summary>
    /// <param name="record">The amendment review update record containing the details of the applicant's response.</param>
    /// <param name="userId">The ID of the user completing the review.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> representing the asynchronous operation, containing a <see cref="Result"/> indicating success or failure.
    /// </returns>
    public async Task<Result> CompleteAmendmentReviewAsync(
        FellingAndRestockingAmendmentReviewUpdateRecord record,
        Guid userId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to complete amendment review for application {ApplicationId} by user {UserId}", record.FellingLicenceApplicationId, userId);

        var result = await updateWoodlandOfficerReviewService.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Failed to complete amendment review for application {ApplicationId} with error {Error}", record.FellingLicenceApplicationId, result.Error);

            await auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.ApplicantReviewedAmendmentsFailure, 
                    record.FellingLicenceApplicationId,
                    userId,
                    requestContext,
                    new
                    {
                        result.Error
                    }),
                cancellationToken);

            return result;
        }

        var notificationResult = await SendNotificationOfAmendmentReviewAsync(
            record.FellingLicenceApplicationId, 
            $"{internalUserSiteOptions.Value.BaseUrl}FellingLicenceApplication/ApplicationSummary/{record.FellingLicenceApplicationId}",
            cancellationToken);

        if (notificationResult.IsFailure)
        {
            logger.LogError("Failed to send amendment review notification for application {ApplicationId} with error {Error}", record.FellingLicenceApplicationId, notificationResult.Error);
            // don't fail the whole operation if the email fails
        }

        logger.LogInformation("Successfully completed amendment review for application {ApplicationId}", record.FellingLicenceApplicationId);

        await auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ApplicantReviewedAmendments,
                record.FellingLicenceApplicationId,
                userId,
                requestContext,
                new
                {
                    NotificationSent = notificationResult.IsSuccess
                }),
            cancellationToken);

        return Result.Success();
    }

    private async Task<Result> SendNotificationOfAmendmentReviewAsync(
        Guid applicationId,
        string linkToApplication, 
        CancellationToken cancellationToken)
    {
        var fla = await getFellingLicenceApplicationService.GetApplicationByIdAsync(
            applicationId, 
            cancellationToken);

        if (fla.IsFailure)
        {
            logger.LogError("Failed to retrieve application {ApplicationId} for sending amendment review notification with error {Error}", applicationId, fla.Error);
            return Result.Failure($"Unable to retrieve application id {applicationId} for sending amendment review notification");
        }

        var adminOfficer = fla.Value.AssigneeHistories.FirstOrDefault(x =>
            x.Role is AssignedUserRole.AdminOfficer && 
            x.TimestampUnassigned is null);

        var woodlandOfficer = fla.Value.AssigneeHistories.FirstOrDefault(x =>
            x.Role is AssignedUserRole.WoodlandOfficer && 
            x.TimestampUnassigned is null);

        if (woodlandOfficer is null)
        {
            logger.LogWarning("No woodland officer assigned to application {ApplicationId} for sending amendment review notification", applicationId);
            return Result.Failure($"No woodland officer assigned to application id {applicationId} for sending amendment review notification");
        }

        var idList = new HashSet<Guid>
        {
            woodlandOfficer.AssignedUserId
        };

        if (adminOfficer is not null)
        {
            idList.Add(adminOfficer.AssignedUserId);
        }

        var retrieveUsersResult = await userAccountService.RetrieveUserAccountsByIdsAsync(idList.ToList(), cancellationToken);

        if (retrieveUsersResult.IsFailure)
        {
            logger.LogError("Failed to retrieve user accounts for sending amendment review notification for application {ApplicationId} with error {Error}", applicationId, retrieveUsersResult.Error);
            return Result.Failure($"Unable to retrieve user accounts for sending amendment review notification for application id {applicationId}");
        }

        var adminHubFooter = await
            getConfiguredFcAreas.TryGetAdminHubAddress(fla.Value.AdministrativeRegion!, cancellationToken);

        var woModel = retrieveUsersResult.Value.First(x => x.UserAccountId == woodlandOfficer.AssignedUserId);
        var aoModel = adminOfficer is not null
            ? retrieveUsersResult.Value.First(x => x.UserAccountId == adminOfficer.AssignedUserId)
            : null;

        var dataModel = new ApplicantReviewedAmendmentsDataModel
        {
            Name = woModel.FullName,
            ApplicationReference = fla.Value.ApplicationReference,
            PropertyName = fla.Value.SubmittedFlaPropertyDetail!.Name,
            ApplicationId = applicationId,
            ViewApplicationURL = linkToApplication,
            AdminHubFooter = adminHubFooter
        };

        var copyToModels = new List<NotificationRecipient>();

        if (aoModel is not null)
        {
            copyToModels.Add(new NotificationRecipient(aoModel.Email, aoModel.FullName));
        }

        return await sendNotifications.SendNotificationAsync(
            dataModel,
            NotificationType.ApplicantReviewedAmendments,
            new NotificationRecipient(woModel.Email, woModel.FullName),
            copyToModels.ToArray(),
            cancellationToken: cancellationToken);
    }
}