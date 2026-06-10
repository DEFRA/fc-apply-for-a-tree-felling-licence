using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.HostApplicationsCommon.Services;

/// <summary>
/// Base class for use cases handling the withdrawal of felling licence applications, containing the shared logic
/// for the process of withdrawing an application, such as removing from the public register if necessary, sending
/// notifications and publishing audit events. This class is intended to be inherited by specific implementations
/// for different contexts, such as external user withdrawals and internal system withdrawals, allowing for reuse
/// of the common withdrawal logic while adhering to different contracts defined for each context. The constructor
/// takes in all dependencies required for the withdrawal process, which are then used in the shared methods for
/// handling the withdrawal steps. Specific implementations can also add additional dependencies or override methods
/// as needed for their specific context.
/// </summary>
/// <param name="getFellingLicenceApplicationServiceForExternalUsers">A service to retrieve felling licence details.</param>
/// <param name="fellingLicenceApplicationExternalRepository">A repository class, used to start a transaction around the various
/// processes in the withdrawal.</param>
/// <param name="withdrawFellingLicenceService">A service class to perform the required data updates on the application for withdrawal.</param>
/// <param name="auditService">An auditing service to record outcomes.</param>
/// <param name="clock">A service to get the current date and time.</param>
/// <param name="publicRegisterService">A service to interact with the consultation public register.</param>
/// <param name="getPropertyProfilesService">A service to retrieve the property profile details linked to the application.</param>
/// <param name="getConfiguredFcAreasService">A service to retrieve the admin hub details that the application falls under.</param>
/// <param name="woodlandOwnerService">A service to retrieve details of the woodland owner for the application.</param>
/// <param name="retrieveExternalAccountsService">A service to retrieve external applicant user accounts.</param>
/// <param name="internalUserAccountService">A service to retrieve internal FC user accounts.</param>
/// <param name="sendNotifications">A service to send notifications.</param>
/// <param name="requestContext">The context of the current request.</param>
/// <param name="logger">A logging implementation.</param>
public abstract class WithdrawApplicationUseCaseBase(
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers,
    IFellingLicenceApplicationExternalRepository fellingLicenceApplicationExternalRepository,
    IWithdrawFellingLicenceService withdrawFellingLicenceService,
    IAuditService<WithdrawApplicationUseCaseBase> auditService,
    IClock clock,
    IPublicRegister publicRegisterService,
    IGetPropertyProfiles getPropertyProfilesService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IRetrieveUserAccountsService retrieveExternalAccountsService,
    IUserAccountService internalUserAccountService,
    ISendNotifications sendNotifications,
    RequestContext requestContext,
    ILogger<WithdrawApplicationUseCaseBase> logger) 
{
    private readonly IGetFellingLicenceApplicationForExternalUsers _getFellingLicenceApplicationServiceForExternalUsers
        = Guard.Against.Null(getFellingLicenceApplicationServiceForExternalUsers);
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository =
        Guard.Against.Null(fellingLicenceApplicationExternalRepository);
    private readonly IWithdrawFellingLicenceService _withdrawFellingLicenceService =
        Guard.Against.Null(withdrawFellingLicenceService);
    private readonly IClock _clock = Guard.Against.Null(clock);
    private readonly IPublicRegister _publicRegisterService = Guard.Against.Null(publicRegisterService);
    private readonly IGetPropertyProfiles _getPropertyProfilesService = Guard.Against.Null(getPropertyProfilesService);
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
    private readonly IRetrieveWoodlandOwners _woodlandOwnerService = Guard.Against.Null(woodlandOwnerService);
    private readonly IRetrieveUserAccountsService _retrieveExternalAccountsService
        = Guard.Against.Null(retrieveExternalAccountsService);
    private readonly IUserAccountService _internalUserAccountService = Guard.Against.Null(internalUserAccountService);
    private readonly ISendNotifications _sendNotifications = Guard.Against.Null(sendNotifications);

    private readonly IAuditService<WithdrawApplicationUseCaseBase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    

    protected async Task<Result> WithdrawApplicationAsync(
        Guid applicationId,
        UserAccessModel userAccess,
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonsOtherDetails,
        string externalLinkToApplication,
        string internalLinkToApplication,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to withdraw application {ApplicationId} for user {User}", 
            applicationId, userAccess.IsSystemUser ? "System" : userAccess.UserAccountId);

        await using var transaction = await _fellingLicenceApplicationRepository.BeginTransactionAsync(cancellationToken);

        Guid? woodlandOwnerId = null;

        try
        {
            // set the application to withdrawn and store the reasons
            var withdrawalResult = await _withdrawFellingLicenceService.WithdrawApplicationAsync(
                applicationId, userAccess, withdrawalReasons, withdrawalReasonsOtherDetails, cancellationToken);

            if (withdrawalResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.FellingLicenceApplicationWithdrawFailure,
                        applicationId,
                        userAccess.UserAccountId,
                        _requestContext,
                        new
                        {
                            Section = "Withdraw FLA",
                            withdrawalResult.Error
                        }), cancellationToken).ConfigureAwait(false);
                logger.LogError(
                    "Could not withdraw application {ApplicationId} when requested, error: {Error}",
                    applicationId,
                    withdrawalResult.Error);
                return Result.Failure("Failed to withdraw the application");
            }


            var (_, isFailure, fellingLicenceApplication) = await _getFellingLicenceApplicationServiceForExternalUsers
                .GetApplicationByIdAsync(applicationId, userAccess, cancellationToken)
                .ConfigureAwait(false);

            if (isFailure || fellingLicenceApplication.LinkedPropertyProfile is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.FellingLicenceApplicationWithdrawFailure,
                        applicationId,
                        userAccess.UserAccountId,
                        _requestContext,
                        new
                        {
                            Section = "Withdraw FLA",
                            Error = $"Application {applicationId} {(isFailure ? "could not be retrieved" : "has no linked property profile")}"
                        }), cancellationToken).ConfigureAwait(false);

                logger.LogError("Application {ApplicationId} {Error} as part of withdrawal process",
                    applicationId, isFailure ? "could not be retrieved" : "has no linked property profile");

                return Result.Failure("Failed to retrieve application for sending notifications of withdrawal");
            }

            woodlandOwnerId = fellingLicenceApplication.WoodlandOwnerId;

            // remove from the PR if necessary
            if (fellingLicenceApplication.PublicRegister.ShouldApplicationBeRemovedFromConsultationPublicRegister())
            {
                logger.LogDebug("Attempting to remove application {ApplicationId} from the consultation public register as part of withdrawing the application", applicationId);

                var removeFromPrResult =
                    await RemoveFromPublicRegisterAsync(fellingLicenceApplication, userAccess, cancellationToken).ConfigureAwait(false);
                if (removeFromPrResult.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    await _auditService.PublishAuditEventAsync(
                        new AuditEvent(
                            AuditEvents.FellingLicenceApplicationWithdrawFailure,
                            applicationId,
                            userAccess.UserAccountId,
                            _requestContext,
                            new
                            {
                                WoodlandOwnerId = woodlandOwnerId,
                                Section = "Withdraw FLA",
                                removeFromPrResult.Error
                            }), cancellationToken).ConfigureAwait(false);
                    logger.LogError(
                        "Could not remove application {ApplicationId} from the public register as part of withdrawal process, error: {Error}",
                        applicationId,
                        removeFromPrResult.Error);
                    return Result.Failure("Could not remove the application from the public register");
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            //send notifications
            await SendNotificationsAsync(
                    fellingLicenceApplication, userAccess, withdrawalResult.Value, externalLinkToApplication, internalLinkToApplication, withdrawalReasons, withdrawalReasonsOtherDetails, cancellationToken)
                .ConfigureAwait(false);

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.FellingLicenceApplicationWithdrawComplete,
                    applicationId,
                    userAccess.UserAccountId,
                    _requestContext,
                    new { WoodlandOwner = woodlandOwnerId }),
                cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (Exception ex)
        {
                logger.LogError(ex, "An error occurred while withdrawing application {ApplicationId}", applicationId);

                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.FellingLicenceApplicationWithdrawFailure,
                        applicationId,
                        userAccess.UserAccountId,
                        _requestContext,
                        new { WoodlandOwner = woodlandOwnerId, Error = ex.Message }), 
                        cancellationToken)
                    .ConfigureAwait(false);

                return Result.Failure($"Withdrawal failure, application id: {applicationId}, error: {ex.Message}");
        }
    }


    private async Task<Result> RemoveFromPublicRegisterAsync(
        FellingLicenceApplication application,
        UserAccessModel user,
        CancellationToken cancellationToken)
    {
        var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();

        logger.LogDebug("Attempting to remove application {ApplicationId} from the public register", application.Id);

        var publicRegisterRemovalResult = await _publicRegisterService.RemoveCaseFromConsultationRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            timestamp,
            cancellationToken).ConfigureAwait(false);

        if (publicRegisterRemovalResult.IsFailure)
        {
            logger.LogError(
                "Could not remove application {ApplicationId} from the public register, error: {Error}",
                application.Id,
                publicRegisterRemovalResult.Error);
            return Result.Failure(publicRegisterRemovalResult.Error);
        }

        logger.LogDebug("Attempting to update woodland officer review of application {ApplicationId} for public register removal",
            application.Id);

        var updateResult = await withdrawFellingLicenceService.UpdatePublicRegisterEntityToRemovedAsync(
            application.Id,
            user.IsSystemUser ? null : user.UserAccountId,
            timestamp,
            cancellationToken).ConfigureAwait(false);

        if (updateResult.IsFailure)
        {
            logger.LogError(
                "Could not update the public register data for application {ApplicationId}, error: {Error}",
                application.Id,
                updateResult.Error);
            return Result.Failure(updateResult.Error);
        }

        return Result.Success();
    }

    private async Task<Result> SendNotificationsAsync(
        FellingLicenceApplication application,
        UserAccessModel user,
        List<Guid> assignedInternalUsers,
        string externalAppLinkToApplication,
        string internalLinkToApplication,
        List<WithdrawalReason> reasons,
        string? withdrawalReasonOtherDetails,
        CancellationToken cancellationToken)
    {
        var withdrawnFromState = application.GetNthStatus(1);

        string? propertyName;

        if (FellingLicenceStatusConstants.SubmitStatuses.Contains(withdrawnFromState.Value))
        {
            logger.LogDebug("Attempting to load property profile {PropertyProfileId} for application {ApplicationId}",
                application.LinkedPropertyProfile?.PropertyProfileId, application.Id);

            var propertyResult = await _getPropertyProfilesService
                .GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, user, cancellationToken)
                .ConfigureAwait(false);
            
            if (propertyResult.IsFailure)
            {
                logger.LogError("Failed to get Property Profile with ID {PropertyProfileId}", application.LinkedPropertyProfile.PropertyProfileId);
                propertyName = application.SubmittedFlaPropertyDetail?.Name;
            }
            else
            {
                propertyName = propertyResult.Value.Name;
            }
        }
        else
        {
            propertyName = application.SubmittedFlaPropertyDetail?.Name;
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(application.AdministrativeRegion)
            ? string.Empty
            : await _getConfiguredFcAreasService
                .TryGetAdminHubAddress(application.AdministrativeRegion, cancellationToken)
                .ConfigureAwait(false);

        logger.LogDebug("Attempting to load woodland owner {WoodlandOwnerId} details for application {ApplicationId}",
            application.WoodlandOwnerId, application.Id);

        var woodlandOwnerResult = await _woodlandOwnerService
            .RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, user, cancellationToken)
            .ConfigureAwait(false);

        if (woodlandOwnerResult.IsFailure)
        {
            logger.LogError("Failed to retrieve woodland owner {WoodlandOwnerId} details, error: {Error}",
                application.WoodlandOwnerId, woodlandOwnerResult.Error);
            return Result.Failure("Failed to retrieve woodland owner details");
        }

        //send the notification to either the applicant user that just processed the withdrawal, or the creator of the application,
        //and send a copy to the woodland owner contact address if that is not the same address

        Guid externalApplicantId;
        AssignedUserRole assignedRole;
        if (user.IsSystemUser || user.IsFcUser)
        {
            externalApplicantId = application.CreatedById;
            assignedRole = AssignedUserRole.Author;
        }
        else
        {
            externalApplicantId = user.UserAccountId;
            assignedRole = AssignedUserRole.Applicant;
        }

        logger.LogDebug("Attempting to load applicant {ApplicantId} details for application {ApplicationId}",
        externalApplicantId, application.Id);

        var externalApplicant = await _retrieveExternalAccountsService
            .RetrieveUserAccountByIdAsync(externalApplicantId, cancellationToken)
            .ConfigureAwait(false);

        if (externalApplicant.IsFailure)
        {
            logger.LogError("Failed to retrieve external applicant {ApplicantId} details, error: {Error}",
                externalApplicantId, externalApplicant.Error);

            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.FellingLicenceApplicationWithdrawNotificationSentFailed,
                    application.Id,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        RecipientId = externalApplicantId,
                        Error = externalApplicant.Error
                    }),
                cancellationToken).ConfigureAwait(false);

            return Result.Failure("Failed to retrieve external applicant details");
        }

        var reasonStrings = reasons.Where(x => x != WithdrawalReason.Other).Select(x => x.GetDisplayName()!).ToList();
        if (reasons.Contains(WithdrawalReason.Other) && !string.IsNullOrWhiteSpace(withdrawalReasonOtherDetails))
        {
            reasonStrings.Add($"Other - {withdrawalReasonOtherDetails}");
        }

        var applicationWithdrawnModel = new ApplicationWithdrawnConfirmationDataModel
        {
            ApplicationReference = application.ApplicationReference,
            PropertyName = propertyName,
            Name = externalApplicant.Value.FullName!,
            ViewApplicationURL = externalAppLinkToApplication,
            AdminHubFooter = adminHubFooter,
            ApplicationId = application.Id,
            ReasonForWithdrawal = reasonStrings
        };

        logger.LogDebug("Sending applicant confirmation of withdrawal notification for application {ApplicationId}", application.Id);

        var applicantNotificationResult = await _sendNotifications.SendNotificationAsync(
            applicationWithdrawnModel,
            NotificationType.ApplicationWithdrawnConfirmation,
            new NotificationRecipient(externalApplicant.Value.Email, externalApplicant.Value.FullName),
            copyToRecipients: woodlandOwnerResult.IsSuccess ? GetWoodlandOwnerCopyToRecipient(externalApplicant.Value.Email, woodlandOwnerResult.Value) : [],
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (applicantNotificationResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.FellingLicenceApplicationWithdrawNotificationSent,
                    application.Id,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        RecipientId = externalApplicantId,
                        RecipientName = externalApplicant.Value.FullName,
                        RecipientEmail = externalApplicant.Value.Email,
                        RecipientRole = assignedRole,
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.FellingLicenceApplicationWithdrawNotificationSentFailed,
                    application.Id,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        RecipientId = externalApplicantId,
                        RecipientName = externalApplicant.Value.FullName,
                        RecipientEmail = externalApplicant.Value.Email,
                        RecipientRole = assignedRole,
                        Error = applicantNotificationResult.Error
                    }),
                cancellationToken).ConfigureAwait(false);
        }

        //send a notification to all internal users that were assigned to the application 
        //prior to it being withdrawn

        if (!assignedInternalUsers.Any())
        {
            logger.LogDebug("No internal users assigned to application {ApplicationId} to send withdrawal notification to",
                application.Id);
            return Result.Success();
        }

        logger.LogDebug("Attempting to load internal users assigned to application {ApplicationId}", application.Id);

        var internalUsers = await _internalUserAccountService
            .RetrieveUserAccountsByIdsAsync(assignedInternalUsers, cancellationToken)
            .ConfigureAwait(false);

        if (internalUsers.IsSuccess)
        {
            logger.LogDebug("Sending notifications of application {ApplicationId} withdrawal to assigned internal users");
            foreach (var internalUser in internalUsers.Value)
            {
                var recipient = new NotificationRecipient(
                    internalUser.Email,
                    internalUser.FullName);

                var notificationModel = new ApplicationWithdrawnConfirmationDataModel
                {
                    ApplicationReference = application.ApplicationReference,
                    Name = recipient.Name!,
                    PropertyName = propertyName,
                    ViewApplicationURL = internalLinkToApplication,
                    AdminHubFooter = adminHubFooter,
                    ApplicationId = application.Id,
                    ReasonForWithdrawal = reasonStrings
                };

                var sendNotificationResult = await _sendNotifications.SendNotificationAsync(
                    notificationModel,
                    NotificationType.ApplicationWithdrawn,
                    recipient,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (sendNotificationResult.IsFailure)
                {
                    logger.LogError(
                        "Could not send notification for withdrawal of {ApplicationId} back to internal user (Id {InternalUserId}): {Error}",
                        application.Id,
                        internalUser.UserAccountId,
                        sendNotificationResult.Error);
                }
            }
        }
        else
        {
            logger.LogError("Failed to retrieve internal user details to send withdrawal notification for application {ApplicationId}", application.Id);
        }

        return Result.Success();
    }

    private static NotificationRecipient[]? GetWoodlandOwnerCopyToRecipient(
        string? mainToEmailAddress,
        WoodlandOwnerModel? woodlandOwner)
    {
        if (!string.IsNullOrWhiteSpace(woodlandOwner?.ContactEmail)
            && !woodlandOwner.ContactEmail.Equals(mainToEmailAddress, StringComparison.CurrentCultureIgnoreCase))
        {
            return
            [
                new NotificationRecipient(woodlandOwner.ContactEmail, woodlandOwner.ContactName)
            ];
        }

        return null;
    }
}