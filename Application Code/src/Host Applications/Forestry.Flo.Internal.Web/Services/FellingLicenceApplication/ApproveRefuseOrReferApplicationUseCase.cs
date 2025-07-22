using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NodaTime;
using IExternalAccountService = Forestry.Flo.Services.Applicants.Services.IRetrieveUserAccountsService;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Handles use case for a field manager to approve, refuse an application, or refer an application to the local authority.
/// </summary>
public class ApproveRefuseOrReferApplicationUseCase(
    ILogger<ApproveRefuseOrReferApplicationUseCase> logger,
    IGetFellingLicenceApplicationForInternalUsers getFellingLicenceService,
    IUpdateFellingLicenceApplication updateFellingLicenceService,
    ISendNotifications notificationsService,
    IUserAccountService internalAccountService,
    IExternalAccountService externalAccountService,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IPublicRegister publicRegister,
    IClock clock,
    IOptions<ExternalApplicantSiteOptions> options,
    IAuditService<ApproveRefuseOrReferApplicationUseCase> auditService,
    RequestContext requestContext,
    IApproverReviewService approverReviewService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    IForesterServices agolServices)
{
    private readonly ILogger<ApproveRefuseOrReferApplicationUseCase> _logger = Guard.Against.Null(logger);


    private readonly IGetFellingLicenceApplicationForInternalUsers _getFellingLicenceService = Guard.Against.Null(getFellingLicenceService);
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceService = Guard.Against.Null(updateFellingLicenceService);
    private readonly IUserAccountService _internalAccountService = Guard.Against.Null(internalAccountService);
    private readonly IExternalAccountService _externalAccountService = Guard.Against.Null(externalAccountService);
    private readonly ISendNotifications _notificationsService = Guard.Against.Null(notificationsService);
    private readonly IRetrieveWoodlandOwners _woodlandOwnerService = Guard.Against.Null(woodlandOwnerService);
    private readonly IPublicRegister _publicRegister = Guard.Against.Null(publicRegister);
    private readonly IClock _clock = Guard.Against.Null(clock);
    private readonly IAuditService<ApproveRefuseOrReferApplicationUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly ExternalApplicantSiteOptions _options = Guard.Against.Null(options).Value;
    private readonly IApproverReviewService _approverReviewService = Guard.Against.Null(approverReviewService);
    private readonly IForesterServices _agolServices = Guard.Against.Null(agolServices);
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);

    /// <summary>
    /// Approves, refuses or refers an application that has been sent for approval.
    /// </summary>
    /// <param name="user">The internal user making the request.</param>
    /// <param name="applicationId">The application id.</param>
    /// <param name="requestedStatus">The requested status for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<FinaliseFellingLicenceApplicationResult> ApproveOrRefuseOrReferApplicationAsync(
        InternalUser user,
        Guid applicationId,
        FellingLicenceStatus requestedStatus,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to set status of application with id {ApplicationId} to {RequestedStatus}", applicationId, requestedStatus);

        if (requestedStatus is not (FellingLicenceStatus.Approved or FellingLicenceStatus.Refused or FellingLicenceStatus.ReferredToLocalAuthority))
        {
            _logger.LogError("Requested status must be approved or refused or referred to local Authority.");
            return FinaliseFellingLicenceApplicationResult.CreateFailure("Status must be set to approved or refused", FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationStatusRequested);
        }
        
        var applicationResult = await _getFellingLicenceService.GetApplicationByIdAsync(
            applicationId, 
            cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError("Could not retrieve application with id {ApplicationId}, error: {Error}", applicationId, applicationResult.Error);
            return FinaliseFellingLicenceApplicationResult.CreateFailure(applicationResult.Error,
                FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotRetrieveApplication);
        }

        var application = applicationResult.Value;

        // check the application has the sent for approval status

        if (application.StatusHistories.MaxBy(x => x.Created)?.Status is not FellingLicenceStatus.SentForApproval)
        {
            _logger.LogError("Application must have a status of {sentForApproval} to be {requested}", FellingLicenceStatus.SentForApproval.GetDisplayNameByActorType(ActorType.InternalUser), requestedStatus.GetDisplayNameByActorType(ActorType.InternalUser));
            return FinaliseFellingLicenceApplicationResult.CreateFailure($"Application must have a status of {FellingLicenceStatus.SentForApproval.GetDisplayNameByActorType(ActorType.InternalUser)} to be {requestedStatus.GetDisplayNameByActorType(ActorType.InternalUser)}",
                FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationState);

        }

        // check user is authorised to approve/refuse the application

        if (application.AssigneeHistories.NotAny(x =>
                x.AssignedUserId == user.UserAccountId && x.Role is AssignedUserRole.FieldManager))
        {
            _logger.LogError("User {id} is not an assigned field manager for the application", user.UserAccountId);
            return FinaliseFellingLicenceApplicationResult.CreateFailure(
                $"User {user.UserAccountId} is not an assigned field manager for the application",
                FinaliseFellingLicenceApplicationProcessOutcomes.UserRoleNotAuthorised);
        }

        var approverReview = await _approverReviewService.GetApproverReviewAsync(applicationId, cancellationToken);

        if (approverReview.HasNoValue)
        {
            _logger.LogError("Unable to retrieve approver review for application with id {ApplicationId}", applicationId);
            return FinaliseFellingLicenceApplicationResult.CreateFailure(
                $"Unable to retrieve approver review for application with id {applicationId}",
                FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotRetrieveApplication);
        }

        await _updateFellingLicenceService.AddStatusHistoryAsync(
            user.UserAccountId!.Value,
            applicationId,
            requestedStatus,
            cancellationToken);

        _logger.LogDebug("Sending notification for status update of application with id {ApplicationId} to {RequestedStatus} to applicant", applicationId, requestedStatus);
        var applicantNotificationResult = await SendApplicantNotificationAsync(
            application,
            requestedStatus,
            cancellationToken);

        var nonBlockingFailures = new List<FinaliseFellingLicenceApplicationProcessOutcomes>();

        if (applicantNotificationResult.IsFailure)
        {
            _logger.LogError("Unable to send to notification informing applicant of status change to {newStatus}, error {error}", requestedStatus, applicantNotificationResult.Error);
            nonBlockingFailures.Add(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotSendNotificationToApplicant);
        }

        SendToDecisionPublicRegisterOutcome? publishToDecisionsPublicRegisterResult = SendToDecisionPublicRegisterOutcome.Exempt;

        if (approverReview.Value.PublicRegisterPublish ?? false)
        {
            _logger.LogDebug("Attempting to publish application with id {ApplicationId} to the Decision Public Register", applicationId);

            publishToDecisionsPublicRegisterResult = await PublishToDecisionPublicRegisterAsync(application, requestedStatus, cancellationToken);

            if (publishToDecisionsPublicRegisterResult is SendToDecisionPublicRegisterOutcome.Failure)
            {
                _logger.LogError("Failed to publish application with id {ApplicationId} to the Decision Public Register", applicationId);
                nonBlockingFailures.Add(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);
            }

            if (publishToDecisionsPublicRegisterResult is SendToDecisionPublicRegisterOutcome
                    .FailedToSaveDecisionDetailsLocally)
            {
                _logger.LogError("Failed to save the decision public register details for application with id {ApplicationId} locally", applicationId);
                nonBlockingFailures.Add(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotStoreDecisionDetailsLocally);
            }
        }

        switch (requestedStatus)
        {
            case FellingLicenceStatus.Approved:
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.ApplicationApproved,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        application.WoodlandOwnerId,
                        ApplicationAuthorId = application.CreatedById,
                        NotificationSent = applicantNotificationResult.IsSuccess,
                        ApprovedByName = user.FullName,
                        DecisionPublicRegisterOutcome = publishToDecisionsPublicRegisterResult
                    }), cancellationToken);
                break;
            case FellingLicenceStatus.Refused:
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.ApplicationRefused,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        application.WoodlandOwnerId,
                        ApplicationAuthorId = application.CreatedById,
                        NotificationSent = applicantNotificationResult.IsSuccess,
                        RefusedByName = user.FullName,
                        DecisionPublicRegisterOutcome = publishToDecisionsPublicRegisterResult
                    }), cancellationToken);
                break;
            case FellingLicenceStatus.ReferredToLocalAuthority:
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.ApplicationReferredToLocalAuthority,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        application.WoodlandOwnerId,
                        ApplicationAuthorId = application.CreatedById,
                        NotificationSent = applicantNotificationResult.IsSuccess,
                        ReferredToLocalAuthorityByName = user.FullName,
                        DecisionPublicRegisterOutcome = publishToDecisionsPublicRegisterResult
                    }), cancellationToken);
                break;
        }

        return FinaliseFellingLicenceApplicationResult.CreateSuccess(nonBlockingFailures);
    }

    /// <summary>
    /// Publish to the Decision Public Register.
    /// </summary>
    private async Task<SendToDecisionPublicRegisterOutcome> PublishToDecisionPublicRegisterAsync(
        Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application,
        FellingLicenceStatus fellingLicenceStatus,
        CancellationToken cancellationToken)
    {
        /*
            FACT: When a case gets put on the decision PR, then if it existed on the Consultation PR, it will get removed automatically by the Public Register system.
           
            Todo: Notes 10/2024:   
        
            1)  A case may not have existed previously on the Consultation PR due to its exemption at that stage, but can 
                still be published to the D-PR at the approval/refusal/refer stage. In this instance all the case metadata, 
                (plus the EsriId and Geom will not be present on PR) - this is a known issue, and FS are engaging with IFOS/ESRI to deal with that.
           
            2)  An PR exemption set during the WO review process, has no bearing on whether it will go to the decision PR,
                during the approval/refusal/refer process.
         */

        //Todo: These 2 IFs will eventually need to change given the above points, will require improved property names on the PublicRegister
        //entity and poss extending it for WO recommendations and the actual approver's action, with expiry date for decision PR:

        _logger.LogDebug("Attempting to publish application with id {ApplicationId} to the Decision Public Register", application.Id);
        
        if (application.PublicRegister is { WoodlandOfficerSetAsExemptFromConsultationPublicRegister: true })
        {
            _logger.LogDebug("Application with Id {Id}, with Reference {CaseReference} is exempt from the Consultation Public Register, so will not be published to the Decision Public Register.",
                application.Id, application.ApplicationReference);
            return SendToDecisionPublicRegisterOutcome.Exempt;
        }

        if (application.PublicRegister?.EsriId == null)
        {
            _logger.LogError("Application having Id of {Id}, with Reference {CaseReference} does not have a prior consultation public register record stored internally.",
                application.Id, application.ApplicationReference);

            return SendToDecisionPublicRegisterOutcome.Failure;
        }

        var esriId = application.PublicRegister!.EsriId.Value;

        if (fellingLicenceStatus != FellingLicenceStatus.ReferredToLocalAuthority &&
            fellingLicenceStatus != FellingLicenceStatus.Approved
            && fellingLicenceStatus != FellingLicenceStatus.Refused)
        {
            _logger.LogError("Application having Id of {Id}, with Reference {CaseReference} is not in the correct state for sending to the decision public register, has {CurrentState}.",
                application.Id, application.ApplicationReference, fellingLicenceStatus);

            return SendToDecisionPublicRegisterOutcome.Failure;
        }

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();
        var publishToDecisionPublicRegisterResult = await _publicRegister.AddCaseToDecisionRegisterAsync(
            esriId,
            application.ApplicationReference,
            fellingLicenceStatus.ToString(),
            now,
            cancellationToken).ConfigureAwait(false);

        if (publishToDecisionPublicRegisterResult.IsFailure)
        {
            _logger.LogWarning("Failed to publish application to decision public register with error {Error}",
                publishToDecisionPublicRegisterResult.Error);

            return SendToDecisionPublicRegisterOutcome.Failure;
        }

        //todo this will need to come from the approver's recommendation, or the configured default duration etc.
        var expiresAt = now.AddDays(28);
        
        var saveDetailsResult = await _updateFellingLicenceService.AddDecisionPublicRegisterDetailsAsync(
            application.Id,
            now,
            expiresAt,
            cancellationToken);

        if (saveDetailsResult.IsFailure)
        {
            _logger.LogWarning("Failed to save the decision public register details for the application locally, error was {Error}",
                saveDetailsResult.Error);

            //if we get here, then there is no way to do any automated removal, as the local system will not know when to remove it.
            return SendToDecisionPublicRegisterOutcome.FailedToSaveDecisionDetailsLocally;
        }

        var assignedIds = application.AssigneeHistories
            .Where(x => x.Role != AssignedUserRole.Applicant && x.Role != AssignedUserRole.Author && x.TimestampUnassigned == null)
            .Select(x => x.AssignedUserId)
            .ToList();

        var adminHubAddress = await _getConfiguredFcAreasService.TryGetAdminHubAddress(
            application.AdministrativeRegion,
            cancellationToken);

        await SendInternalPublicRegisterNotifications(
            application.ApplicationReference,
            application.SubmittedFlaPropertyDetail!.Name,
            adminHubAddress,
            now,
            expiresAt,
            assignedIds,
            cancellationToken);

        return SendToDecisionPublicRegisterOutcome.Success;
    }

    private async Task<Result> SendApplicantNotificationAsync(
        Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application,
        FellingLicenceStatus requestedStatus,
        CancellationToken cancellationToken)
    {
        var (_, applicantFailure, applicantUser) = await 
            _externalAccountService.RetrieveUserAccountByIdAsync(application.CreatedById, cancellationToken);

        var (woodlandOwnerSuccess, _, woodlandOwner) = await
            _woodlandOwnerService.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, cancellationToken);

        if (applicantFailure)
        {
            _logger.LogError("Unable to retrieve user account id {id}", application.CreatedById);
            return Result.Failure($"Unable to retrieve user account id {application.CreatedById}");
        }

        var applicantRecipient = new NotificationRecipient(applicantUser.Email, applicantUser.FullName);

        var internalUsers = await
            _internalAccountService.RetrieveUserAccountsByIdsAsync(application.AssigneeHistories
                    .Where(x => x.Role is not AssignedUserRole.Applicant && x.TimestampUnassigned == null)
                    .Select(x => x.AssignedUserId).ToList(),
                cancellationToken);

        if (internalUsers.IsFailure)
        {
            _logger.LogError("Unable to retrieve internal user accounts, error: {error}", internalUsers.Error);
            return Result.Failure($"Unable to internal user accounts for application with id {application.Id}");
        }

        var copyToRecipients = internalUsers.Value
            .Select(x => new NotificationRecipient(x.Email, x.FullName))
            .ToList();

        if (woodlandOwnerSuccess)
        {
            var woodlandOwnerCopyToRecipient = GetWoodlandOwnerCopyToRecipient(applicantUser.Email, woodlandOwner);
            if (woodlandOwnerCopyToRecipient != null)
            {
                copyToRecipients.Add(woodlandOwnerCopyToRecipient);
            }
        }

        string? approverName = null;
        string? approverEmail = null;

        var approverId = application.AssigneeHistories
            .Where(x => x.Role is AssignedUserRole.FieldManager)
            .MaxBy(x => x.TimestampAssigned)?.AssignedUserId;

        if (approverId != null)
        {
            var approver = await _internalAccountService
                .GetUserAccountAsync(approverId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (approver.HasNoValue)
            {
                _logger.LogError("Unable to retrieve approver user account id {id}", approverId.Value);
                return Result.Failure($"Unable to retrieve approver user account id {approverId.Value}");
            }

            approverName = approver.Value.FullName(false);
            approverEmail = approver.Value.Email;
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(application.AdministrativeRegion)
            ? string.Empty
            : await _getConfiguredFcAreasService
                .TryGetAdminHubAddress(application.AdministrativeRegion, cancellationToken)
                .ConfigureAwait(false);

        switch (requestedStatus)
        {
            case FellingLicenceStatus.Approved:
            {
                var approvalModel = new InformApplicantOfApplicationApprovalDataModel
                {
                    ApplicationReference = application.ApplicationReference,
                    Name = applicantUser.FullName,
                    PropertyName = application.SubmittedFlaPropertyDetail?.Name,
                    ApproverName = approverName,
                    ViewApplicationURL = $"{_options.BaseUrl}FellingLicenceApplication/SupportingDocumentation/{application.Id}",
                    AdminHubFooter = adminHubFooter
                };

                return await _notificationsService.SendNotificationAsync(
                    approvalModel,
                    NotificationType.InformApplicantOfApplicationApproval,
                    applicantRecipient,
                    copyToRecipients.ToArray(),
                    cancellationToken: cancellationToken);
            }
            case FellingLicenceStatus.Refused:
            {
                var refusalModel = new InformApplicantOfApplicationRefusalDataModel
                {
                    ApplicationReference = application.ApplicationReference,
                    Name = applicantUser.FullName,
                    PropertyName = application.SubmittedFlaPropertyDetail?.Name,
                    ApproverName = approverName,
                    ApproverEmail = approverEmail,
                    ViewApplicationURL = $"{_options.BaseUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}",
                    AdminHubFooter = adminHubFooter
                };

                return await _notificationsService.SendNotificationAsync(
                    refusalModel,
                    NotificationType.InformApplicantOfApplicationRefusal,
                    applicantRecipient,
                    copyToRecipients.ToArray(),
                    cancellationToken: cancellationToken);
            }
            case FellingLicenceStatus.ReferredToLocalAuthority:
            {
                var submittedDate = application.StatusHistories
                    .Where(x => x.Status == FellingLicenceStatus.Submitted)
                    .MaxBy(x => x.Created)?.Created;

                var localAuthorityName = await GetLocalAuthorityForFellingLicenceApplicationAsync(application, cancellationToken);

                    var referredToLocalAuthorityModel = new InformApplicantOfApplicationReferredToLocalAuthorityDataModel
                {
                    ApplicationReference = application.ApplicationReference,
                    Name = applicantUser.FullName,
                    PropertyName = application.SubmittedFlaPropertyDetail?.Name,
                    SubmittedDate = submittedDate.HasValue
                        ? DateTimeDisplay.GetDateDisplayString(submittedDate)
                        : null,
                    ApproverName = approverName,
                    ViewApplicationURL = $"{_options.BaseUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}",
                    LocalAuthorityName = localAuthorityName,
                    AdminHubFooter = adminHubFooter
                };

                return await _notificationsService.SendNotificationAsync(
                    referredToLocalAuthorityModel,
                    NotificationType.InformApplicantOfApplicationReferredToLocalAuthority,
                    applicantRecipient,
                    copyToRecipients.ToArray(),
                    cancellationToken: cancellationToken);
            }
            default: return Result.Failure("Invalid status change was requested");
        }
    }

    private static NotificationRecipient? GetWoodlandOwnerCopyToRecipient(
        string? mainToEmailAddress,
        WoodlandOwnerModel? woodlandOwner)
    {
        if (!string.IsNullOrWhiteSpace(woodlandOwner?.ContactEmail)
            && !woodlandOwner.ContactEmail.Equals(mainToEmailAddress, StringComparison.CurrentCultureIgnoreCase))
        {
            return new NotificationRecipient(woodlandOwner.ContactEmail, woodlandOwner.ContactName);
        }

        return null;
    }

    private async Task<string> GetLocalAuthorityForFellingLicenceApplicationAsync(
        Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application, 
        CancellationToken cancellationToken)
    {
        var centrePoint = !string.IsNullOrEmpty(application.CentrePoint)
            ? JsonConvert.DeserializeObject<Point>(application.CentrePoint)
        : null;

        if (centrePoint != null)
        {
            var getLocalAuthorityResult = await _agolServices.GetLocalAuthorityAsync(centrePoint, cancellationToken);

            if (getLocalAuthorityResult.IsSuccess)
            {
                if (getLocalAuthorityResult.Value != null)
                {
                    _logger.LogDebug("Local authority is {LocalAuthorityName} for application having Id {ApplicationId}", getLocalAuthorityResult.Value.Name, application.Id);
                    return getLocalAuthorityResult.Value.Name;
                }

                _logger.LogWarning("Local authority not found for coordinates {Coordinates}, for application having Id {ApplicationId}", centrePoint, application.Id);
            }
            else
            {
                _logger.LogWarning("Failed to get local authority for application having Id {ApplicationId}, error is {Error}", application.Id, getLocalAuthorityResult.Error);
            }
        }
        else
        {
            _logger.LogWarning("Center point coordinates could not be found on the application, so Local authority cannot be calculated for application having Id {ApplicationId}", application.Id);
        }

        return string.Empty;
    }

    private async Task SendInternalPublicRegisterNotifications(
        string applicationReference,
        string propertyName,
        string adminHubAddress,
        DateTime publishedDate,
        DateTime expiresAt,
        List<Guid> assignedUserIds,
        CancellationToken cancellationToken)
    {
        var users = await _internalAccountService.RetrieveUserAccountsByIdsAsync(assignedUserIds, cancellationToken);

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
                ExpiryDate = DateTimeDisplay.GetDateDisplayString(expiresAt),
                Name = user.FullName,
                AdminHubFooter = adminHubAddress
            };

            var notificationResult = await _notificationsService.SendNotificationAsync(
                notificationModel,
                NotificationType.InformFcStaffOfApplicationAddedToDecisionPublicRegister,
                new NotificationRecipient(user.Email, user.FullName),
                cancellationToken: cancellationToken);

            if (notificationResult.IsFailure)
            {
                _logger.LogError("Could not send notification for publish to decision public register to {Name}", user.FullName);
            }
        }
    }

    public async Task<Result> UpdateApproverIdAsync(
        Guid applicationId, 
        Guid? approverId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to set approver id for application {ApplicationId}", applicationId);

        var result = await _updateFellingLicenceService.SetApplicationApproverAsync(
            applicationId,
            approverId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to set approver id for application {ApplicationId} with error {Error}", applicationId, result.Error);
        }

        return result;
    }
}