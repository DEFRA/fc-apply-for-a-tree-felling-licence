using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class PublicRegisterUseCase : FellingLicenceApplicationUseCaseBase, IPublicRegisterUseCase
{
    private readonly INotificationHistoryService _notificationHistoryService;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly IAuditService<PublicRegisterUseCase> _auditService;
    private readonly RequestContext _requestContext;
  
    private readonly IPublicRegister _publicRegister;
    private readonly IClock _clock;
    private readonly ISendNotifications _sendNotifications;
    private readonly ILogger<PublicRegisterUseCase> _logger;
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly WoodlandOfficerReviewOptions _woodlandOfficerReviewOptions;

    public PublicRegisterUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IPublicRegister publicRegister,
        INotificationHistoryService notificationHistoryService,
        IAuditService<PublicRegisterUseCase> auditService,
        IAgentAuthorityService agentAuthorityService,
        RequestContext requestContext,
        IClock clock,
        ISendNotifications sendNotifications,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IOptions<WoodlandOfficerReviewOptions> publicRegisterOptions,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        ILogger<PublicRegisterUseCase> logger) 
        : base(internalUserAccountService, 
            externalUserAccountService, 
            fellingLicenceApplicationInternalRepository, 
            woodlandOwnerService,
            agentAuthorityService, 
            getConfiguredFcAreasService, 
            woodlandOfficerReviewSubStatusService)
    {
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _publicRegister = Guard.Against.Null(publicRegister);
        _notificationHistoryService = Guard.Against.Null(notificationHistoryService);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _woodlandOfficerReviewOptions = Guard.Against.Null(publicRegisterOptions.Value);
        _clock = Guard.Against.Null(clock);
        _sendNotifications = Guard.Against.Null(sendNotifications);
        _logger = logger;
       
    }

    /// <inheritdoc />
    public async Task<Result<PublicRegisterViewModel>> GetPublicRegisterDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve public register details for application with id {ApplicationId}", applicationId);

        var publicRegister =
            await _getWoodlandOfficerReviewService.GetPublicRegisterDetailsAsync(applicationId, cancellationToken);

        if (publicRegister.IsFailure)
        {
            _logger.LogError("Failed to retrieve public register details with error {Error}", publicRegister.Error);
            return publicRegister.ConvertFailure<PublicRegisterViewModel>();
        }

        var application = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (application.IsFailure)
        {
            _logger.LogError("Failed to retrieve application summary with error {Error}", application.Error);
            return application.ConvertFailure<PublicRegisterViewModel>();
        }

        var comments = await _notificationHistoryService.RetrieveNotificationHistoryAsync(
            applicationId,
            [ NotificationType.PublicRegisterComment ],
            cancellationToken);

        if (comments.IsFailure)
        {
            _logger.LogError("Failed to retrieve public register comments with error {Error}", comments.Error);
            return comments.ConvertFailure<PublicRegisterViewModel>();
        }

        var removeModel = new RemoveFromPublicRegisterModel
        {
            ApplicationReference = application.Value.ApplicationReference,
            EsriId = publicRegister.Value.GetValueOrDefault(x => x.EsriId)
        };

        var result = new PublicRegisterViewModel
        {
            FellingLicenceApplicationSummary = application.Value,
            ApplicationId = applicationId,
            PublicRegister = publicRegister.Value.HasValue ? publicRegister.Value.Value : new PublicRegisterModel
            {
                ConsultationPublicRegisterPeriodDays = _woodlandOfficerReviewOptions.PublicRegisterPeriod.Days
            },
            ReceivedPublicRegisterComments = comments.Value,
            RemoveFromPublicRegister = removeModel
        };

        SetBreadcrumbs(result);

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> StorePublicRegisterExemptionAsync(
        Guid applicationId,
        bool isExempt,
        string? exemptionReason,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to update public register exemption for application with id {ApplicationId}", applicationId);

        var updateResult = await _updateWoodlandOfficerReviewService.SetPublicRegisterExemptAsync(
            applicationId,
            user.UserAccountId.Value,
            isExempt,
            exemptionReason,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            if (updateResult.Value)
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateWoodlandOfficerReview,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new { Section = "Public Register" }),
                    cancellationToken);

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.AddToConsultationPublicRegisterSuccess,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new { Exempt = isExempt, ExemptionReason = exemptionReason }),
                    cancellationToken);
            }

            return Result.Success();
        }

        _logger.LogError("Failed to update public register exemption with error {Error}", updateResult.Error);
        await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Public Register", updateResult.Error, cancellationToken);
        await AuditPublicRegisterFailureEvent(applicationId, user, updateResult.Error, cancellationToken);

        return Result.Failure(updateResult.Error);
    }

    /// <inheritdoc />
    public async Task<Result> PublishToConsultationPublicRegisterAsync(
        Guid applicationId,
        TimeSpan period,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to publish application with id {ApplicationId} to the consultation public register", applicationId);

        var getPublishModel =
            await _getWoodlandOfficerReviewService.GetApplicationDetailsToSendToPublicRegisterAsync(
                applicationId,
                cancellationToken);

        if (getPublishModel.IsFailure)
        {
            _logger.LogError("Failed to get publish to public register model with error {Error}", getPublishModel.Error);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Public Register", getPublishModel.Error, cancellationToken);
            await AuditPublicRegisterFailureEvent(applicationId, user, getPublishModel.Error, cancellationToken);
            return Result.Failure("Retrieving data to publish application to consultation public register failed.");
        }

        var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();
        var esriId = getPublishModel.Value.ExistingEsriId;

        if (esriId.HasValue)
        {
            // handle republishing an application that is already on the public register

            var republishResult = await _publicRegister.ReturnCaseToConsultationRegisterAsync(
                getPublishModel.Value.ExistingEsriId.Value,
                getPublishModel.Value.CaseReference,
                timestamp,
                Convert.ToInt32(Math.Floor(period.TotalDays)),
                cancellationToken);

            if (republishResult.IsFailure)
            {
                _logger.LogError("Failed to republish application to public register with error {Error}", republishResult.Error);
                await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Public Register", republishResult.Error, cancellationToken);
                await AuditPublicRegisterFailureEvent(applicationId, user, republishResult.Error, cancellationToken);
                return Result.Failure("Republishing application to consultation public register failed.");
            }


        }
        else
        {
            var publishResult = await _publicRegister.AddCaseToConsultationRegisterAsync(
                getPublishModel.Value.CaseReference,
                getPublishModel.Value.PropertyName,
                _woodlandOfficerReviewOptions.DefaultCaseTypeOnPublishToPublicRegister,
                getPublishModel.Value.GridReference,
                getPublishModel.Value.NearestTown,
                getPublishModel.Value.LocalAuthority,
                getPublishModel.Value.AdminRegion,
                timestamp,
                Convert.ToInt32(Math.Floor(period.TotalDays)),
                getPublishModel.Value.BroadleafArea,
                getPublishModel.Value.ConiferousArea,
                getPublishModel.Value.OpenGroundArea,
                getPublishModel.Value.TotalArea,
                getPublishModel.Value.Compartments,
                cancellationToken);

            if (publishResult.IsFailure)
            {
                _logger.LogError("Failed to publish application to public register with error {Error}", publishResult.Error);
                await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Public Register", publishResult.Error, cancellationToken);
                await AuditPublicRegisterFailureEvent(applicationId, user, publishResult.Error, cancellationToken);
                return Result.Failure("Publishing application to consultation public register failed.");
            }

            esriId = publishResult.Value;
        }

        var updateResult = await _updateWoodlandOfficerReviewService.PublishedToPublicRegisterAsync(
            applicationId,
            user.UserAccountId.Value,
            esriId.Value,
            timestamp,
            period,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { Section = "Public Register" }),
                cancellationToken);
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.AddToConsultationPublicRegisterSuccess,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { PublicationDate = timestamp, EsriId = esriId.Value }),
                cancellationToken);

            var adminHubAddress = await GetConfiguredFcAreasService.TryGetAdminHubAddress(getPublishModel.Value.AdminRegion, cancellationToken);

            await SendNotifications(
                applicationId,
                getPublishModel.Value.CaseReference,
                getPublishModel.Value.PropertyName,
                adminHubAddress,
                timestamp,
                period,
                getPublishModel.Value.AssignedInternalUserIds,
                cancellationToken);

            return Result.Success();
        }

        _logger.LogError("Failed to update application as published to public register with error {Error}", updateResult.Error);
        await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Public Register", updateResult.Error, cancellationToken);
        await AuditPublicRegisterFailureEvent(applicationId, user, updateResult.Error, cancellationToken);
        return Result.Failure(updateResult.Error);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveFromPublicRegisterAsync(
        Guid applicationId,
        InternalUser user,
        int esriId,
        string applicationReference,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to remove application with id {ApplicationId} from the consultation public register", applicationId);

        var publicRegisterResult = await _publicRegister.RemoveCaseFromConsultationRegisterAsync(
            esriId,
            applicationReference,
            _clock.GetCurrentInstant().ToDateTimeUtc(),
            cancellationToken);

        if (publicRegisterResult.IsFailure)
        {
            _logger.LogError("Failed to remove application from public register with error {Error}", publicRegisterResult.Error);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Public Register", publicRegisterResult.Error, cancellationToken);
            await AuditPublicRegisterFailureEvent(applicationId, user, publicRegisterResult.Error, cancellationToken);
            return Result.Failure("Removing application from consultation public register failed.");
        }

        var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();
        var updateResult = await _updateWoodlandOfficerReviewService.RemovedFromPublicRegisterAsync(
            applicationId,
            user.UserAccountId!.Value,
            timestamp,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { Section = "Public Register" }),
                cancellationToken);
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.AddToConsultationPublicRegisterSuccess,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { RemovalDate = timestamp, EsriId = esriId }),
                cancellationToken);

            return Result.Success();
        }

        _logger.LogError("Failed to update application as removed from public register with error {Error}", updateResult.Error);
        await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Public Register", updateResult.Error, cancellationToken);
        await AuditPublicRegisterFailureEvent(applicationId, user, updateResult.Error, cancellationToken);

        return Result.Failure(updateResult.Error);
    }

    /// <inheritdoc />
    public async Task<Result<ReviewCommentModel>> GetPublicRegisterCommentAsync(Guid applicationId, Guid commentId, CancellationToken cancellationToken)
    {
        var commentResult = await _notificationHistoryService.GetNotificationHistoryByIdAsync(commentId, cancellationToken);
        if (commentResult.IsFailure)
            return Result.Failure<ReviewCommentModel>(commentResult.Error);

        commentResult.Value.LastUpdatedUser = await GetUserNameAsync(commentResult.Value.LastUpdatedById, cancellationToken);

        var publicRegisterDetailsResult = await GetPublicRegisterDetailsAsync(applicationId, cancellationToken);
        if (publicRegisterDetailsResult.IsFailure)
            return Result.Failure<ReviewCommentModel>("Public register details not found.");

        publicRegisterDetailsResult.Value.Breadcrumbs!.Breadcrumbs.Add(
            new BreadCrumb("Consultation public register", "WoodlandOfficerReview", "PublicRegister", applicationId.ToString()));

        var viewModel = new ReviewCommentModel
        {
            FellingLicenceApplicationSummary = publicRegisterDetailsResult.Value.FellingLicenceApplicationSummary,
            Breadcrumbs = publicRegisterDetailsResult.Value.Breadcrumbs,
            ApplicationId = publicRegisterDetailsResult.Value.ApplicationId,
            Comment = commentResult.Value
        };
        return Result.Success(viewModel);
    }

    /// <inheritdoc />
    public async Task<Result> UpdatePublicRegisterDetailsAsync(Guid commentId, NotificationHistoryModel model, CancellationToken cancellationToken)
    {
        // Update Response, Status, LastUpdatedById, LastUpdatedDate on the notification history
        var result = await _notificationHistoryService.UpdateResponseStatusByIdAsync(
            commentId,
            model.Status,
            model.Response,
            model.LastUpdatedById ?? Guid.Empty,
            model.LastUpdatedDate ?? DateTime.UtcNow,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to update public register comment with error {Error}", result.Error);
        }

        return result;
    }

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Open applications", "Home", "Index", null),
            new BreadCrumb(model.FellingLicenceApplicationSummary.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary.Id.ToString()),
            new BreadCrumb("Woodland Officer Review", "WoodlandOfficerReview", "Index", model.FellingLicenceApplicationSummary.Id.ToString())
        };
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = "Public Register"
        };
    }

    private async Task AuditWoodlandOfficerReviewFailureEvent(
        Guid applicationId,
        InternalUser user,
        string section,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.UpdateWoodlandOfficerReviewFailure,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new { Section = section, Error = error }), cancellationToken);
    }

    private async Task AuditPublicRegisterFailureEvent(
        Guid applicationId,
        InternalUser user,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AddToConsultationPublicRegisterFailure,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new { Error = error }), cancellationToken);
    }

    private async Task SendNotifications(
        Guid applicationId,
        string applicationReference,
        string propertyName,
        string adminHubAddress,
        DateTime publishedDate,
        TimeSpan period,
        List<Guid> assignedUserIds,
        CancellationToken cancellationToken)
    {
        var users = await InternalUserAccountService.RetrieveUserAccountsByIdsAsync(assignedUserIds, cancellationToken);

        if (users.IsFailure)
        {
            _logger.LogError("Failed to retrieve user accounts with error {Error}", users.Error);
            return;
        }

        foreach (var user in users.Value)
        {
            var notificationModel = new InformFcStaffOfApplicationAddedToPublicRegisterDataModel
            {
                PropertyName = propertyName,
                ApplicationReference = applicationReference,
                PublishedDate = DateTimeDisplay.GetDateDisplayString(publishedDate),
                ExpiryDate = DateTimeDisplay.GetDateDisplayString(publishedDate.Add(period)),
                Name = user.FullName,
                AdminHubFooter = adminHubAddress,
                ApplicationId = applicationId
            };

            var notificationResult = await _sendNotifications.SendNotificationAsync(
                notificationModel,
                NotificationType.InformFcStaffOfApplicationAddedToConsultationPublicRegister,
                new NotificationRecipient(user.Email, user.FullName),
                cancellationToken: cancellationToken);

            if (notificationResult.IsFailure)
            {
                _logger.LogError("Could not send notification for publish to consultation public register to {Name}", user.FullName);
            }
        }
    }

    private async Task<string> GetUserNameAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return string.Empty;
        }
        var user = await InternalUserAccountService.GetUserAccountAsync(userId.Value, cancellationToken);
        if (user.HasNoValue)
        {
            return string.Empty;
        }
        var userModel = ModelMapping.ToUserAccountModel(user.Value);
        return $"{userModel.FirstName} {userModel.LastName}".Trim();
    }
}