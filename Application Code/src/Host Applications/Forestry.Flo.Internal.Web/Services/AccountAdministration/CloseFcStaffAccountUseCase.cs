using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services.AccountAdministration;

/// <summary>
/// Handles use case for an account administrator to close FC staff user accounts.
/// </summary>
public class CloseFcStaffAccountUseCase : ICloseFcStaffAccountUseCase
{
    private readonly ILogger<CloseFcStaffAccountUseCase> _logger;
    private readonly IUserAccountService _internalAccountService;
    private readonly IAuditService<CloseFcStaffAccountUseCase> _audit;
    private readonly RequestContext _requestContext;

    public CloseFcStaffAccountUseCase(
        ILogger<CloseFcStaffAccountUseCase> logger,
        IUserAccountService internalAccountService,
        IAuditService<CloseFcStaffAccountUseCase> audit,
        RequestContext requestContext)
    {
        _logger = Guard.Against.Null(logger);
        _internalAccountService = Guard.Against.Null(internalAccountService);
        _audit = Guard.Against.Null(audit);
        _requestContext = Guard.Against.Null(requestContext);
    }

    public async Task<Result<CloseUserAccountModel>> RetrieveUserAccountDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var userAccount = await _internalAccountService.GetUserAccountAsync(id, cancellationToken);

        if (userAccount.HasNoValue)
        {
            return Result.Failure<CloseUserAccountModel>($"Unable to retrieve user account model for user {id}");
        }

        return new CloseUserAccountModel
        {
            AccountToClose = ModelMapping.ToUserAccountModel(userAccount.Value)
        };
    }

    /// <summary>
    /// Sets the status of an internal user account to closed.
    /// </summary>
    /// <param name="userAccountId">The id of the user to close.</param>
    /// <param name="internalUser">The <see cref="InternalUser"/> performing the action.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result representing whether the account has been closed.</returns>
    public async Task<Result> CloseFcStaffAccountAsync(Guid userAccountId, InternalUser internalUser, CancellationToken cancellationToken)
    {
        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            _logger.LogDebug("User must be an {accountAdministrator} to close user accounts, requesting id: {id}, user id: {userId}",
                AccountTypeInternal.AccountAdministrator.GetDisplayName(), internalUser.UserAccountId, userAccountId);
            return Result.Failure($"User must be an {AccountTypeInternal.AccountAdministrator.GetDisplayName()} to close user accounts");
        }

        if (internalUser.UserAccountId == userAccountId)
        {
            _logger.LogDebug("User cannot close their own account, user id: {userId}", userAccountId);
            return Result.Failure("User cannot close their own account");
        }

        var (_, isFailure, userAccountModel, error) =
            await _internalAccountService.SetUserAccountStatusAsync(userAccountId, Status.Closed, cancellationToken);

        if (isFailure)
        {
            _logger.LogError(error);

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.AccountAdministratorCloseAccountFailure,
                    userAccountId,
                    internalUser.UserAccountId,
                    _requestContext,
                    new
                    {
                        Error = error
                    }),
                cancellationToken);

            return Result.Failure(error);
        }
        
        _logger.LogDebug("Closing FC staff user account with id {userId}, requested by: {internalUserId}", userAccountId, internalUser.UserAccountId);

        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.AccountAdministratorCloseAccount,
                userAccountId,
                internalUser.UserAccountId,
                _requestContext,
                new
                {
                    ClosedAccountFullName = userAccountModel.FullName,
                    ClosedAccountType = userAccountModel.AccountType
                }),
            cancellationToken);

        return Result.Success();
    }
}