using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;
using Forestry.Flo.Services.Common.Extensions;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Handles use case for automatic withdrawal and notifications to outstanding 'with applicant' applications 
/// </summary>
public class AutomaticWithdrawalNotificationUseCase(
    IClock clock,
    IWithdrawalNotificationService withdrawalNotificationService,
    IOptions<VoluntaryWithdrawalNotificationOptions> notificationOptions,
    ILogger<AutomaticWithdrawalNotificationUseCase> logger,
    IRetrieveUserAccountsService externalAccountService,
    ISendNotifications sendNotifications,
    RequestContext requestContext,
    IAuditService<AutomaticWithdrawalNotificationUseCase> auditService,
    IOptions<ExternalApplicantSiteOptions> externalApplicantSiteOptions,
    IRetrieveWoodlandOwners woodlandOwnersService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    IUserAccountRepository userAccountRepository,
    IFellingLicenceApplicationExternalRepository fellingLicenceApplicationRepository,
    IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
    IPublicRegister publicRegisterService)
{
    private readonly IWithdrawalNotificationService _withdrawalNotificationService = Guard.Against.Null(withdrawalNotificationService);
    private readonly IRetrieveUserAccountsService _externalAccountService = Guard.Against.Null(externalAccountService);
    private readonly VoluntaryWithdrawalNotificationOptions _notificationOptions = Guard.Against.Null(notificationOptions).Value;
    private readonly ISendNotifications _sendNotifications = Guard.Against.Null(sendNotifications);
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
    private readonly IClock _clock = Guard.Against.Null(clock);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly IAuditService<AutomaticWithdrawalNotificationUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly ExternalApplicantSiteOptions _externalApplicantSiteOptions = Guard.Against.Null(externalApplicantSiteOptions).Value;
    private readonly IRetrieveWoodlandOwners _woodlandOwnersService = Guard.Against.Null(woodlandOwnersService);
    private readonly IUserAccountRepository _userAccountRepository = Guard.Against.Null(userAccountRepository);
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);

    /// <summary>
    /// Processes applications that have exceeded the threshold for automatic withdrawal.
    /// </summary>
    /// <param name="viewFlaBaseUrl">The base URL for viewing felling licence applications.</param>
    /// <param name="withdrawFellingLicenceService">The service used to withdraw felling licence applications.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessApplicationsAsync(
        string viewFlaBaseUrl,
        IWithdrawFellingLicenceService withdrawFellingLicenceService,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Attempting to automatic withdrawal of applications that have exceeded ThresholdAutomaticWithdrawal:{ThresholdAutomaticWithdrawal}",
            _notificationOptions.ThresholdAutomaticWithdrawal);

        var (_, isFailure, relevantApplications, error) =
            await _withdrawalNotificationService.GetApplicationsAfterThresholdForWithdrawalAsync(
                _notificationOptions.ThresholdAutomaticWithdrawal,
                cancellationToken);

        if (isFailure)
        {
            logger.LogError("Unable to retrieve applications for withdrawal, error: {error}", error);
            return;
        }

        var userAccess = new UserAccessModel { IsFcUser = true };
        var currentDate = _clock.GetCurrentInstant().ToDateTimeUtc();

        foreach (var application in relevantApplications)
        {
            await using var transaction = await _fellingLicenceApplicationRepository.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var resultWithdrawal = await withdrawFellingLicenceService.WithdrawApplication(
                    application.ApplicationId,
                    userAccess,
                    cancellationToken).ConfigureAwait(false);

                if (resultWithdrawal.IsFailure)
                {
                    logger.LogError("Unable to withdraw application id {applicationId}, error: {error}",
                        application.ApplicationId, resultWithdrawal.Error);
                    await _auditService.PublishAuditEventAsync(
                        new AuditEvent(
                            AuditEvents.WithdrawFellingLicenceApplicationFailure,
                            application.ApplicationId,
                            null,
                            _requestContext,
                            new
                            {
                                Section = "Withdraw FLA",
                                resultWithdrawal.Error
                            }), cancellationToken)
                        .ConfigureAwait(false);
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var (_, isPublicRegisterFailure, (hasPublicRegister, publicRegister), publicRegisterError) =
                    await _getWoodlandOfficerReviewService.GetPublicRegisterDetailsAsync(
                        application.ApplicationId,
                        cancellationToken).ConfigureAwait(false);

                if (isPublicRegisterFailure)
                {
                    logger.LogError(
                        "Unable to retrieve public register details for application id {applicationId}, error: {error}",
                        application.ApplicationId,
                        publicRegisterError);
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (application.ApplicationReference is not null &&
                    hasPublicRegister &&
                    publicRegister.EsriId.HasValue &&
                    publicRegister.ConsultationPublicRegisterRemovedTimestamp.HasNoValue())
                {
                    var publicRegisterRemovalResult = await publicRegisterService.RemoveCaseFromConsultationRegisterAsync(
                        publicRegister.EsriId.Value,
                        application.ApplicationReference,
                        _clock.GetCurrentInstant().ToDateTimeUtc(),
                        cancellationToken).ConfigureAwait(false);

                    if (publicRegisterRemovalResult.IsFailure)
                    {
                        await _auditService.PublishAuditEventAsync(
                            new AuditEvent(
                                AuditEvents.WithdrawFellingLicenceApplicationFailure,
                                application.ApplicationId,
                                null,
                                _requestContext,
                                new
                                {
                                    Section = "Withdraw FLA",
                                    publicRegisterRemovalResult.Error
                                }), cancellationToken).ConfigureAwait(false);
                        logger.LogError(
                            "Could not remove the Felling Licence Application with ID {ApplicationId} from the public register",
                            application.ApplicationId);
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    
                    var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();

                    var updateResult = await withdrawFellingLicenceService.UpdatePublicRegisterEntityToRemovedAsync(
                        application.ApplicationId,
                        null,
                        timestamp,
                        cancellationToken);

                    if (updateResult.IsFailure)
                    {
                        logger.LogError(
                            "Unable to update public register details for application id {applicationId}, error: {error}",
                            application.ApplicationId,
                            updateResult.Error);
                        await _auditService.PublishAuditEventAsync(
                                new AuditEvent(
                                    AuditEvents.WithdrawFellingLicenceApplicationFailure,
                                    application.ApplicationId,
                                    null,
                                    _requestContext,
                                    new
                                    {
                                        Section = "Withdraw FLA",
                                        updateResult.Error
                                    }), cancellationToken)
                            .ConfigureAwait(false);
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    await _auditService.PublishAuditEventAsync(new AuditEvent(
                            AuditEvents.UpdateWoodlandOfficerReview,
                            application.ApplicationId,
                            null,
                            _requestContext,
                            new
                            {
                                Section = "Public Register"
                            }),
                        cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                await InformApplicantOfApplicationWithdrawalOption(
                    application,
                    currentDate,
                    cancellationToken);

                await InformFcUsersOfApplicationWithdrawal(
                    resultWithdrawal.Value,
                    application,
                    viewFlaBaseUrl,
                    cancellationToken);

                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.WithdrawFellingLicenceApplication,
                        application.ApplicationId,
                        null,
                        _requestContext,
                        new
                        {
                            application.ApplicationId,
                            WithdrawalDate = _clock.GetCurrentInstant().ToDateTimeUtc().Date
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing application id {applicationId}", application.ApplicationId);
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.WithdrawFellingLicenceApplicationFailure,
                        application.ApplicationId,
                        null,
                        _requestContext,
                        new
                        {
                            Section = "Withdraw FLA",
                            Error = ex.Message
                        }), cancellationToken).ConfigureAwait(false);
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        logger.LogDebug("Automatic withdrawal of {withdrawnApplications} applications", relevantApplications.Count);
    }

    private async Task InformApplicantOfApplicationWithdrawalOption(
        VoluntaryWithdrawalNotificationModel application,
        DateTime currentDate,
        CancellationToken cancellationToken)
    {
        var (_, createdByUserFailure, createdByUser, error) =
            await _externalAccountService.RetrieveUserAccountByIdAsync(
                application.CreatedById,
                cancellationToken);

        var (woodlandOwnerSuccess, _, woodlandOwner) =
            await _woodlandOwnersService.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, cancellationToken);

        if (createdByUserFailure)
        {
            logger.LogError("Unable to retrieve applicant user account of id {applicantId}, error: {error}",
                application.CreatedById, error);
            return;
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(application.AdministrativeRegion)
            ? string.Empty
            : await _getConfiguredFcAreasService
                .TryGetAdminHubAddress(application.AdministrativeRegion, cancellationToken)
                .ConfigureAwait(false);

        var applicantNotificationModel = new ApplicationWithdrawnConfirmationDataModel
        {
            ApplicationReference = application.ApplicationReference!,
            PropertyName = "n/a",
            Name = createdByUser.FullName,
            ViewApplicationURL = $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList/{application.ApplicationId}",
            AdminHubFooter = adminHubFooter
        };

        var applicantRecipient =
            new NotificationRecipient(createdByUser.Email, applicantNotificationModel.Name);

        var notificationResult = await _sendNotifications.SendNotificationAsync(
            applicantNotificationModel,
            NotificationType.ApplicationWithdrawnConfirmation,
            applicantRecipient,
            woodlandOwnerSuccess && woodlandOwner.ContactEmail != createdByUser.Email
                ? [new NotificationRecipient(woodlandOwner.ContactEmail!, woodlandOwner.ContactName)]
                : null,
            cancellationToken: cancellationToken);

        if (notificationResult.IsFailure)
        {
            logger.LogError("Unable to send automatic withdrawal notification to {recipient}, error: {error}",
                applicantRecipient.Address,
                notificationResult.Error);
        }
    }

    async Task InformFcUsersOfApplicationWithdrawal(
        IList<Guid> resultWithdrawal,
        VoluntaryWithdrawalNotificationModel fla,
        string linkToApplication,
        CancellationToken cancellationToken)
    {
        var (_, createdByUserFailure, createdByUser, error) = await _externalAccountService.RetrieveUserAccountByIdAsync(
            fla.CreatedById,
            cancellationToken);
        if (createdByUserFailure)
        {
            logger.LogError("Unable to retrieve creator user account when informing FC users of app withdrawal of id {userId}, error: {error}",
                fla.CreatedById, error);
            return;
        }

        if(resultWithdrawal.Count == 0)
        {
            return;
        }

        var internalUsers = await _userAccountRepository.GetUsersWithIdsInAsync(resultWithdrawal, cancellationToken);
        if (internalUsers.IsFailure)
        {
            logger.LogError("Unable to retrieve internal users when informing FC users of app withdrawal, error: {error}", internalUsers.Error);
            return;
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(fla.AdministrativeRegion)
            ? string.Empty
            : await _getConfiguredFcAreasService
                .TryGetAdminHubAddress(fla.AdministrativeRegion, cancellationToken)
                .ConfigureAwait(false);

        foreach (var internalUser in internalUsers.Value)
        {
            var recipient = new NotificationRecipient(
                internalUser.Email,
                internalUser.FullName(false));

            var notificationModel = new ApplicationWithdrawnConfirmationDataModel
            {
                ApplicationReference = fla.ApplicationReference!,
                Name = recipient.Name!,
                PropertyName = "n/a",
                ViewApplicationURL = linkToApplication,
                AdminHubFooter = adminHubFooter
            };

            var sendNotificationResult = await _sendNotifications.SendNotificationAsync(
                notificationModel,
                NotificationType.ApplicationWithdrawn,
                recipient,
                senderName: createdByUser.FullName,
                cancellationToken: cancellationToken);

            if (sendNotificationResult.IsFailure)
            {
                logger.LogError(
                    "Could not send notification for withdrawal of {ApplicationId} back to internal user (Id {internalUserId}): error: {error}",
                    internalUser.Id, fla.ApplicationId, sendNotificationResult.Error);
            }
        }
    }
}