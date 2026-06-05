using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;

public class ExternalConsulteeInviteUseCase : FellingLicenceApplicationUseCaseBase, IExternalConsulteeInviteUseCase
{
    private readonly INotificationHistoryService _notificationHistoryService;
    private readonly IUserAccountService _internalUserAccountService;
    private readonly IExternalConsulteeReviewService _externalConsulteeReviewService;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly ISendNotifications _notificationService;
    private readonly IAuditService<ExternalConsulteeInviteUseCase> _auditService;
    private readonly ILogger<ExternalConsulteeInviteUseCase> _logger;
    private readonly IClock _clock;
    private readonly UserInviteOptions _settings;
    private readonly RequestContext _requestContext;
    private const string ApplicationNotFoundError = "Could not locate Felling Licence Application with the given id";

    public ExternalConsulteeInviteUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        ISendNotifications notificationService,
        INotificationHistoryService notificationHistoryService,
        IAuditService<ExternalConsulteeInviteUseCase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IExternalConsulteeReviewService externalConsulteeReviewService,
        ILogger<ExternalConsulteeInviteUseCase> logger,
        IClock clock,
        IOptions<UserInviteOptions> options,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        RequestContext requestContext) : base(
        internalUserAccountService,
        externalUserAccountService,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        agentAuthorityService,
        getConfiguredFcAreasService,
        woodlandOfficerReviewSubStatusService)
    {
        _notificationHistoryService = Guard.Against.Null(notificationHistoryService);
        _internalUserAccountService = Guard.Against.Null(internalUserAccountService);
        _externalConsulteeReviewService = Guard.Against.Null(externalConsulteeReviewService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _notificationService = Guard.Against.Null(notificationService);
        _auditService = Guard.Against.Null(auditService);
        _logger = Guard.Against.Null(logger);
        _clock = Guard.Against.Null(clock);
        _settings = Guard.Against.Null(options).Value;
        _requestContext = Guard.Against.Null(requestContext);
    }

    /// <inheritdoc />
    public async Task<Result<ExternalConsulteeIndexViewModel>> GetConsulteeInvitesIndexViewModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (hasValue)
        {
            return await ExtractApplicationSummaryAsync(fla, cancellationToken)
                .Map(applicationSummary => new ExternalConsulteeIndexViewModel
                {
                    ApplicationId = applicationId,
                    FellingLicenceApplicationSummary = applicationSummary,
                    InviteLinks = ModelMapping.ToExternalInviteLinkList(fla.ExternalAccessLinks, fla.ConsulteeComments),
                    ApplicationNeedsConsultations = fla.WoodlandOfficerReview?.ApplicationNeedsConsultations,
                    ConsultationsComplete = fla.WoodlandOfficerReview?.ConsultationsComplete ?? false,
                    CurrentDateTimeUtc = _clock.GetCurrentInstant().ToDateTimeUtc()
                })
                .OnFailure(e => { _logger.LogError(e); });
        }

        _logger.LogError(ApplicationNotFoundError);
        return Result.Failure<ExternalConsulteeIndexViewModel>(ApplicationNotFoundError);
    }

    /// <inheritdoc />
    public async Task<Result> SetDoesNotRequireConsultationsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Setting woodland officer review consultations to not needed");

        return await _updateWoodlandOfficerReviewService.UpdateConsultationsStatusAsync(
            applicationId, user.UserAccountId!.Value, false, false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> SetConsultationsCompleteAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Setting woodland officer review consultations to complete");

        return await _updateWoodlandOfficerReviewService.UpdateConsultationsStatusAsync(
            applicationId, user.UserAccountId!.Value, true, true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ExternalConsulteeInviteFormModel>> GetNewExternalConsulteeInviteViewModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (hasValue)
        {
            var prCompleted =
                fla.PublicRegister != null
                && (fla.PublicRegister.ConsultationPublicRegisterPublicationTimestamp.HasValue);
            var prExempt = fla.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister;

            var documentModels = ModelMapping
                .ToDocumentModelList(fla.Documents?
                    .Where(x => x.VisibleToConsultee && x.DeletionTimestamp.HasNoValue())
                    .OrderByDescending(x => x.CreatedTimestamp)
                    .ToList() ?? [])
                .ToList();

            return await ExtractApplicationSummaryAsync(fla, cancellationToken)
                .Map(applicationSummary =>
                {
                    return new ExternalConsulteeInviteFormModel
                    {
                        FellingLicenceApplicationSummary = applicationSummary,
                        ApplicationId = applicationId,
                        ExemptFromConsultationPublicRegister = prExempt,
                        ExemptFromConsultationPublicRegisterReason = prExempt is true ? fla.PublicRegister?.WoodlandOfficerConsultationPublicRegisterExemptionReason : null,
                        PublicRegisterAlreadyCompleted = prCompleted,
                        SelectedDocumentIds = documentModels.Select(d => (Guid?)d.Id).ToList(),
                        ConsulteeDocuments = documentModels
                    };
                })
                .OnFailure(e => { _logger.LogError(e); });
        }
        _logger.LogError(ApplicationNotFoundError);
        return Result.Failure<ExternalConsulteeInviteFormModel>(ApplicationNotFoundError);
    }

    /// <inheritdoc />
    public async Task<Result> InviteExternalConsulteeAsync(
        ExternalConsulteeInviteModel externalConsulteeInviteModel,
        Guid applicationId,
        InternalUser user, 
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (!hasValue)
        {
            _logger.LogError(ApplicationNotFoundError);
            return Result.Failure(ApplicationNotFoundError);
        }

        var endDate = _clock.GetCurrentInstant().ToDateTimeUtc().AddDays(_settings.InviteLinkExpiryDays);

        var adminHubFooter = await GetAdminHubAddressDetailsAsync(fla.AdministrativeRegion, cancellationToken)
            .ConfigureAwait(false);

        var externalConsulteeInvite = new ExternalConsulteeInviteDataModel
        {
            ApplicationReference = fla.ApplicationReference,
            ConsulteeName = externalConsulteeInviteModel.ConsulteeName,
            EmailText = externalConsulteeInviteModel.ConsulteeEmailText,
            SenderName = user.FullName!,
            SenderEmail = user.EmailAddress!,
            CommentsEndDate = DateTimeDisplay.GetDateDisplayString(endDate),
            ViewApplicationURL = externalConsulteeInviteModel.ExternalAccessLink,
            AdminHubFooter = adminHubFooter,
            ApplicationId = applicationId,
            PropertyName = fla.SubmittedFlaPropertyDetail?.Name
        };

        var accessLink = new ExternalAccessLink
        {
            Name = externalConsulteeInviteModel.ConsulteeName,
            Purpose = externalConsulteeInviteModel.Purpose!,
            AccessCode = externalConsulteeInviteModel.ExternalAccessCode,
            ContactEmail = externalConsulteeInviteModel.Email,
            FellingLicenceApplicationId = applicationId,
            CreatedTimeStamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
            ExpiresTimeStamp = _clock.GetCurrentInstant().ToDateTimeUtc().AddDays(_settings.InviteLinkExpiryDays),
            IsMultipleUseAllowed = true,
            LinkType = ExternalAccessLinkType.ConsulteeInvite,
            SharedSupportingDocuments = externalConsulteeInviteModel.SelectedDocumentIds
        };

        var notificationType = externalConsulteeInviteModel.ExemptFromConsultationPublicRegister
            ? NotificationType.ExternalConsulteeInvite
            : NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo;


        var addAccessLinkResult = await FellingLicenceRepository.AddExternalAccessLinkAsync(accessLink, cancellationToken);

        if (addAccessLinkResult.IsFailure)
        {
            _logger.LogError(
                "Error occurred adding external access link to application {ApplicationId}, error: {Error}",
                applicationId, addAccessLinkResult.Error);
            await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationFailure, user, cancellationToken, addAccessLinkResult.Error.ToString());

            return Result.Failure("Failed to add external access link");
        }

        var updateConsultationStatusResult = await _updateWoodlandOfficerReviewService.UpdateConsultationsStatusAsync(
            applicationId, user.UserAccountId!.Value, true, false, cancellationToken);

        if (updateConsultationStatusResult.IsFailure)
        {
            _logger.LogError(
                "Error occurred updating consultation status for application {ApplicationId} after adding external access link, error: {Error}",
                applicationId, updateConsultationStatusResult.Error);

            await FellingLicenceRepository.DeleteExternalAccessLinkAsync(accessLink, cancellationToken);

            await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationFailure, user, cancellationToken, updateConsultationStatusResult.Error);

            return Result.Failure("Failed to update consultations status");
        }

        // only send to consultee first, in case a copy to internal staff fails, because then the consultee will still receive their invite
        // but we'll have rolled back the access link save to the database so the link won't work
        var sendNotificationResult = await _notificationService.SendNotificationAsync(
            externalConsulteeInvite,
            notificationType,
            new NotificationRecipient(externalConsulteeInviteModel.Email, externalConsulteeInviteModel.ConsulteeName),
            cancellationToken: cancellationToken);

        if (sendNotificationResult.IsFailure)
        {
            _logger.LogError("Error occurred sending consultee invite notification for application {ApplicationId} to email {Email}",
                applicationId, externalConsulteeInviteModel.Email);

            await FellingLicenceRepository.DeleteExternalAccessLinkAsync(accessLink, cancellationToken);
            
            await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationFailure, user, cancellationToken, sendNotificationResult.Error);

            return Result.Failure("Failed to send consultee invite notification");
        }

        accessLink.NotificationHistoryId = sendNotificationResult.Value;
        await FellingLicenceRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationSent, user, cancellationToken);

        // now copy email to the internal staff
        var internalStaffIds = fla.AssigneeHistories
            .Where(x => x.TimestampUnassigned.HasNoValue() 
                        && x.Role is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer or AssignedUserRole.FieldManager)
            .Select(x => x.AssignedUserId)
            .Distinct()
            .ToList(); 
        
        var internalStaff =
            await _internalUserAccountService.RetrieveUserAccountsByIdsAsync(internalStaffIds, cancellationToken);

        if (internalStaff.IsFailure)
        {
            _logger.LogError("Unable to retrieve internal users for application {ApplicationId} to send consultee invite notification, error: {Error}",
                applicationId, internalStaff.Error);

            await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationCopyToStaffFailure, user, cancellationToken, 
                "Failed to retrieve internal staff to copy consultee invite email to, error: " + internalStaff.Error);
        }
        else
        {
            var staffRecipients = internalStaff.Value
                .Select(s => new NotificationRecipient(s.Email, s.FullName))
                .ToList();

            var copiesSuccessful = true;
            foreach (var staffRecipient in staffRecipients)
            {
                var copyResult = await _notificationService.SendNotificationAsync(
                    externalConsulteeInvite,
                    notificationType,
                    staffRecipient,
                    cancellationToken: cancellationToken);

                if (copyResult.IsFailure)
                {
                    _logger.LogError("Failed to copy consultee invitation email to staff member {StaffEmail} for application {ApplicationId}, error: {Error}",
                        staffRecipient.Address, applicationId, copyResult.Error);
                    copiesSuccessful = false;
                }
            }

            if (!copiesSuccessful)
            {
                await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationCopyToStaffFailure, user, cancellationToken, "Failed to copy invite email to one or more internal staff members");
            }
            else
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.ExternalConsulteeInvitationCopyToStaff,
                        accessLink.FellingLicenceApplicationId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            InvitedByUserId = user.UserAccountId,
                            ConsulteeName = accessLink.Name,
                            ConsulteeEmailAddress = accessLink.ContactEmail,
                            ApplicationId = accessLink.FellingLicenceApplicationId,
                            InviteExpiryDateTime = accessLink.ExpiresTimeStamp,
                            Staff = staffRecipients.Select(x => x.Address).ToList()
                        }),
                    cancellationToken);
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SendReminderToConsulteeAsync(
        Guid applicationId,
        Guid accessCode,
        string externalAccessLink,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (!hasValue)
        {
            _logger.LogError(ApplicationNotFoundError);
            return Result.Failure(ApplicationNotFoundError);
        }

        var adminHubFooter = await GetAdminHubAddressDetailsAsync(fla.AdministrativeRegion, cancellationToken)
            .ConfigureAwait(false);

        var accessLink = fla.ExternalAccessLinks.SingleOrDefault(x => x.AccessCode == accessCode);

        if (accessLink == null)
        {
            _logger.LogError("Could not locate external access link with code {AccessCode} for application {ApplicationId}",
                accessCode, applicationId);
            await PublishAuditErrorEvent(applicationId, AuditEvents.ExternalConsulteeReminderFailure, user,
                cancellationToken, "Could not find access link");
            return Result.Failure("Could not locate external access link");
        }

        var internalStaffIds = fla.AssigneeHistories
            .Where(x =>
                x.TimestampUnassigned.HasNoValue()
                && x.Role is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer or AssignedUserRole.FieldManager)
            .Select(x => x.AssignedUserId)
            .Distinct()
            .ToList();

        var internalStaff =
            await _internalUserAccountService.RetrieveUserAccountsByIdsAsync(internalStaffIds, cancellationToken);

        if (internalStaff.IsFailure)
        {
            _logger.LogError("Unable to retrieve internal users for application {ApplicationId} to send consultee reminder notification, error: {Error}",
                applicationId, internalStaff.Error);

            await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeReminderFailure, user, cancellationToken, internalStaff.Error);

            return Result.Failure("Unable to load internal staff assigned to the application to copy consultee invite reminder to");
        }

        var staffRecipients = internalStaff.Value
            .Select(s => new NotificationRecipient(s.Email, s.FullName));

        var reminderModel = new ExternalConsulteeInviteReminderDataModel
        {
            ApplicationId = applicationId,
            AdminHubFooter = adminHubFooter,
            ApplicationReference = fla.ApplicationReference,
            PropertyName = fla.SubmittedFlaPropertyDetail?.Name,
            ConsultationEndDate = DateTimeDisplay.GetDateDisplayString(accessLink.ExpiresTimeStamp),
            ViewApplicationURL = externalAccessLink
        };

        var sendNotificationResult = await _notificationService.SendNotificationAsync(
            reminderModel,
            NotificationType.ExternalConsulteeInviteReminder,
            new NotificationRecipient(accessLink.ContactEmail, accessLink.Name),
            copyToRecipients: staffRecipients.ToArray(),
            cancellationToken: cancellationToken);

        if (sendNotificationResult.IsFailure)
        {
            _logger.LogError("Error occurred sending consultee invite reminder notification for application {ApplicationId} to email {Email}",
                applicationId, accessLink.ContactEmail);

            await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeReminderFailure, user, cancellationToken, sendNotificationResult.Error);

            return Result.Failure("Failed to send consultee invite notification");
        }

        await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeReminderSent, user, cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ReceivedConsulteeCommentsViewModel>> GetReceivedCommentsAsync(
        Guid applicationId,
        Guid accessCode,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to retrieve information to display consultee comments received for application id {ApplicationId} and access link code {AccessCode}",
            applicationId,
            accessCode);

        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (!hasValue)
        {
            _logger.LogError("Could not locate Felling Licence Application with the given id {id}", applicationId);
            return Result.Failure<ReceivedConsulteeCommentsViewModel>($"Could not locate Felling Licence Application with the given id {applicationId}");
        }

        var (_, isFailure, flaModel, error) = await ExtractApplicationSummaryAsync(fla, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Could not load application summary model: {Error}", error);
            return Result.Failure<ReceivedConsulteeCommentsViewModel>($"Could not load application summary model: {error}");
        }

        var comments = await _externalConsulteeReviewService.RetrieveConsulteeCommentsForAccessCodeAsync(
            applicationId,
            accessCode,
            cancellationToken);

        var items = comments
            .OrderByDescending(x => x.CreatedTimestamp)
            .Select(x => new ReceivedConsulteeCommentModel
            {
                AuthorName = x.AuthorName,
                Comment = x.Comment,
                CreatedTimestamp = x.CreatedTimestamp,
                Attachments = GetAttachments(x.ConsulteeAttachmentIds.ToList(), fla.Documents)
            });

        var inviteNotificationContent = "Unable to load invite notification content";

        var accessLink = fla.ExternalAccessLinks.SingleOrDefault(x => x.AccessCode == accessCode);
        var notificationHistoryId = accessLink?.NotificationHistoryId;

        if (notificationHistoryId.HasValue)
        {
            var notification =
                await _notificationHistoryService.GetNotificationHistoryByIdAsync(notificationHistoryId.Value, cancellationToken);

            if (notification.IsSuccess)
            {
                inviteNotificationContent = notification.Value.Text;
            }
            else
            {
                _logger.LogError("Unable to load notification history with id {NotificationHistoryId}, error: {Error}",
                    notificationHistoryId, notification.Error);
            }
        }

        var result = new ReceivedConsulteeCommentsViewModel
        {
            ApplicationId = applicationId,
            ConsulteeName = accessLink?.Name ?? "Unable to locate consultee invitation",
            Email = accessLink?.ContactEmail ?? "Unable to locate consultee invitation",
            FellingLicenceApplicationSummary = flaModel,
            ReceivedComments = items.ToList(),
            PublicRegisterExempt = fla.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister is true,
            PublicRegisterExemptionReason = fla.PublicRegister?.WoodlandOfficerConsultationPublicRegisterExemptionReason,
            InviteContent = inviteNotificationContent,
            SharedDocuments = ModelMapping.ToDocumentModelList((fla.Documents ?? [])
                .Where(x => x.VisibleToConsultee
                            && x.DeletionTimestamp.HasNoValue()
                            && (accessLink?.SharedSupportingDocuments.Any(s => s == x.Id) ?? false))
                .OrderByDescending(x => x.CreatedTimestamp)
                .ToList()).ToList(),
            InvitationPurpose = accessLink?.Purpose ?? "Unable to locate consultee invitation",
            InvitationDate = accessLink?.CreatedTimeStamp
        };

        return Result.Success(result);
    }

    private Task PublishAuditEvent(ExternalAccessLink accessLink, string eventName, InternalUser user,
        CancellationToken cancellationToken,
        string? error = null) =>
        _auditService.PublishAuditEventAsync(
            new AuditEvent(
                eventName,
                accessLink.FellingLicenceApplicationId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    InvitedByUserId = user.UserAccountId,
                    ConsulteeName = accessLink.Name,
                    ConsulteeEmailAddress = accessLink.ContactEmail,
                    ApplicationId = accessLink.FellingLicenceApplicationId,
                    InviteExpiryDateTime = accessLink.ExpiresTimeStamp,
                    Error = error
                }),
            cancellationToken);

    private Task PublishAuditErrorEvent(
        Guid applicationId,
        string eventName, 
        InternalUser user,
        CancellationToken cancellationToken,
        string? error = null) =>
        _auditService.PublishAuditEventAsync(
            new AuditEvent(
                eventName,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    Error = error
                }),
            cancellationToken);

    private static string ExtractDatabaseError(UserDbErrorReason e) =>
        e == UserDbErrorReason.NotUnique
            ? "the access link already exists"
            : "a database error";

    private static Dictionary<Guid, string> GetAttachments(IList<Guid>? consulteeAttachmentIds, IList<Document>? flaDocuments)
    {
        if (consulteeAttachmentIds == null || !consulteeAttachmentIds.Any() || flaDocuments == null || !flaDocuments.Any())
        {
            return new Dictionary<Guid, string>();
        }

        var result = new Dictionary<Guid, string>();
        foreach (var consulteeAttachmentId in consulteeAttachmentIds)
        {
            var document = flaDocuments.FirstOrDefault(x => x.Id == consulteeAttachmentId);
            if (document != null)
            {
                result[consulteeAttachmentId] = document.FileName;
            }
        }

        return result;
    }
}