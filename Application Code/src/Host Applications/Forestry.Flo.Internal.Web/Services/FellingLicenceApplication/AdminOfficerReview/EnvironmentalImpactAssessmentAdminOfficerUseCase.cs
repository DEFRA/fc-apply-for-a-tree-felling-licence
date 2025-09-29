using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;
using Result = CSharpFunctionalExtensions.Result;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

public class EnvironmentalImpactAssessmentAdminOfficerUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    ILogger<EnvironmentalImpactAssessmentAdminOfficerUseCase> logger,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IUpdateAdminOfficerReviewService updateAdminOfficerReviewService,
    IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
    IAgentAuthorityService agentAuthorityService,
    IAuditService<AdminOfficerReviewUseCaseBase> auditService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    IUpdateFellingLicenceApplication updateFellingLicenceApplication,
    ISendNotifications sendNotifications,
    IOptions<EiaOptions> eiaOptions,
    IClock clock,
    RequestContext requestContext)
    : AdminOfficerReviewUseCaseBase(internalUserAccountService,
        externalUserAccountService,
        logger,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        updateAdminOfficerReviewService,
        getFellingLicenceApplication,
        auditService,
        agentAuthorityService,
        getConfiguredFcAreasService,
        requestContext)
{

    /// <summary>
    /// Retrieves the Environmental Impact Assessment (EIA) details for a specified felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result{EnvironmentalImpactAssessmentModel}"/> containing the EIA model if successful,
    /// or a failure result with an error message if the application or EIA cannot be retrieved.
    /// </returns>
    public async Task<Result<EnvironmentalImpactAssessmentModel>> GetEnvironmentalImpactAssessmentAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var fellingLicence = await getFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);

        if (fellingLicence.IsFailure)
        {
            logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<EnvironmentalImpactAssessmentModel>("Unable to retrieve felling licence application");
        }

        var (_, isFailure, eia, error) = await getFellingLicenceApplication.GetEnvironmentalImpactAssessmentAsync(applicationId, cancellationToken);

        if (isFailure)
        {
            logger.LogError("Unable to retrieve EIA for felling licence application {ApplicationId}, error: {Error}", applicationId, error);
            return Result.Failure<EnvironmentalImpactAssessmentModel>("Unable to retrieve EIA for felling licence application");
        }

        var model = new EnvironmentalImpactAssessmentModel
        {
            Id = eia.Id,
            FellingLicenceApplicationId = eia.FellingLicenceApplicationId,
            HasApplicationBeenCompleted = eia.HasApplicationBeenCompleted,
            HasApplicationBeenSent = eia.HasApplicationBeenSent,
            HasTheEiaFormBeenReceived = eia.HasTheEiaFormBeenReceived,
            AreAttachedFormsCorrect = eia.AreAttachedFormsCorrect,
            EiaTrackerReferenceNumber = eia.EiaTrackerReferenceNumber,
            ApplicationReference = fellingLicence.Value.ApplicationReference,
            EiaRequests = eia.EiaRequests.Select(r => new EnvironmentalImpactAssessmentRequestModel
            {
                Id = r.Id,
                EnvironmentalImpactAssessmentId = r.EnvironmentalImpactAssessmentId,
                RequestType = r.RequestType,
                RequestingUserId = r.RequestingUserId,
                NotificationTime = r.NotificationTime
            }).ToList(),
            EiaDocuments = ModelMapping.ToDocumentModelList(
                fellingLicence.Value!.Documents!.Where(x => x.Purpose is DocumentPurpose.EiaAttachment && x.DeletionTimestamp is null).ToList()),
        };

        return model;
    }

    public Task<Result<FellingLicenceApplicationSummaryModel>> GetSummaryModel(Guid applicationId,
        CancellationToken cancellationToken) =>
        GetFellingLicenceDetailsAsync(applicationId, cancellationToken);


    /// <summary>
    /// Confirms whether the attached EIA forms are correct for the specified application.
    /// Updates the Environmental Impact Assessment record as an Admin Officer.
    /// If the forms are not correct, sends a notification to the applicant and rolls back the transaction on failure.
    /// </summary>
    /// <param name="viewModel">The view model containing details about the EIA forms and their correctness.</param>
    /// <param name="performingUserId">The ID of the user performing the confirmation.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the confirmation and notification process.
    /// </returns>
    public async Task<Result> ConfirmAttachedEiaFormsAreCorrectAsync(
        EiaWithFormsPresentViewModel viewModel,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        if (viewModel.AreTheFormsCorrect.HasNoValue())
        {
            logger.LogWarning("EIA forms correctness not specified for application {ApplicationId}",
                viewModel.ApplicationId);

            await AuditAdminOfficerReviewUpdateFailureAsync(
                viewModel.ApplicationId,
                "Must specify if the EIA forms are correct",
                performingUserId,
                cancellationToken);
            await AuditEiaUpdateFailureAsync(
                viewModel.ApplicationId,
                "Must specify if the EIA forms are correct",
                performingUserId,
                cancellationToken);

            return Result.Failure("Must specify if the EIA forms are correct");
        }

        await using var transaction = await updateFellingLicenceApplication.BeginTransactionAsync(cancellationToken);

        var result = await updateFellingLicenceApplication.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
            viewModel.ApplicationId,
            new EnvironmentalImpactAssessmentAdminOfficerRecord
            {
                AreAttachedFormsCorrect = viewModel.AreTheFormsCorrect!.Value,
                EiaTrackerReferenceNumber = string.IsNullOrWhiteSpace(viewModel.EiaTrackerReferenceNumber)
                    ? Maybe<string>.None 
                    : viewModel.EiaTrackerReferenceNumber
            },
            cancellationToken);

        if (result.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogError("Failed to save EIA forms confirmation for application {ApplicationId}. Error: {Error}",
                viewModel.ApplicationId, result.Error);

            await AuditAdminOfficerReviewUpdateFailureAsync(
                viewModel.ApplicationId,
                result.Error,
                performingUserId,
                cancellationToken);
            await AuditEiaUpdateFailureAsync(
                viewModel.ApplicationId,
                result.Error,
                performingUserId,
                cancellationToken);

            return result;
        }

        if (viewModel.AreTheFormsCorrect is false)
        {
            var notificationResult = await PrepareAndSendEiaReminderAsync(
                RequestType.MissingDocuments, 
                viewModel.ApplicationId, 
                cancellationToken);

            if (notificationResult.IsFailure)
            {
                logger.LogError(
                    "Failed to send notification for complete EIA forms for application {ApplicationId}. Error: {Error}",
                    viewModel.ApplicationId,
                    notificationResult.Error);

                await AuditAdminOfficerReviewUpdateFailureAsync(
                    viewModel.ApplicationId,
                    notificationResult.Error,
                    performingUserId,
                    cancellationToken);
                await AuditEiaUpdateFailureAsync( 
                    viewModel.ApplicationId,
                    notificationResult.Error,
                    performingUserId,
                    cancellationToken);

                await transaction.RollbackAsync(cancellationToken);

                return Result.Failure("Unable to send notification for incomplete EIA forms");
            }
        }

        await transaction.CommitAsync(cancellationToken);

        await AuditAdminOfficerReviewUpdateAsync(
            viewModel.ApplicationId,
            false,
            performingUserId,
            cancellationToken);
        await AuditEiaUpdateAsync(
            viewModel.ApplicationId,
            performingUserId,
            cancellationToken);

        logger.LogInformation("Successfully saved EIA forms confirmation for application {ApplicationId}",
            viewModel.ApplicationId);
        return Result.Success();
    }

    /// <summary>
    /// Confirms whether the EIA forms have been received for the specified application.
    /// Updates the Environmental Impact Assessment record as an Admin Officer and audits the operation.
    /// If the forms have not been received, triggers a notification and handles transaction rollback on failure.
    /// </summary>
    /// <param name="viewModel">The view model containing EIA form receipt details.</param>
    /// <param name="performingUserId">The ID of the user performing the confirmation.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the confirmation and notification process.
    /// </returns>
    public async Task<Result> ConfirmEiaFormsHaveBeenReceivedAsync(
        EiaWithFormsAbsentViewModel viewModel,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        if (viewModel.HaveTheFormsBeenReceived.HasNoValue())
        {
            logger.LogWarning("EIA forms correctness not specified for application {ApplicationId}",
                viewModel.ApplicationId);
            await AuditAdminOfficerReviewUpdateFailureAsync(
                viewModel.ApplicationId,
                "Must specify if the EIA forms are correct",
                performingUserId,
                cancellationToken);
            await AuditEiaUpdateFailureAsync(
                viewModel.ApplicationId,
                "Must specify if the EIA forms are correct",
                performingUserId,
                cancellationToken);
            return Result.Failure("Must specify if the EIA forms are correct");
        }

        await using var transaction = await updateFellingLicenceApplication.BeginTransactionAsync(cancellationToken);

        var result = await updateFellingLicenceApplication.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
            viewModel.ApplicationId,
            new EnvironmentalImpactAssessmentAdminOfficerRecord
            {
                HasTheEiaFormBeenReceived = viewModel.HaveTheFormsBeenReceived!.Value,
                EiaTrackerReferenceNumber = string.IsNullOrWhiteSpace(viewModel.EiaTrackerReferenceNumber)
                    ? Maybe<string>.None
                    : viewModel.EiaTrackerReferenceNumber
            },
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Failed to save EIA received confirmation for application {ApplicationId}. Error: {Error}",
                viewModel.ApplicationId, result.Error);

            await transaction.RollbackAsync(cancellationToken);

            await AuditAdminOfficerReviewUpdateFailureAsync(
                viewModel.ApplicationId,
                result.Error,
                performingUserId,
                cancellationToken);
            await AuditEiaUpdateFailureAsync(
                viewModel.ApplicationId,
                result.Error,
                performingUserId,
                cancellationToken);

            return result;
        }

        if (viewModel.HaveTheFormsBeenReceived is false)
        {
            var notificationResult = await PrepareAndSendEiaReminderAsync(
                RequestType.Reminder,
                viewModel.ApplicationId,
                cancellationToken);

            if (notificationResult.IsFailure)
            {
                logger.LogError(
                    "Failed to send EIA reminder notification for application {ApplicationId}. Error: {Error}",
                    viewModel.ApplicationId,
                    notificationResult.Error);

                await transaction.RollbackAsync(cancellationToken);

                await AuditAdminOfficerReviewUpdateFailureAsync(
                    viewModel.ApplicationId,
                    notificationResult.Error,
                    performingUserId,
                    cancellationToken);
                await AuditEiaUpdateFailureAsync(
                    viewModel.ApplicationId,
                    notificationResult.Error,
                    performingUserId,
                    cancellationToken);

                return Result.Failure("Unable to send EIA reminder notification");
            }
        }

        await transaction.CommitAsync(cancellationToken);

        await AuditAdminOfficerReviewUpdateAsync(
            viewModel.ApplicationId,
            false,
            performingUserId,
            cancellationToken);
        await AuditEiaUpdateAsync( 
            viewModel.ApplicationId,
            performingUserId,
            cancellationToken);

        logger.LogInformation("Successfully saved EIA received confirmation for application {ApplicationId}",
                viewModel.ApplicationId);
        return Result.Success();
    }

    public Task<Result<List<UserAccountModel>>> RetrieveUserAccountsByIdsAsync(
        List<Guid> ids,
        CancellationToken cancellationToken) =>
        InternalUserAccountService.RetrieveUserAccountsByIdsAsync(ids, cancellationToken);

    private async Task<Result> PrepareAndSendEiaReminderAsync(
        RequestType type,
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        var (_, isFailure, application, error) = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);

        if (isFailure)
        {
            logger.LogError("Unable to retrieve felling licence application {ApplicationId} for EIA reminder notification, error: {Error}", applicationId, error);
            return Result.Failure<EnvironmentalImpactAssessmentReminderDataModel>("Unable to retrieve felling licence application");
        }

        var applicantId = application.AssigneeHistories
            .First(x => x.Role is AssignedUserRole.Author)
            .AssignedUserId;

        var applicantRetrieval =
            await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(applicantId, cancellationToken);

        if (applicantRetrieval.IsFailure)
        {
            logger.LogError("Unable to retrieve applicant user account {UserId} for EIA reminder notification, error: {Error}", applicantId, applicantRetrieval.Error);
            return Result.Failure<EnvironmentalImpactAssessmentReminderDataModel>("Unable to retrieve applicant user account");
        }

        var adminOfficerId = application.AssigneeHistories
            .First(x => x.Role is AssignedUserRole.AdminOfficer && x.TimestampUnassigned is null)
            .AssignedUserId;

        var internalUserRetrieval =
            await InternalUserAccountService.GetUserAccountAsync(adminOfficerId, cancellationToken);

        if (internalUserRetrieval.HasNoValue){
            logger.LogError("Unable to retrieve admin officer user account {UserId} for EIA reminder notification", adminOfficerId);
            return Result.Failure<EnvironmentalImpactAssessmentReminderDataModel>("Unable to retrieve admin officer user account");
        }

        var dataModel = new EnvironmentalImpactAssessmentReminderDataModel
        {
            ApplicationReference = application.ApplicationReference,
            PropertyName = application.SubmittedFlaPropertyDetail!.Name,
            ApplicationSubmissionTime = application.StatusHistories
                .First(x => x.Status is FellingLicenceStatus.Submitted).Created.CreateFormattedDate(),
            RecipientName = applicantRetrieval.Value.FullName(),
            SenderName = internalUserRetrieval.Value.FullName(),
            ApplicationFormUri = eiaOptions.Value.EiaApplicationExternalUri,
            ContactEmail = eiaOptions.Value.EiaContactEmail,
            ContactNumber = eiaOptions.Value.EiaContactPhone,
            ApplicationId = applicationId
        };

        var notificationResult = await sendNotifications.SendNotificationAsync(
            dataModel,
            type switch
            {
                RequestType.MissingDocuments => NotificationType.EiaReminderMissingDocuments,
                RequestType.Reminder => NotificationType.EiaReminderToSendDocuments,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported notification type: {type}")
            },
            new NotificationRecipient(applicantRetrieval.Value.Email, applicantRetrieval.Value.FullName()),
            cancellationToken: cancellationToken
        );

        var storeRequestHistoryResult =
            await UpdateAdminOfficerReviewService.AddEnvironmentalImpactAssessmentRequestHistoryAsync(
                new EnvironmentalImpactAssessmentRequestHistoryRecord
                {
                    ApplicationId = applicationId,
                    RequestingUserId = adminOfficerId,
                    NotificationTime = clock.GetCurrentInstant().ToDateTimeUtc(),
                    RequestType = type
                },
                cancellationToken);

        if (storeRequestHistoryResult.IsFailure)
        {
            logger.LogError(
                "Failed to store EIA request history for application {ApplicationId}. Error: {Error}",
                applicationId,
                storeRequestHistoryResult.Error);
            return Result.Failure("Unable to store EIA request history");
        }

        logger.LogDebug(
            "Successfully sent EIA reminder notification for application {ApplicationId}",
            applicationId);
        return notificationResult;
    }


    private async Task AuditEiaUpdateAsync(
        Guid applicationId,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        await AuditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.AdminOfficerCompleteEiaCheck,
                    applicationId,
                    performingUserId,
                    RequestContext,
                    new
                    { }),
                cancellationToken)
            .ConfigureAwait(false);
    }

    protected async Task AuditEiaUpdateFailureAsync(
        Guid applicationId,
        string? error,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        await AuditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.AdminOfficerCompleteEiaCheckFailure,
                    applicationId,
                    performingUserId,
                    RequestContext,
                    new
                    {
                        Error = error,
                    }),
                cancellationToken)
            .ConfigureAwait(false);
    }
}