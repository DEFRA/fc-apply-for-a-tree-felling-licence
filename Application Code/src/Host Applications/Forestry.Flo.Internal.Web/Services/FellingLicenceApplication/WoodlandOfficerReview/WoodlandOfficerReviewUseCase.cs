using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Internal.Web.Services.MassTransit.Messages;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class WoodlandOfficerReviewUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
    IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
    IActivityFeedItemProvider activityFeedItemProvider,
    IAuditService<WoodlandOfficerReviewUseCase> auditService,
    ISendNotifications emailService,
    IAgentAuthorityService agentAuthorityService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    IClock clock,
    IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
    RequestContext requestContext,
    IBus busControl,
    IOptions<FellingLicenceApplicationOptions> fellingLicenceApplicationOptions,
    ILogger<WoodlandOfficerReviewUseCase> logger)
    : FellingLicenceApplicationUseCaseBase(internalUserAccountService,
        externalUserAccountService,
        fellingLicenceApplicationRepository,
        woodlandOwnerService,
        agentAuthorityService,
        getConfiguredFcAreasService,
        woodlandOfficerReviewSubStatusService), IWoodlandOfficerReviewUseCase
{
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
    private readonly IClock _clock = Guard.Against.Null(clock);
    private readonly IAuditService<WoodlandOfficerReviewUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly ISendNotifications _emailService = Guard.Against.Null(emailService);
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
    private readonly IActivityFeedItemProvider _activityFeedItemProvider = Guard.Against.Null(activityFeedItemProvider);
    private readonly FellingLicenceApplicationOptions _fellingLicenceApplicationOptions = Guard.Against.Null(fellingLicenceApplicationOptions?.Value);

    /// <inheritdoc />
    public async Task<Result<WoodlandOfficerReviewModel>> WoodlandOfficerReviewAsync(
        Guid applicationId,
        InternalUser user,
        string hostingPage,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to retrieve woodland officer review status for application with id {ApplicationId}", applicationId);

        var woodlandOfficerReviewStatus = await _getWoodlandOfficerReviewService.GetWoodlandOfficerReviewStatusAsync(
            applicationId, cancellationToken);

        if (woodlandOfficerReviewStatus.IsFailure)
        {
            logger.LogError("Failed to retrieve woodland officer review details with error {Error}", woodlandOfficerReviewStatus.Error);
            return woodlandOfficerReviewStatus.ConvertFailure<WoodlandOfficerReviewModel>();
        }

        var application = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (application.IsFailure)
        {
            logger.LogError("Failed to retrieve application summary with error {Error}", application.Error);
            return application.ConvertFailure<WoodlandOfficerReviewModel>();
        }

        var providerModel = new ActivityFeedItemProviderModel()
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = application.Value.ApplicationReference,
            ItemTypes = new[] { ActivityFeedItemType.WoodlandOfficerReviewComment },
        };

        var activityFeedItems = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            logger.LogError("Failed to retrieve activity feed items with error {Error}", application.Error);
            return activityFeedItems.ConvertFailure<WoodlandOfficerReviewModel>();
        }

        var recommendedDuration = woodlandOfficerReviewStatus.Value.RecommendedLicenceDuration;
        if (recommendedDuration == null)
        {
            var defaultRecommendedLicenceDuration = Enum.IsDefined(typeof(RecommendedLicenceDuration), _fellingLicenceApplicationOptions.DefaultLicenseDuration)
                ? (RecommendedLicenceDuration)_fellingLicenceApplicationOptions.DefaultLicenseDuration
                : RecommendedLicenceDuration.None; // or another fallback if None is not appropriate

            if (application.Value.IsForTenYearLicence)
            {
                defaultRecommendedLicenceDuration = RecommendedLicenceDuration.TenYear;
            }

            recommendedDuration = defaultRecommendedLicenceDuration;
        }

        var result = new WoodlandOfficerReviewModel
        {
            ApplicationId = applicationId,
            FellingLicenceApplicationSummary = application.Value,
            AssignedFieldManager = application.Value?.AssigneeHistories.FirstOrDefault(
                x => x.Role == AssignedUserRole.FieldManager && x.TimestampUnassigned.HasValue == false)?.UserAccount?.FullName,
            WoodlandOfficerReviewCommentsFeed = new ActivityFeedModel
            {
                ActivityFeedTitle = "Woodland officer review comments",
                ApplicationId = applicationId,
                NewCaseNoteType = CaseNoteType.WoodlandOfficerReviewComment,
                DefaultCaseNoteFilter = CaseNoteType.WoodlandOfficerReviewComment,
                ActivityFeedItemModels = activityFeedItems.Value,
                HostingPage = hostingPage,
                ShowFilters = false
            },
            WoodlandOfficerReviewTaskListStates = woodlandOfficerReviewStatus.Value.WoodlandOfficerReviewTaskListStates,
            RecommendedLicenceDuration = recommendedDuration,
            RecommendationForDecisionPublicRegister = woodlandOfficerReviewStatus.Value.RecommendationForDecisionPublicRegister,
            RecommendationForDecisionPublicRegisterReason = woodlandOfficerReviewStatus.Value.RecommendationForDecisionPublicRegisterReason,
            SupplementaryPoints = woodlandOfficerReviewStatus.Value.SupplementaryPoints
        };
        result.WoodlandOfficerReviewCommentsFeed.ShowAddCaseNote = result.Editable(user);

        SetBreadcrumbs(result);

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> CompleteWoodlandOfficerReviewAsync(
        Guid applicationId, 
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool? recommendationForDecisionPublicRegister,
        string recommendationForPublicRegisterReason,
        string internalLinkToApplication,
        string? supplementaryPoints,
        InternalUser user, 
        CancellationToken cancellationToken)
    {
        const string notificationsError = "The Woodland Officer Review has been completed but the system was unable to send notifications";

        logger.LogDebug("Attempting to complete the woodland officer review for application with id {ApplicationId}", applicationId);

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var result = await _updateWoodlandOfficerReviewService.CompleteWoodlandOfficerReviewAsync(
            applicationId,
            user.UserAccountId!.Value,
            recommendedLicenceDuration,
            recommendationForDecisionPublicRegister,
            recommendationForPublicRegisterReason,
            supplementaryPoints,
            now,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Failed to complete woodland officer review for application with id {ApplicationId} with error {Error}", applicationId, result.Error);
            await AppendAuditFailure(
                applicationId, 
                user.UserAccountId!.Value, 
                new { Error = result.Error },
                cancellationToken);
            return Result.Failure("Could not complete Woodland Officer Review");
        }

        await busControl.Publish(
            new GenerateSubmittedPdfPreviewMessage(
                user.UserAccountId!.Value,
                applicationId),
            cancellationToken);

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ConfirmWoodlandOfficerReview,
                applicationId,
                user.UserAccountId!.Value,
                _requestContext,
                new
                {
                    CompletedDate = now,
                    RecommendedLicenceDuration = recommendedLicenceDuration,
                    RecommendationForDecisionPublicRegister = recommendationForDecisionPublicRegister
                }),
            cancellationToken);

        var applicant = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(result.Value.ApplicantId, cancellationToken);

        if (applicant.IsFailure)
        {
            logger.LogError("Unable to determine applicant for notification");
            await AppendAuditFailure(applicationId, user.UserAccountId.Value, new { Error = applicant.Error }, cancellationToken);
            return Result.Failure(notificationsError);
        }

        var woodlandOfficer = await InternalUserAccountService.GetUserAccountAsync(user.UserAccountId!.Value, cancellationToken);
        if (woodlandOfficer.HasNoValue)
        {
            logger.LogError("Unable to find a user with the id of {Id}", user.UserAccountId!.Value);
            await AppendAuditFailure(applicationId, user.UserAccountId.Value,
                new { Error = "Unable to find woodland officer to notify" }, cancellationToken);
            return Result.Failure(notificationsError);
        }

        var fieldManager = await InternalUserAccountService.GetUserAccountAsync(result.Value.FieldManagerId, cancellationToken);
        if (fieldManager.HasNoValue)
        {
            logger.LogError("Unable to find a user with the id of {Id}", result.Value.FieldManagerId);
            await AppendAuditFailure(applicationId, user.UserAccountId.Value,
                new { Error = "Unable to find field manager to notify" }, cancellationToken);
            return Result.Failure(notificationsError);
        }

        var sendNotificationsResult = await SendReviewConfirmationNotifications(
            result.Value.ApplicationReference,
            applicant.Value,
            fieldManager.Value,
            woodlandOfficer.Value,
            applicationId,
            internalLinkToApplication,
            result.Value.AdminHubName,
            cancellationToken);

        if (sendNotificationsResult.IsFailure)
        {
            return Result.Failure(notificationsError);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> CompleteLarchCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        var result = await _updateWoodlandOfficerReviewService.UpdateLarchCheckAsync(
            applicationId,
            performingUserId,
            cancellationToken);

        if (result.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReviewFailure,
                    applicationId,
                    performingUserId,
                    _requestContext),
                cancellationToken);
            return result;
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                applicationId,
                performingUserId,
                _requestContext),
            cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<Result> CompleteConfirmedFellingAndRestockingDetailsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        var result = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
            applicationId,
            user.UserAccountId!.Value,
            true,
            cancellationToken);

        if (result.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReviewFailure,
                    applicationId,
                    user.UserAccountId!.Value,
                    _requestContext),
                cancellationToken);
            return result;
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                applicationId,
                user.UserAccountId!.Value,
                _requestContext),
            cancellationToken);
        
        return result;
    }

    /// <inheritdoc />
    public async Task<Result> CompleteEiaScreeningAsync(
        Guid applicationId,
        InternalUser user,
        bool isScreeningCompleted,
        CancellationToken cancellationToken)
    {
        var result = await _updateWoodlandOfficerReviewService.CompleteEiaScreeningCheckAsync(
            applicationId,
            user.UserAccountId!.Value,
            isScreeningCompleted,
            cancellationToken);

        if (result.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReviewFailure,
                    applicationId,
                    user.UserAccountId!.Value,
                    _requestContext,
                    new
                    {
                        Section = "EIA screening",
                        Error = result.Error
                    }),
                cancellationToken);

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.WoodlandOfficerReviewEiaScreeningFailure,
                    applicationId,
                    user.UserAccountId!.Value,
                    _requestContext,
                    new
                    {
                        ScreeningCompleted = isScreeningCompleted,
                        Error = result.Error
                    }),
                cancellationToken);

            return result;
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                applicationId,
                user.UserAccountId!.Value,
                _requestContext,
                new
                {
                    Section = "EIA screening"
                }),
            cancellationToken);


        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.WoodlandOfficerReviewEiaScreening,
                applicationId,
                user.UserAccountId!.Value,
                _requestContext,
                new
                {
                    ScreeningCompleted = isScreeningCompleted,
                }),
            cancellationToken);

        return result;
    }

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Open applications", "Home", "Index", null),
            new BreadCrumb(model.FellingLicenceApplicationSummary.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary.Id.ToString())
        };
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = "Woodland officer review"
        };
    }

    private async Task AppendAuditFailure(Guid entityGuid, Guid userGuid, object? auditData = null, CancellationToken cancellationToken = default)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmWoodlandOfficerReviewFailure,
            entityGuid,
            userGuid,
            _requestContext,
            auditData
        ), cancellationToken);
    }

    private async Task AppendNotificationAuditFailure(Guid entityGuid, Guid userGuid, object? auditData = null, CancellationToken cancellationToken = default)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmWoodlandOfficerReviewNotificationFailure,
            entityGuid,
            userGuid,
            _requestContext,
            auditData
        ), cancellationToken);
    }

    private async Task<Result> SendReviewConfirmationNotifications(string applicationReference,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount applicant,
        UserAccount fieldManager,
        UserAccount performingUser,
        Guid applicationId,
        string internalLinkToApplication,
        string adminHubName,
        CancellationToken cancellationToken)
    {
        var adminHubFooter = await GetConfiguredFcAreasService.TryGetAdminHubAddress(adminHubName, cancellationToken);

        var fieldManagerModel = new InformAssignedUserOfApplicationStatusTransitionDataModel
        {
            Name = fieldManager.FullName(),
            ApplicationReference = applicationReference,
            PreviousAssignedUserName = performingUser.FullName(),
            PreviousAssignedEmailAddress = performingUser.Email,
            ViewApplicationURL = internalLinkToApplication,
            AdminHubFooter = adminHubFooter,
            ApplicationId = applicationId
        };

        var result1 = await _emailService.SendNotificationAsync(
            fieldManagerModel,
            NotificationType.InformFieldManagerOfWoodlandOfficerReviewCompletion,
            new NotificationRecipient(fieldManager.Email, fieldManager.FullName()),
            cancellationToken: cancellationToken);

        if (result1.IsFailure)
        {
            logger.LogError("Unable to send woodland officer review confirmation notification to field manager with id {id}", fieldManager.Id);
            await AppendNotificationAuditFailure(
                applicationId, performingUser.Id, new { error = "Failed to send notification to field manager" }, cancellationToken);
            return Result.Failure("Unable to send woodland officer review confirmation notification to field manager");
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmWoodlandOfficerReviewNotificationSent,
            applicationId,
            performingUser.Id,
            _requestContext,
            new
            {
                recipient = "Field Manager",
                recipientId = fieldManager.Id
            }
        ), cancellationToken);

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmWoodlandOfficerReviewNotificationSent,
            applicationId,
            performingUser.Id,
            _requestContext,
            new
            {
                recipient = "Applicant",
                recipientId = applicant.Id
            }
        ), cancellationToken);

        return Result.Success();
    }
}