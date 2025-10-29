using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
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

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Handles the use case for identifying applications that have reached the end of their Public Register period
/// on the Decision Public Register, and then invokes the call to remove them from the Decision public Register,
/// finally sending notifications to the Approvers.
/// </summary>
public class RemoveApplicationsFromDecisionPublicRegisterUseCase : IRemoveApplicationsFromDecisionPublicRegisterUseCase
{
    private readonly IGetFellingLicenceApplicationForInternalUsers _getFellingLicenceApplicationService;
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceApplicationService;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService;
    private readonly IPublicRegister _publicRegister;
    private readonly IUserAccountService _internalAccountService;
    private readonly IClock _clock;
    private readonly ISendNotifications _sendNotifications;
    private readonly IAuditService<RemoveApplicationsFromDecisionPublicRegisterUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<RemoveApplicationsFromDecisionPublicRegisterUseCase> _logger;

    public RemoveApplicationsFromDecisionPublicRegisterUseCase(
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplicationService,
        IUpdateFellingLicenceApplication updateFellingLicenceApplicationService,
        IPublicRegister publicRegister,
        IUserAccountService internalAccountService,
        IClock clock,
        ISendNotifications sendNotifications,
        IAuditService<RemoveApplicationsFromDecisionPublicRegisterUseCase> auditService,
        RequestContext requestContext,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        ILogger<RemoveApplicationsFromDecisionPublicRegisterUseCase> logger)
    {
        _getFellingLicenceApplicationService = Guard.Against.Null(getFellingLicenceApplicationService);
        _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);
        _publicRegister = Guard.Against.Null(publicRegister);
        _internalAccountService = Guard.Against.Null(internalAccountService);
        _clock = Guard.Against.Null(clock);
        _sendNotifications = Guard.Against.Null(sendNotifications);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
       
        _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);
        _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
    }

    /// <summary>
    /// Removes Felling Licence Applications from the Decision Public Register when their expiry/end date is reached.
    /// </summary>
    /// <param name="viewApplicationBaseUrl">The base URL for viewing an application summary on the internal app.</param>
    /// <param name="cancellationToken">A cancellation Token</param>
    /// <returns></returns>
    public async Task ExecuteAsync(
        string viewApplicationBaseUrl,
        CancellationToken cancellationToken)
    {
        var relevantApplications = await _getFellingLicenceApplicationService.
            RetrieveFinalisedApplicationsHavingExpiredOnTheDecisionPublicRegisterAsync(
            cancellationToken);
     
        if (relevantApplications.Count == 0)
        {
            _logger.LogDebug("No applications were found which have expired since the last execution check");
            return;
        }

        _logger.LogDebug("Found {Count} applications on the Decision Public Register which have expired", 
            relevantApplications.Count);

        var totalUserIds = relevantApplications.SelectMany(x => x.AssignedUserIds!).Distinct().ToList();

        var (_, isFailure, retrievedUsers, error) = await _internalAccountService.RetrieveUserAccountsByIdsAsync(
            totalUserIds, cancellationToken);

        if (isFailure)
        {
            _logger.LogWarning("Unable to retrieve internal user accounts in InformAssignedFcStaffOfNearingPublicRegisterPeriodEndAsync, error: {error}", error);
        }

        var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();

        foreach (var relevantApplication in relevantApplications)
        {
            if (relevantApplication.PublicRegister != null)
            {
                if (relevantApplication.PublicRegister.EsriId.HasValue)
                {
                    var result =
                        await _publicRegister.RemoveCaseFromDecisionRegisterAsync(
                            relevantApplication.PublicRegister.EsriId.Value,
                            relevantApplication.ApplicationReference!, cancellationToken);

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
                             "from the actual Decision Public Register", modelData.ApplicationReference);

            updateFlaResult = await _updateFellingLicenceApplicationService.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
                modelData.PublicRegister!.FellingLicenceApplicationId,
                timestamp, cancellationToken);

            if (updateFlaResult.Value.IsSuccess)
            {
                _logger.LogDebug("Successfully added decision public register removal date to the Felling Application " +
                                 "with reference {CaseReference}", modelData.ApplicationReference);
            }
            else
            {
                _logger.LogError("Unable to add the decision public register removal date to the Felling Application " +
                                 "with reference {CaseReference} Error was {Error}", modelData.ApplicationReference,
                    updateFlaResult.Value.Error);
            }
        }

        if (gisResult.IsFailure)
        {
            _logger.LogError("Unable to remove the Felling Application with reference {CaseReference} " +
                             "from the Decision Public Register, Error was {Error}",
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
                string.Join(',',dataModel.AssignedUserIds ?? Array.Empty<Guid>()),
                dataModel.PublicRegister?.FellingLicenceApplicationId);
            return;
        }

        var adminHubFooter = await _getConfiguredFcAreasService.TryGetAdminHubAddress(dataModel.AdminHubName, cancellationToken);

        foreach (var userId in dataModel.AssignedUserIds!)
        {
            var user = retrievedUsers.FirstOrDefault(x => x.UserAccountId == userId);

            if (user is null)
            {
                _logger.LogError("Unable to retrieve internal user account for user {id}", userId);
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
                            dataModel.PublicRegister!.DecisionPublicRegisterExpiryTimestamp!.Value),
                    ViewApplicationURL =
                        $"{viewApplicationBaseUrl}/{dataModel.PublicRegister!.FellingLicenceApplicationId}",
                    PropertyName = dataModel.PropertyName,
                    AdminHubFooter = adminHubFooter,
                    PublishDate = DateTimeDisplay.GetDateDisplayString(
                        dataModel.PublicRegister.DecisionPublicRegisterPublicationTimestamp!.Value),
                    RegisterName = "Decision",
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
                _logger.LogError("Unable to send notification for removal of application from decision public register period end to " +
                                 "{address} for application {id}", recipient.Address, dataModel.PublicRegister.FellingLicenceApplicationId);
            }
        }

        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.DecisionPublicRegisterApplicationRemovalNotification,
                dataModel.PublicRegister!.FellingLicenceApplicationId,
                null,
                _requestContext,
                new
                {
                    PublicRegisterPeriodEndDate = dataModel.PublicRegister.DecisionPublicRegisterExpiryTimestamp,
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
                new AuditEvent(AuditEvents.DecisionPublicRegisterAutomatedExpirationRemovalSuccess,
                    modelData.PublicRegister!.FellingLicenceApplicationId,
                    null,
                    _requestContext,
                    new
                    {
                        CaseReference = modelData.ApplicationReference,
                        DecisionPublicRegisterPeriodEndDate = modelData.PublicRegister.DecisionPublicRegisterExpiryTimestamp,
                        LocalApplicationSetToRemoved = updateFlaResult is { IsSuccess: true }
                    }), cancellationToken);
            return;
        }

        //else is failure
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(AuditEvents.DecisionPublicRegisterAutomatedExpirationRemovalFailure,
                modelData.PublicRegister!.FellingLicenceApplicationId,
                null,
                _requestContext,
                new
                {
                    CaseReference = modelData.ApplicationReference,
                    DecisionPublicRegisterPeriodEndDate = modelData.PublicRegister.DecisionPublicRegisterExpiryTimestamp,
                    LocalApplicationSetToRemoved = updateFlaResult is { IsSuccess: true }
                }), cancellationToken);
    }
}