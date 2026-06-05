using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Options;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// A use case for reverting a felling license application from the withdrawn state.
/// </summary>
/// <remarks>
/// This use case ensures that only users with the <see cref="AccountTypeInternal.AccountAdministrator"/> role
/// can perform the operation. It also logs audit events for both success and failure scenarios.
/// </remarks>
/// <param name="auditService">The audit service used to log audit events.</param>
/// <param name="requestContext">The context of the current request.</param>
/// <param name="updateFellingLicenceApplicationService">The service used to update felling license applications.</param>
/// <param name="logger">The logger used to log information and errors.</param>
public class RevertApplicationFromWithdrawnUseCase(
    IAuditService<RevertApplicationFromWithdrawnUseCase> auditService,
    RequestContext requestContext,
    IUpdateFellingLicenceApplication updateFellingLicenceApplicationService,
    IGetPropertyProfiles getPropertyProfilesService,
    IRetrieveUserAccountsService retrieveUserAccountsService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    ISendNotifications sendNotificationsService,
    IOptions<ExternalApplicantSiteOptions> applicantSiteOptions,
    ILogger<RevertApplicationFromWithdrawnUseCase> logger) : IRevertApplicationFromWithdrawnUseCase
{
    private readonly IAuditService<RevertApplicationFromWithdrawnUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);
    private readonly IGetPropertyProfiles _getPropertyProfilesService = Guard.Against.Null(getPropertyProfilesService);
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
    private readonly ExternalApplicantSiteOptions _applicantSiteOptions = Guard.Against.Null(applicantSiteOptions).Value;
    private readonly ISendNotifications _sendNotificationsService = Guard.Against.Null(sendNotificationsService);

    /// <summary>
    /// Reverts a felling license application from the withdrawn state.
    /// </summary>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="applicationId">The unique identifier of the application to be reverted.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the operation.
    /// </returns>
    /// <remarks>
    /// Only users with the <see cref="AccountTypeInternal.AccountAdministrator"/> role are allowed to perform this operation.
    /// If the operation fails, an audit event is logged with the error details.
    /// </remarks>
    public async Task<Result> RevertApplicationFromWithdrawnAsync(
        InternalUser user,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to revert application {AppId} from withdrawn for user {UserId}", applicationId, user.UserAccountId);

        if (user.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            const string permissionError = "You do not have permission to revert applications from withdrawn";
            logger.LogWarning("User {UserId} is not an administrator and cannot revert applications from withdrawn.", user.UserAccountId);
            await AuditErrorAsync(
                user,
                applicationId,
                permissionError,
                cancellationToken);
            return Result.Failure(permissionError);
        }

        var result = await _updateFellingLicenceApplicationService.TryRevertApplicationFromWithdrawnAsync(
            user.UserAccountId!.Value,
            applicationId,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Failed to revert application {AppId} from withdrawn for user {UserId} with error: {Error}", applicationId, user.UserAccountId, result.Error);
            await AuditErrorAsync(
                user,
                applicationId,
                result.Error,
                cancellationToken);
            return result;
        }

        logger.LogInformation("Reverting application {AppId} from withdrawn for user {UserId}", applicationId, user.UserAccountId);
        await AuditSuccessAsync(
            user,
            applicationId,
            cancellationToken);

        await SendConfirmationNotificationAsync(user, result.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private async Task SendConfirmationNotificationAsync(
        InternalUser performingUser,
        ReopenApplicationResultModel dataModel, 
        CancellationToken cancellationToken)
    {
        var authorAccount = await _retrieveUserAccountsService
            .RetrieveUserAccountByIdAsync(dataModel.AuthorId, cancellationToken)
            .ConfigureAwait(false);

        if (authorAccount.IsFailure)
        {
            logger.LogError("Failed to retrieve application {ApplicationId} author {AuthorId} details to send confirmation, error: {Error}",
                dataModel.ApplicationId, dataModel.AuthorId, authorAccount.Error);

            await AuditNotificationErrorAsync(performingUser, dataModel.ApplicationId, null, authorAccount.Error, cancellationToken)
                .ConfigureAwait(false);

            return;
        }

        var recipient = new NotificationRecipient(authorAccount.Value.Email, authorAccount.Value.FullName);

        var propertyName = dataModel.PropertyName;
        if (string.IsNullOrWhiteSpace(propertyName) && dataModel.LinkedPropertyProfileId.HasValue)
        {
            var property = await _getPropertyProfilesService
                .GetPropertyByIdAsync(dataModel.LinkedPropertyProfileId.Value, UserAccessModel.SystemUserAccessModel, cancellationToken)
                .ConfigureAwait(false);

            if (property.IsFailure)
            {
                logger.LogError("Failed to retrieve property profile {PropertyProfileId} for application {ApplicationId} to send confirmation, error: {Error}",
                    dataModel.LinkedPropertyProfileId.Value, dataModel.ApplicationId, property.Error);

                await AuditNotificationErrorAsync(performingUser, dataModel.ApplicationId, recipient, property.Error, cancellationToken)
                    .ConfigureAwait(false);

                return;
            }

            propertyName = property.Value.Name;
        }

        var adminHubName = await _getConfiguredFcAreasService
            .TryGetAdminHubAddress(dataModel.AdminHubName, cancellationToken)
            .ConfigureAwait(false);

        var applicationUrl = $"{_applicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={dataModel.ApplicationId}";

        var notificationModel = new InformApplicantOfApplicationReopenedDataModel
        {
            ApplicationId = dataModel.ApplicationId,
            ApplicationReference = dataModel.ApplicationReference,
            Name = recipient.Name,
            PropertyName = propertyName,
            SubmittedDate = DateTimeDisplay.GetDateDisplayString(dataModel.SubmittedDate),
            AdminHubFooter = adminHubName,
            ViewApplicationURL = applicationUrl
        };

        var notificationResult = await _sendNotificationsService.SendNotificationAsync(
                notificationModel,
                NotificationType.InformApplicantOfApplicationReopened,
                recipient,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (notificationResult.IsFailure)
        {
            logger.LogError("Failed to send notification for application {ApplicationId} to recipient {Email}, error: {Error}",
                dataModel.ApplicationId, recipient.Address, notificationResult.Error);
            await AuditNotificationErrorAsync(performingUser, dataModel.ApplicationId, recipient,
                notificationResult.Error, cancellationToken).ConfigureAwait(false);
            return;
        }

        await AuditNotificationSuccessAsync(performingUser, dataModel.ApplicationId, recipient, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Logs an audit event for a failed attempt to revert an application from the withdrawn state.
    /// </summary>
    /// <param name="user">The user performing the operation.</param>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    private async Task AuditErrorAsync(
        InternalUser user,
        Guid applicationId,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.RevertApplicationFromWithdrawnFailure,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new
            {
                Error = error
            }), cancellationToken);
    }

    /// <summary>
    /// Logs an audit event for a failed attempt to send the notification for reverting an application from the withdrawn state.
    /// </summary>
    /// <param name="user">The user performing the operation.</param>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    private async Task AuditNotificationErrorAsync(
        InternalUser user,
        Guid applicationId,
        NotificationRecipient? recipient,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.RevertApplicationFromWithdrawnNotificationFailure,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new
            {
                RecipientName = recipient?.Name,
                RecipientEmail = recipient?.Address,
                Error = error
            }), cancellationToken);
    }

    /// <summary>
    /// Logs an audit event for a successful attempt to revert an application from the withdrawn state.
    /// </summary>
    /// <param name="user">The user performing the operation.</param>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    private async Task AuditSuccessAsync(
        InternalUser user,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.RevertApplicationFromWithdrawnSuccess,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new { }),
            cancellationToken);
    }

    /// <summary>
    /// Logs an audit event for a successful attempt to send the notification for reverting an application from the withdrawn state.
    /// </summary>
    /// <param name="user">The user performing the operation.</param>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    private async Task AuditNotificationSuccessAsync(
        InternalUser user,
        Guid applicationId,
        NotificationRecipient recipient,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.RevertApplicationFromWithdrawnNotificationSent,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    RecipientName = recipient.Name,
                    RecipientEmail = recipient.Address
                }),
            cancellationToken);
    }
}

