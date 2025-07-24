using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services.AccountAdministration;

/// <summary>
/// Handles use case for an account administrator to retrieve all FC staff user accounts.
/// </summary>
public class GetFcStaffMembersUseCase
{
    private readonly ILogger<GetFcStaffMembersUseCase> _logger;
    private readonly IUserAccountService _internalAccountService;

    public GetFcStaffMembersUseCase(
        ILogger<GetFcStaffMembersUseCase> logger,
        IUserAccountService internalAccountService)
    {
        _logger = Guard.Against.Null(logger);
        _internalAccountService = Guard.Against.Null(internalAccountService);
    }

    /// <summary>
    /// Retrieves a list of <see cref="UserAccountModel"/> models for every FC staff member.
    /// </summary>
    /// <param name="internalUser">The internal user requesting the list.</param>
    /// <param name="returnUrl">The return url to redirect to if the user presses cancel.</param>
    /// <param name="includeLoggedInUser">Whether the logged in user will appear in this list.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated list of <see cref="UserAccountModel"/> models.</returns>
    /// <remarks>This method can only be performed by account administrators.</remarks>
    public async Task<Result<FcStaffListModel>> GetAllFcStaffMembersAsync(InternalUser internalUser, string returnUrl, bool includeLoggedInUser, CancellationToken cancellationToken)
    {
        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            _logger.LogDebug("User must be an {accountAdministrator} to retrieve all user accounts, id: {id}", AccountTypeInternal.AccountAdministrator.GetDisplayName(), internalUser.UserAccountId);
            return Result.Failure<FcStaffListModel>($"User must be an {AccountTypeInternal.AccountAdministrator.GetDisplayName()} to retrieve all user accounts");
        }
        
        _logger.LogDebug("Retrieving all FC staff member user accounts for user {id}", internalUser.UserAccountId);

        var userList = await _internalAccountService.ListConfirmedUserAccountsAsync(
            cancellationToken,
            !includeLoggedInUser 
                ? new List<Guid>{internalUser.UserAccountId!.Value}
                : null);

        return new FcStaffListModel
        {
            FcStaffList = ModelMapping.ToUserAccountModels(userList),
            ReturnUrl = returnUrl
        };
    }
}