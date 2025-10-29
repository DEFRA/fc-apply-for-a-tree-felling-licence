using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

/// <summary>
/// Handles use case for notifying assigned internal users of an application nearing the end of its public register period.
/// </summary>
public class PublicRegisterExpiryUseCase : IPublicRegisterExpiryUseCase
{
    private readonly IGetFellingLicenceApplicationForInternalUsers _getFellingLicenceApplicationService;
    private readonly IUserAccountService _internalAccountService;
    private readonly ISendNotifications _sendNotifications;
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceApplicationService;
    private readonly IPublicRegister _publicRegister;
    private readonly ILogger<PublicRegisterExpiryUseCase> _logger;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService;
    private readonly IClock _clock;
    private readonly RequestContext _requestContext;
    private readonly IAuditService<PublicRegisterExpiryUseCase> _auditService;

    /// <summary>
    /// Usecase to remove expired applications from the Consultation Public Register.
    /// </summary>
    /// <param name="getFellingLicenceApplicationService">A service to retrieve the applications requiring removal.</param>
    /// <param name="updateFellingLicenceApplicationService">A service to update the applications once they are removed.</param>
    /// <param name="publicRegister">A public register service to remove the applications.</param>
    /// <param name="logger">A logging implementation.</param>
    /// <param name="internalAccountService">A service to retrieve details of internal users.</param>
    /// <param name="sendNotifications">A service to send notifications.</param>
    /// <param name="requestContext">The context of the request.</param>
    /// <param name="auditService">A service to audit outcomes.</param>
    /// <param name="getConfiguredFcAreasService">A service to get admin hub details.</param>
    /// <param name="clock">A service to get the current date and time.</param>
    public PublicRegisterExpiryUseCase(
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplicationService,
        IUpdateFellingLicenceApplication updateFellingLicenceApplicationService,
        IPublicRegister publicRegister,
        ILogger<PublicRegisterExpiryUseCase> logger,
        IUserAccountService internalAccountService,
        ISendNotifications sendNotifications,
        RequestContext requestContext,
        IAuditService<PublicRegisterExpiryUseCase> auditService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IClock clock)
    {
        _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);
        _publicRegister = publicRegister;
        _logger = logger;
        _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
        _clock = Guard.Against.Null(clock);
        _internalAccountService = Guard.Against.Null(internalAccountService);
        _sendNotifications = Guard.Against.Null(sendNotifications);
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _getFellingLicenceApplicationService = Guard.Against.Null(getFellingLicenceApplicationService);
    }

    /// <summary>
    /// Removes Felling Licence Applications from the Consultation Public Register when their expiry/end date is reached.
    /// </summary>
    /// <param name="viewApplicationBaseUrl">The base URL for viewing an application summary on the internal app.</param>
    /// <param name="cancellationToken">A cancellation Token</param>
    public async Task RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(
        string viewApplicationBaseUrl,
        CancellationToken cancellationToken)
    {
        var relevantApplications = await _getFellingLicenceApplicationService.
            RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(cancellationToken);

        if (relevantApplications.Count == 0)
        {
            _logger.LogDebug("No applications were found which have expired since the last execution check");
            return;
        }

        _logger.LogDebug("Found {Count} applications on the Consultation Public Register which have expired",
            relevantApplications.Count);

        var totalUserIds = relevantApplications.SelectMany(x => x.AssignedUserIds!).Distinct().ToList();

        var (_, isFailure, retrievedUsers, error) = await _internalAccountService.RetrieveUserAccountsByIdsAsync(
            totalUserIds, cancellationToken);

        if (isFailure)
        {
            _logger.LogWarning("Unable to retrieve internal user accounts in InformAssignedFcStaffOfNearingPublicRegisterPeriodEndAsync, error: {Error}", error);
        }

        var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();

        foreach (var relevantApplication in relevantApplications)
        {
            if (relevantApplication.PublicRegister != null)
            {
                if (relevantApplication.PublicRegister.EsriId.HasValue)
                {
                    var result =
                        await _publicRegister.RemoveCaseFromConsultationRegisterAsync(
                            relevantApplication.PublicRegister.EsriId.Value,
                            relevantApplication.ApplicationReference!, 
                            timestamp,
                            cancellationToken);

                    await HandleResultAsync(
                        result,
                        relevantApplication,
                        retrievedUsers,
                        timestamp,
                        viewApplicationBaseUrl,
                        cancellationToken);
                }
                else
                {
                    _logger.LogError("The application having Id {Id}, although having a Public Register entry in the local system does not have an Esri Id, " +
                                     "so a removal cannot be performed", relevantApplication.PublicRegister.FellingLicenceApplicationId);
                }
            }
            else
            {
                //Should never get here, given we are using the entity to get the relevant applications to remove in the first place...
                _logger.LogError("The application does not have a Public Register entry in the local system");
            }
        }
    }

    private async Task HandleResultAsync(
        Result gisResult,
        PublicRegisterPeriodEndModel modelData,
        List<UserAccountModel>? retrievedUsers,
        DateTime timestamp,
        string viewApplicationBaseUrl,
        CancellationToken cancellationToken)
    {
        Result? updateFlaResult = null;

        if (gisResult.IsSuccess)
        {
            _logger.LogDebug("Successfully removed the Felling Application with reference {CaseReference} " +
                             "from the actual Consultation Public Register", modelData.ApplicationReference);

            updateFlaResult = await _updateFellingLicenceApplicationService.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
                modelData.PublicRegister!.FellingLicenceApplicationId,
                timestamp, 
                cancellationToken);

            if (updateFlaResult.Value.IsSuccess)
            {
                _logger.LogDebug("Successfully added consultation public register removal date to the Felling Application " +
                                 "with reference {CaseReference}", modelData.ApplicationReference);
            }
            else
            {
                _logger.LogError("Unable to add the consultation public register removal date to the Felling Application " +
                                 "with reference {CaseReference} Error was {Error}", modelData.ApplicationReference,
                    updateFlaResult.Value.Error);
            }
        }

        if (gisResult.IsFailure)
        {
            _logger.LogError("Unable to remove the Felling Application with reference {CaseReference} " +
                             "from the Consultation Public Register, Error was {Error}",
                modelData.ApplicationReference, gisResult.Error);

            await SendNotificationsAsync(modelData, retrievedUsers, viewApplicationBaseUrl, cancellationToken);
        }

        await AuditResultAsync(gisResult, updateFlaResult, modelData, cancellationToken);
    }

    private async Task SendNotificationsAsync(
        PublicRegisterPeriodEndModel dataModel,
        List<UserAccountModel>? retrievedUsers,
        string viewApplicationBaseUrl,
        CancellationToken cancellationToken)
    {
        var notificationsSent = 0;

        if (retrievedUsers is null || retrievedUsers.Count == 0)
        {
            _logger.LogError("Unable to retrieve approver user account(s), for UserIds of {UserIds} for sending of notification for application having" +
                             "Id of {ApplicationId}",
                string.Join(',', dataModel.AssignedUserIds ?? Array.Empty<Guid>()),
                dataModel.PublicRegister?.FellingLicenceApplicationId);
            return;
        }

        var adminHubFooter = await _getConfiguredFcAreasService.TryGetAdminHubAddress(dataModel.AdminHubName, cancellationToken);

        foreach (var userId in dataModel.AssignedUserIds!)
        {
            var user = retrievedUsers.FirstOrDefault(x => x.UserAccountId == userId);

            if (user is null)
            {
                _logger.LogError("Unable to retrieve internal user account for user {UserId}", userId);
                continue;
            }

            var recipient = new NotificationRecipient(user.Email, user.FullName);

            var notificationModel =
                new InformFCStaffOfDecisionPublicRegisterAutomaticRemovalOnExpiryDataModel
                {
                    ApplicationReference = dataModel.ApplicationReference!,
                    Name = user.FullName,
                    DecisionPublicRegisterExpiryDate =
                        DateTimeDisplay.GetDateDisplayString(
                            dataModel.PublicRegister!.ConsultationPublicRegisterExpiryTimestamp!.Value),
                    ViewApplicationURL =
                        $"{viewApplicationBaseUrl}/{dataModel.PublicRegister!.FellingLicenceApplicationId}",
                    PropertyName = dataModel.PropertyName,
                    AdminHubFooter = adminHubFooter,
                    PublishDate = DateTimeDisplay.GetDateDisplayString(
                        dataModel.PublicRegister.ConsultationPublicRegisterPublicationTimestamp!.Value),
                    RegisterName = "Consultation",
                    ApplicationId = dataModel.PublicRegister.FellingLicenceApplicationId
                };

            var notificationResult =
                await _sendNotifications.SendNotificationAsync(
                        notificationModel,
                        NotificationType.InformFcStaffOfApplicationRemovedFromDecisionPublicRegisterFailure,
                        recipient,
                        cancellationToken: cancellationToken)
                    .Map(() => notificationsSent++);

            if (notificationResult.IsFailure)
            {
                _logger.LogError("Unable to send notification for removal of application from consultation public register period end to " +
                                 "{EmailAddress} for application {ApplicationId}", recipient.Address, dataModel.PublicRegister.FellingLicenceApplicationId);
            }
        }

        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.ConsultationPublicRegisterApplicationRemovalNotification,
                dataModel.PublicRegister!.FellingLicenceApplicationId,
                null,
                _requestContext,
                new
                {
                    PublicRegisterPeriodEndDate = dataModel.PublicRegister.ConsultationPublicRegisterExpiryTimestamp,
                    NumberOfFcStaffNotificationRecipients = notificationsSent,
                    ApplicationRemovalSuccess = false
                }), cancellationToken);
    }

    private async Task AuditResultAsync(Result result,
        Result? updateFlaResult,
        PublicRegisterPeriodEndModel modelData,
        CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(AuditEvents.ConsultationPublicRegisterAutomatedExpirationRemovalSuccess,
                    modelData.PublicRegister!.FellingLicenceApplicationId,
                    null,
                    _requestContext,
                    new
                    {
                        CaseReference = modelData.ApplicationReference,
                        ConsultationPublicRegisterPeriodEndDate = modelData.PublicRegister.ConsultationPublicRegisterExpiryTimestamp,
                        LocalApplicationSetToRemoved = updateFlaResult is { IsSuccess: true }
                    }), cancellationToken);
            return;
        }

        //else is failure
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(AuditEvents.ConsultationPublicRegisterAutomatedExpirationRemovalFailure,
                modelData.PublicRegister!.FellingLicenceApplicationId,
                null,
                _requestContext,
                new
                {
                    CaseReference = modelData.ApplicationReference,
                    ConsultationPublicRegisterPeriodEndDate = modelData.PublicRegister.ConsultationPublicRegisterExpiryTimestamp,
                    LocalApplicationSetToRemoved = updateFlaResult is { IsSuccess: true }
                }), cancellationToken);
    }
}