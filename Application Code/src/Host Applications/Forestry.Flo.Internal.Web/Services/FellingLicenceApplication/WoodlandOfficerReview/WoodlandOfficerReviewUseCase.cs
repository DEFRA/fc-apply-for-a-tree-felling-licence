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
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class WoodlandOfficerReviewUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly IClock _clock;
    private readonly IAuditService<WoodlandOfficerReviewUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ISendNotifications _emailService;
    private readonly ILogger<WoodlandOfficerReviewUseCase> _logger;
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;

    public WoodlandOfficerReviewUseCase(
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
        RequestContext requestContext,
        ILogger<WoodlandOfficerReviewUseCase> logger)
        : base(internalUserAccountService, 
            externalUserAccountService,
            fellingLicenceApplicationRepository,
            woodlandOwnerService,
            agentAuthorityService, 
            getConfiguredFcAreasService)
    {
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _clock = Guard.Against.Null(clock);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _emailService = Guard.Against.Null(emailService);
        _logger = logger;
        _activityFeedItemProvider = Guard.Against.Null(activityFeedItemProvider);
    }

    public async Task<Result<WoodlandOfficerReviewModel>> WoodlandOfficerReviewAsync(
        Guid applicationId,
        InternalUser user,
        string hostingPage,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve woodland officer review status for application with id {ApplicationId}", applicationId);

        var woodlandOfficerReviewStatus = await _getWoodlandOfficerReviewService.GetWoodlandOfficerReviewStatusAsync(
            applicationId, cancellationToken);

        if (woodlandOfficerReviewStatus.IsFailure)
        {
            _logger.LogError("Failed to retrieve woodland officer review details with error {Error}", woodlandOfficerReviewStatus.Error);
            return woodlandOfficerReviewStatus.ConvertFailure<WoodlandOfficerReviewModel>();
        }

        var application = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (application.IsFailure)
        {
            _logger.LogError("Failed to retrieve application summary with error {Error}", application.Error);
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
            _logger.LogError("Failed to retrieve activity feed items with error {Error}", application.Error);
            return activityFeedItems.ConvertFailure<WoodlandOfficerReviewModel>();
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
            RecommendedLicenceDuration = woodlandOfficerReviewStatus.Value.RecommendedLicenceDuration,
            RecommendationForDecisionPublicRegister = woodlandOfficerReviewStatus.Value.RecommendationForDecisionPublicRegister
        };
        result.WoodlandOfficerReviewCommentsFeed.ShowAddCaseNote = result.Editable(user);

        SetBreadcrumbs(result);

        return Result.Success(result);
    }

    public async Task<Result> CompleteWoodlandOfficerReviewAsync(
        Guid applicationId, 
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool? recommendationForDecisionPublicRegister,
        string internalLinkToApplication,
        InternalUser user, 
        CancellationToken cancellationToken)
    {
        const string notificationsError = "The Woodland Officer Review has been completed but the system was unable to send notifications";

        _logger.LogDebug("Attempting to complete the woodland officer review for application with id {ApplicationId}", applicationId);

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var result = await _updateWoodlandOfficerReviewService.CompleteWoodlandOfficerReviewAsync(
            applicationId,
            user.UserAccountId!.Value,
            recommendedLicenceDuration,
            recommendationForDecisionPublicRegister,
            now,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to complete woodland officer review for application with id {ApplicationId} with error {Error}", applicationId, result.Error);
            await AppendAuditFailure(
                applicationId, 
                user.UserAccountId!.Value, 
                new { Error = result.Error },
                cancellationToken);
            return Result.Failure("Could not complete Woodland Officer Review");
        }

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
            _logger.LogError("Unable to determine applicant for notification");
            await AppendAuditFailure(applicationId, user.UserAccountId.Value, new { Error = applicant.Error }, cancellationToken);
            return Result.Failure(notificationsError);
        }

        var woodlandOfficer = await InternalUserAccountService.GetUserAccountAsync(user.UserAccountId!.Value, cancellationToken);
        if (woodlandOfficer.HasNoValue)
        {
            _logger.LogError("Unable to find a user with the id of {Id}", user.UserAccountId!.Value);
            await AppendAuditFailure(applicationId, user.UserAccountId.Value,
                new { Error = "Unable to find woodland officer to notify" }, cancellationToken);
            return Result.Failure(notificationsError);
        }

        var fieldManager = await InternalUserAccountService.GetUserAccountAsync(result.Value.FieldManagerId, cancellationToken);
        if (fieldManager.HasNoValue)
        {
            _logger.LogError("Unable to find a user with the id of {Id}", result.Value.FieldManagerId);
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

        return sendNotificationsResult.IsSuccess
            ? Result.Success()
            : Result.Failure(notificationsError);
    }


    /// <summary>
    /// Completes the mapping check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the mapping check has been updated successfully.</returns>
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

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Home", "Home", "Index", null),
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
            AdminHubFooter = adminHubFooter
        };

        var result1 = await _emailService.SendNotificationAsync(
            fieldManagerModel,
            NotificationType.InformFieldManagerOfWoodlandOfficerReviewCompletion,
            new NotificationRecipient(fieldManager.Email, fieldManager.FullName()),
            cancellationToken: cancellationToken);

        if (result1.IsFailure)
        {
            _logger.LogError("Unable to send woodland officer review confirmation notification to field manager with id {id}", fieldManager.Id);
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