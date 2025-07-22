using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Internal.Web.Services.AccountAdministration;

/// <summary>
/// Handles use case for an account administrator to retrieve external user accounts.
/// </summary>
public class GetApplicantUsersUseCase
{
    private readonly ILogger<GetApplicantUsersUseCase> _logger;
    private readonly IRetrieveUserAccountsService _externalAccountService;

    public GetApplicantUsersUseCase(
        ILogger<GetApplicantUsersUseCase> logger,
        IRetrieveUserAccountsService externalAccountService)
    {
        _logger = Guard.Against.Null(logger);
        _externalAccountService = Guard.Against.Null(externalAccountService);
    }

    /// <summary>
    /// Populates an <see cref="ExternalUserListModel"/> with all active external user accounts.
    /// </summary>
    /// <param name="internalUser">The internal user requesting the list.</param>
    /// <param name="returnUrl">A link to redirect to if the user selects cancel.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ExternalUserListModel"/>.</returns>
    /// <remarks>This method can only be performed by account administrators.</remarks>
    public async Task<Result<ExternalUserListModel>> RetrieveListOfActiveExternalUsersAsync(InternalUser internalUser, string returnUrl, CancellationToken cancellationToken)
    {
        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            _logger.LogDebug("User must be an {accountAdministrator} to retrieve agent user accounts, id: {id}", AccountTypeInternal.AccountAdministrator.GetDisplayName(), internalUser.UserAccountId);
            return Result.Failure<ExternalUserListModel>($"User must be an {AccountTypeInternal.AccountAdministrator.GetDisplayName()} to retrieve agent user accounts");
        }
        
        _logger.LogDebug("Retrieving all agent user accounts for user {id}", internalUser.UserAccountId);

        var userList = await _externalAccountService.RetrieveActiveExternalUsersByAccountTypeAsync(
            new List<AccountTypeExternal>()
            {
                AccountTypeExternal.AgentAdministrator,
                AccountTypeExternal.Agent,
                AccountTypeExternal.FcUser,
                AccountTypeExternal.WoodlandOwner,
                AccountTypeExternal.WoodlandOwnerAdministrator
            },
            cancellationToken);

        //todo - replace use of repository classes in usecases with calls to services instead; and services should
        //communicate using models not entities with the usecases.  then we can do the isfcagency/accounttype logic in the
        //service and not need the extra logic in the Select below...

        var results = new ExternalUserListModel
        {
            ExternalUserList = userList
                .Select(x =>
                {
                    var isFcUser = x.Agency?.IsFcAgency is true;
                    var model = new ExternalUserModel
                    {
                        AgencyModel = x.Agency is not null
                            ? ModelMapping.ToAgencyModel(x.Agency)
                            : null,
                        ExternalUser = ModelMapping.ToExternalUserAccountModel(x)
                    };
                    if (isFcUser)
                    {
                        model.ExternalUser.AccountType = AccountTypeExternal.FcUser;
                    }
                    return model;
                }),
            ReturnUrl = returnUrl
        };

        return Result.Success(results);
    }

    /// <summary>
    /// Retrieves a populated <see cref="ExternalUserModel"/> for a given user account.
    /// </summary>
    /// <param name="userId">The identifier for the external user account.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing a populated <see cref="ExternalUserModel"/> if successful, or an error if not.</returns>
    public async Task<Result<ExternalUserModel>> RetrieveExternalUserAsync(Guid userId, CancellationToken cancellationToken) 
     { 
        var user = await _externalAccountService.RetrieveUserAccountEntityByIdAsync(userId, cancellationToken);

        if (user.IsFailure)
        {
            return Result.Failure<ExternalUserModel>("Unable to retrieve user account");
        }

        return new ExternalUserModel
        {
            ExternalUser = ModelMapping.ToExternalUserAccountModel(user.Value),
            AgencyModel = user.Value.Agency is not null
                ? ModelMapping.ToAgencyModel(user.Value.Agency)
                : null
        };
     }
}