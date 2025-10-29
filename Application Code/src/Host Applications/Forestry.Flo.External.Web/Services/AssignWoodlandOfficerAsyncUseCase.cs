using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services.MassTransit.Messages;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.External.Web.Services;

public class AssignWoodlandOfficerAsyncUseCase
{
    private readonly IAuditService<AssignWoodlandOfficerAsyncUseCase> _auditService;
    private readonly IGetFellingLicenceApplicationForExternalUsers _getFellingLicenceApplicationService;
    private readonly ISendNotifications _sendNotifications;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreas;
    private readonly ILogger<AssignWoodlandOfficerAsyncUseCase> _logger;
    private readonly RequestContext _requestContext;
    private readonly ISubmitFellingLicenceService _submitFellingLicenceService;
    private readonly InternalUserSiteOptions _internalUserSiteOptions;

    public AssignWoodlandOfficerAsyncUseCase(
        IAuditService<AssignWoodlandOfficerAsyncUseCase> auditService,
        RequestContext requestContext,
        ISubmitFellingLicenceService submitFellingLicenceService,
        IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationService,
        ISendNotifications sendNotifications,
        IGetConfiguredFcAreas getConfiguredFcAreas,
        ILogger<AssignWoodlandOfficerAsyncUseCase> logger,
        IOptions<InternalUserSiteOptions> internalUserSiteOptions)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _submitFellingLicenceService = submitFellingLicenceService ?? throw new ArgumentNullException(nameof(submitFellingLicenceService));
        _getFellingLicenceApplicationService = getFellingLicenceApplicationService ?? throw new ArgumentNullException(nameof(getFellingLicenceApplicationService));
        _sendNotifications = sendNotifications ?? throw new ArgumentNullException(nameof(sendNotifications));
        _getConfiguredFcAreas = Guard.Against.Null(getConfiguredFcAreas);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _internalUserSiteOptions = internalUserSiteOptions?.Value ?? throw new ArgumentNullException(nameof(internalUserSiteOptions));
    }

    /// <summary>
    /// Assigns a woodland officer to a felling licence application.
    /// </summary>
    /// <param name="message">A populated <see cref="AssignWoodlandOfficerMessage"/> containing data to assign a user to the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the internal user has been assigned to the application.</returns>
    public async Task<Result> AssignWoodlandOfficerAsync(
        AssignWoodlandOfficerMessage message,
        CancellationToken cancellationToken)
    {
        string errorMessage;

        var userAccModel = new UserAccessModel
        {
            IsFcUser = message.IsFcUser,
            AgencyId = message.AgencyId,
            UserAccountId = message.UserId,
            WoodlandOwnerIds = new List<Guid> { message.WoodlandOwnerId }
        };

        var (_, applicationRetrievalFailure, application) =
            await _getFellingLicenceApplicationService.GetApplicationByIdAsync(
                message.ApplicationId, 
                userAccModel,
                cancellationToken);

        if (applicationRetrievalFailure)
        {
            _logger.LogError("Unable to retrieve application with identifier {ApplicationId}, error: {Error}", message.ApplicationId, applicationRetrievalFailure.GetDescription());
            errorMessage = $"Unable to retrieve application with identifier {message.ApplicationId}";
            await PublishAssignWoodlandOfficerFailures(message, errorMessage, cancellationToken);

            return Result.Failure(errorMessage);
        }

        var internalUrl =
            $"{_internalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{message.ApplicationId}";

        var (isSuccess, _, autoAssignRecord) = await _submitFellingLicenceService.AutoAssignWoodlandOfficerAsync(
            message.ApplicationId,
            message.UserId,
            internalUrl,
            cancellationToken);

        if (isSuccess)
            await SendAssignmentNotificationAsync(
                autoAssignRecord,
                application,
                message.LinkToApplication,
                message.UserId,
                cancellationToken);

        return Result.Success();
    }

    private async Task SendAssignmentNotificationAsync(
        AutoAssignWoRecord autoAssignRecord,
        FellingLicenceApplication fellingLicenceApplication,
        string linkToApplication,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        var autoAssignRecipient = new NotificationRecipient(
        autoAssignRecord.AssignedUserEmail,
        $"{autoAssignRecord.AssignedUserFirstName} {autoAssignRecord.AssignedUserLastName}".Trim().Replace("  ", " "));

        var adminHubFooter = await
            _getConfiguredFcAreas.TryGetAdminHubAddress(fellingLicenceApplication.AdministrativeRegion, cancellationToken);

        var autoAssignNotificationModel = new UserAssignedToApplicationDataModel
        {
            ApplicationReference = fellingLicenceApplication.ApplicationReference,
            AssignedRole = AssignedUserRole.WoodlandOfficer.GetDisplayName()!,
            Name = autoAssignRecipient.Name!,
            ViewApplicationURL = linkToApplication,
            AdminHubFooter = adminHubFooter,
            ApplicationId = fellingLicenceApplication.Id
        };

        var sendNotificationAssigneeResult = await _sendNotifications.SendNotificationAsync(
            autoAssignNotificationModel,
            NotificationType.UserAssignedToApplication,
            autoAssignRecipient,
            cancellationToken: cancellationToken);

        if (sendNotificationAssigneeResult.IsFailure)
        {
            _logger.LogError("Could not send notification for assignment of user Id {UserId} to application {ApplicationId}: {Error}",
            autoAssignRecord.AssignedUserId, fellingLicenceApplication.Id, sendNotificationAssigneeResult.Error);
        }

        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.AssignFellingLicenceApplicationToStaffMember,
                fellingLicenceApplication.Id,
                performingUserId,
                _requestContext,
                new
                {
                    AssignedUserRole = autoAssignNotificationModel.AssignedRole,
                    AssignedStaffMemberId = autoAssignRecord.AssignedUserId,
                    UnassignedStaffMemberId = autoAssignRecord.UnassignedUserId,
                    NotificationSent = sendNotificationAssigneeResult.IsSuccess
                }),
            cancellationToken);
    }

    private async Task PublishAssignWoodlandOfficerFailures(
        AssignWoodlandOfficerMessage message,
        string errorMessage,
        CancellationToken cancellationToken,
        Exception? ex = null)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure,
                message.ApplicationId,
                message.UserId,
                _requestContext,
                new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = errorMessage
                }),
            cancellationToken);

        if (ex is not null)
        {
            _logger.LogError(ex,
                @"Error when assigning woodland officer, application id: {fellingLicenceApplicationId}",
                message.ApplicationId);
        }
    }
}