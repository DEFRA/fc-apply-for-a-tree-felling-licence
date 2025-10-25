using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Internal.Web.Services.AccountAdministration;

/// <summary>
/// Handles use case for an account administrator to retrieve external user accounts.
/// </summary>
public class GetApplicantUsersUseCase : IGetApplicantUsersUseCase
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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