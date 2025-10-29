using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Services;

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
    ILogger<RevertApplicationFromWithdrawnUseCase> logger) : IRevertApplicationFromWithdrawnUseCase
{
    private readonly IAuditService<RevertApplicationFromWithdrawnUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);

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
        return Result.Success();
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
}

