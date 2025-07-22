using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Implementation of <see cref="IRetrieveUserAccountsService"/> that uses repository classes.
/// </summary>
public class RetrieveUserAccountsService : IRetrieveUserAccountsService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAgencyRepository _agencyRepository;
    private readonly ILogger<RetrieveUserAccountsService> _logger;

    public RetrieveUserAccountsService(
        IUserAccountRepository userAccountRepository,
        IAgencyRepository agencyRepository,
        ILogger<RetrieveUserAccountsService> logger)
    {
        ArgumentNullException.ThrowIfNull(userAccountRepository);
        ArgumentNullException.ThrowIfNull(agencyRepository);

        _userAccountRepository = userAccountRepository;
        _agencyRepository = agencyRepository;
        _logger = logger;
    }

    ///<inheritdoc />
    public async Task<Result<List<UserAccountModel>>> RetrieveUserAccountsForWoodlandOwnerAsync(
        Guid woodlandOwnerId, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to retrieve user accounts linked to woodland owner with id {WoodlandOwnerId}", woodlandOwnerId);

        try
        {
            var agencyForWoodlandOwner = await _agencyRepository.FindAgencyForWoodlandOwnerAsync(woodlandOwnerId, cancellationToken);

            if (agencyForWoodlandOwner.HasValue)
            {
                _logger.LogDebug("Woodland owner with id {WoodlandOwnerId} is managed by Agency with id {AgencyId}, attempting to retrieve users for that agency", woodlandOwnerId, agencyForWoodlandOwner.Value.Id);
                var agentAccounts =
                    await _userAccountRepository.GetUsersWithAgencyIdAsync(agencyForWoodlandOwner.Value.Id, cancellationToken);

                return MapUserAccountEntities(agencyForWoodlandOwner.Value.IsFcAgency, agentAccounts);
            }

            _logger.LogDebug("Woodland owner with id {WoodlandOwnerId} is not managed by an agency, attempting to retrieve accounts linked to the Woodland Owner", woodlandOwnerId);
            var accounts = await _userAccountRepository.GetUsersWithWoodlandOwnerIdAsync(woodlandOwnerId, cancellationToken);

            if (accounts.IsFailure && accounts.Error == UserDbErrorReason.NotFound)
            {
                _logger.LogDebug("No standard WO user accounts found for woodland owner id {WoodlandOwnerId}", woodlandOwnerId);
                return Result.Success(new List<UserAccountModel>(0));
            }

            if (accounts.IsFailure)
            {
                _logger.LogDebug("Failed to retrieve WO user accounts for woodland owner {WoodlandOwnerId}, error: {Error}", woodlandOwnerId, accounts.Error);
                return Result.Failure<List<UserAccountModel>>("Could not retrieve WO user accounts");
            }

            return MapUserAccountEntities(false, accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in RetrieveUserAccountsForWoodlandOwnerAsync");
            return Result.Failure<List<UserAccountModel>>(ex.Message);
        }
    }

    ///<inheritdoc />
    public async Task<Result<List<UserAccountModel>>> RetrieveUserAccountsForFcAgencyAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to retrieve user accounts linked to the FC agency");

        try
        {
            var agency = await _agencyRepository.FindFcAgency(cancellationToken);

            if (agency.HasNoValue)
            {
                _logger.LogWarning("Could not locate an Agency with IsFcAgency set to true");
                return Result.Failure<List<UserAccountModel>>("Could not locate an FC Agency");
            }

            _logger.LogDebug("FC Agency located, attempting to retrieve accounts for it");
            var accounts = await _userAccountRepository.GetUsersWithAgencyIdAsync(agency.Value.Id, cancellationToken);
            return MapUserAccountEntities(true, accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in RetrieveUserAccountsForFcAgencyAsync");
            return Result.Failure<List<UserAccountModel>>(ex.Message);
        }
        
    }

    ///<inheritdoc />
    public async Task<Result<bool>> IsUserAccountLinkedToFcAgencyAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to check if a user account is linked to the FC agency");

        try
        {
            var account = await _userAccountRepository.GetAsync(userAccountId, cancellationToken);
            if (account.IsFailure)
            {
                _logger.LogError("Could not retrieve a user account with id {UserAccountId}", userAccountId);
                return Result.Failure<bool>(account.Error.GetDescription());
            }

            if (account.Value.AgencyId.HasNoValue())
            {
                _logger.LogDebug("User account with id {UserAccountId} is not linked to any agency", userAccountId);
                return Result.Success(false);
            }

            var agency = await _agencyRepository.FindFcAgency(cancellationToken);

            if (agency.HasNoValue)
            {
                _logger.LogWarning("Could not locate an Agency with IsFcAgency set to true");
                return Result.Success(false);
            }

            return Result.Success(account.Value.AgencyId == agency.Value.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in IsUserAccountLinkedToFcAgencyAsync");
            return Result.Failure<bool>(ex.Message);
        }
    }

    private Result<List<UserAccountModel>> MapUserAccountEntities(
        bool isFcAgency,
        Result<IList<UserAccount>, UserDbErrorReason> getAccountsResult)
    {
        if (getAccountsResult.IsFailure)
        {
            _logger.LogError("Could not retrieve user accounts: error {Error}", getAccountsResult.Error);
            return Result.Failure<List<UserAccountModel>>(getAccountsResult.Error.GetDescription());
        }

        var result = getAccountsResult.Value.Select(x => new UserAccountModel
        {
            UserAccountId = x.Id,
            Email = x.Email,
            FirstName = x.FirstName,
            LastName = x.LastName,
            AccountType = isFcAgency ? AccountTypeExternal.FcUser : x.AccountType,
            Status = x.Status
        }).ToList();
        return Result.Success(result);
    }

    public async Task<Result<UserAccountModel>> RetrieveUserAccountByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, result, error) = await _userAccountRepository.GetAsync(id, cancellationToken);

        if (isFailure)
        {
            return Result.Failure<UserAccountModel>(error.ToString());
        }

        var userAccountModel = new UserAccountModel
        {
            AccountType = result.AccountType,
            Email = result.Email,
            FirstName = result.FirstName,
            LastName = result.LastName,
            UserAccountId = result.Id,
            Status = result.Status
        };

        return Result.Success(userAccountModel);
    }

    /// <inheritdoc />
    public async Task<IList<UserAccount>> RetrieveActiveExternalUsersByAccountTypeAsync(IList<AccountTypeExternal> accountTypes, CancellationToken cancellationToken)
    {
        return await _userAccountRepository.GetExternalUsersByAccountTypeAsync(accountTypes, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<UserAccount>> RetrieveUserAccountEntityByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _userAccountRepository.GetAsync(id, cancellationToken);

        return user.IsFailure
            ? Result.Failure<UserAccount>(user.Error.ToString())
            : user.Value;
    }

    /// <inheritdoc />
    public async Task<Result<List<UserAccount>>> RetrieveUsersLinkedToAgencyAsync(Guid agencyId,
        CancellationToken cancellationToken)
    {
        var result = await _userAccountRepository.GetUsersWithAgencyIdAsync(agencyId, cancellationToken);

        return result.IsSuccess
            ? result.Value.ToList()
            : Result.Failure<List<UserAccount>>(result.Error.ToString());
    }

    /// <inheritdoc />
    public async Task<Result<UserAccessModel>> RetrieveUserAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userResult = await _userAccountRepository.GetAsync(userId, cancellationToken).ConfigureAwait(false);

        if (userResult.IsFailure)
        {
            return Result.Failure<UserAccessModel>("Could not retrieve a user account with the given id");
        }

        var user = userResult.Value;
        if (user.IsWoodlandOwner())
        {
            return Result.Success(new UserAccessModel
            {
                UserAccountId = userId,
                IsFcUser = false,
                WoodlandOwnerIds = new List<Guid> { user.WoodlandOwnerId!.Value }
            });
        }

        if (user.AccountType == AccountTypeExternal.FcUser)
        {
            return Result.Success(new UserAccessModel
            {
                UserAccountId = userId,
                IsFcUser = true
            });
        }

        var agentAuthorities = await _agencyRepository.ListAuthoritiesByAgencyAsync(
            user.AgencyId!.Value, 
            new[] { AgentAuthorityStatus.Created , AgentAuthorityStatus.FormUploaded }, 
            cancellationToken).ConfigureAwait(false);

        if (agentAuthorities.IsFailure)
        {
            return Result.Failure<UserAccessModel>("Could not retrieve list of woodland owners for user");
        }

        var result = new UserAccessModel
        {
            UserAccountId = userId,
            IsFcUser = false,
            AgencyId = user.AgencyId,
            WoodlandOwnerIds = agentAuthorities.Value.Select(x => x.WoodlandOwner.Id).ToList()
        };

        return Result.Success(result);
    }

    ///<inheritdoc />
    public async Task<Maybe<UserAccountModel>> RetrieveUserAccountByEmailAddressAsync(string emailAddress, CancellationToken cancellationToken)
    {
        var queryByEmailResult = await _userAccountRepository.GetByEmailAsync(emailAddress, cancellationToken);

        if (queryByEmailResult.IsSuccess)
        {
            var responseModel = new UserAccountModel
            {
                UserAccountId = queryByEmailResult.Value.Id,
                Email = queryByEmailResult.Value.Email,
                FirstName = queryByEmailResult.Value.FirstName,
                LastName = queryByEmailResult.Value.LastName,
                AccountType = queryByEmailResult.Value.AccountType,
                Status = queryByEmailResult.Value.Status
            };

            return Maybe<UserAccountModel>.From(responseModel);
        }
        return Maybe<UserAccountModel>.None;
    }
}