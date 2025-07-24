using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.InternalUsers.Repositories;

namespace Forestry.Flo.Services.InternalUsers.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly IAuditService<UserAccountService> _auditService;
        private readonly RequestContext _requestContext;
        private readonly IUserAccountRepository _userAccountRepository;

        public UserAccountService(
            IAuditService<UserAccountService> auditService,
            IUserAccountRepository userAccountRepository,
            RequestContext requestContext)
        {
            _auditService = auditService;
            _requestContext = requestContext;
            _userAccountRepository = Guard.Against.Null(userAccountRepository);
        }

        /// <inheritdoc />
        public async Task<UserAccount> CreateFcUserAccountAsync(string? identityProviderId, string email)
        {
            var userAccount = new UserAccount
            {
                IdentityProviderId = identityProviderId,
                AccountType = AccountTypeInternal.FcStaffMember, // Default value for initial account creation
                Title = null, 
                FirstName = null, 
                LastName = null,
                Email = email,
                Status = Status.Requested,
                Roles = RolesService.RolesStringFromList(new List<Roles> { Roles.FcUser })
            };

            CancellationToken cancellationToken = default;

            await _userAccountRepository.AddAsync(userAccount, cancellationToken);
            await _userAccountRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return userAccount;
        }

        /// <inheritdoc />
        public async Task<Maybe<UserAccount>> GetUserAccountAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            var userAccount = await _userAccountRepository.GetAsync(userAccountId, cancellationToken);

            return userAccount.IsSuccess 
                ? Maybe<UserAccount>.From(userAccount.Value) 
                : Maybe<UserAccount>.None;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<UserAccount>> ListNonConfirmedUserAccountsAsync(CancellationToken cancellationToken = default)
        {
            return await _userAccountRepository.GetUnconfirmedUserAccountsAsync(cancellationToken);
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<UserAccount>> ListConfirmedUserAccountsAsync(CancellationToken cancellationToken = default, List<Guid>? excludeUsers = null)
        {
            return await _userAccountRepository.GetConfirmedUserAccountsAsync(cancellationToken, excludeUsers);
        }

        /// <inheritdoc />
        public async Task<Maybe<UserAccount>> GetUserAccountByIdentityProviderIdAsync(
            string identityProviderId,
            CancellationToken cancellationToken)
        {
            var result = await _userAccountRepository.GetByIdentityProviderIdAsync(identityProviderId, cancellationToken);

            return result.IsSuccess
                ? Maybe<UserAccount>.From(result.Value)
                : Maybe<UserAccount>.None;
        }

        /// <inheritdoc />
        public async Task<Result<UserAccount>> UpdateUserAccountDetailsAsync(
            UpdateRegistrationDetailsModel userAccountModel, 
            CancellationToken cancellationToken)
        {
            var (_, isFailure, userAccount, error) = await _userAccountRepository.GetAsync(userAccountModel.UserAccountId, cancellationToken);

            if (isFailure)
            {
                return Result.Failure<UserAccount>(error.ToString());
            }

            userAccount.Title = userAccountModel.Title;
            userAccount.FirstName = userAccountModel.FirstName;
            userAccount.LastName = userAccountModel.LastName;
            userAccount.CanApproveApplications = userAccountModel.CanApproveApplications;
            userAccount.AccountTypeOther = userAccountModel.AccountType == AccountTypeInternal.Other
                ? userAccountModel.AccountTypeOther
                : null;

            if (userAccount.AccountType != userAccountModel.AccountType)
            {
                userAccount.AccountType = userAccountModel.AccountType;
                userAccount.Roles = RolesService.RolesStringFromList(userAccountModel.Roles);
            }

            await _userAccountRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return userAccount;
        }

        /// <inheritdoc />
        public async Task UpdateUserAccountConfirmedAsync(Guid userAccountId, bool? setCanApproveApplications, CancellationToken cancellationToken = default)
        {
            const bool userAccountConfirmed = true;

            await UpdateUserAccountConfirmedAsync(userAccountId, setCanApproveApplications, cancellationToken, userAccountConfirmed);
        }

        /// <inheritdoc />
        public async Task UpdateUserAccountDeniedAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            const bool userAccountConfirmed = false;

            await UpdateUserAccountConfirmedAsync(userAccountId, null, cancellationToken, userAccountConfirmed);
        }

        /// <inheritdoc />
        public async Task<Result<List<UserAccountModel>>> RetrieveUserAccountsByIdsAsync(
            List<Guid> ids,
            CancellationToken cancellationToken)
        {
            var (_, isFailure, result, error) = await _userAccountRepository.GetUsersWithIdsInAsync(ids, cancellationToken);

            return isFailure 
                ? Result.Failure<List<UserAccountModel>>(error.ToString()) 
                : MapUserAccountsToUserAccountModels(result).ToList();
        }

        /// <inheritdoc />
        public async Task<Result<UserAccountModel>> SetUserAccountStatusAsync(
            Guid userId,
            Status requestedStatus,
            CancellationToken cancellationToken)
        {
            var (_, isFailure, result, error) = await _userAccountRepository.GetAsync(userId, cancellationToken);

            if (isFailure)
            {
                return Result.Failure<UserAccountModel>($"Unable to retrieve user with id {userId}, error: {error}");
            }

            result.Status = requestedStatus;

            await _userAccountRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            var model = new UserAccountModel
            {
                AccountType = result.AccountType,
                Email = result.Email,
                FirstName = result.FirstName,
                LastName = result.LastName,
                Status = result.Status,
                Title = result.Title,
                UserAccountId = result.Id
            };

            return Result.Success(model);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<UserAccountModel>>> GetConfirmedUsersByAccountTypeAsync(
            AccountTypeInternal accountType, 
            AccountTypeInternalOther? accountTypeOther,
            CancellationToken cancellationToken)
        {
            var accounts = await _userAccountRepository.GetConfirmedUserAccountsByAccountTypeAsync(
                accountType, accountTypeOther, cancellationToken).ConfigureAwait(false);

            return Result.Success(MapUserAccountsToUserAccountModels(accounts));
        }

        private static IEnumerable<UserAccountModel> MapUserAccountsToUserAccountModels(IEnumerable<UserAccount> userAccounts)
        {
            return userAccounts.Select(x => new UserAccountModel
            {
                AccountType = x.AccountType,
                Email = x.Email,
                FirstName = x.FirstName,
                LastName = x.LastName,
                UserAccountId = x.Id,
                Title = x.Title,
                Status = x.Status
            }).ToList();
        }

        private async Task<Result> UpdateUserAccountConfirmedAsync(Guid userAccountId, bool? setCanApproveApplications, CancellationToken cancellationToken, bool userAccountConfirmed)
        {
            var (_, isFailure, userAccount) = await _userAccountRepository.GetAsync(userAccountId, cancellationToken);

            if (isFailure)
            {
                return Result.Failure($"Unable to retrieve user account with id {userAccountId}");
            }

            userAccount.Status = userAccountConfirmed
                ? Status.Confirmed
                : Status.Denied;

            if (setCanApproveApplications.HasValue)
            {
                userAccount.CanApproveApplications = setCanApproveApplications.Value;
            }

            await _userAccountRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            await AuditUserAccountEvent(AuditEvents.UpdateAccountEvent, userAccount, cancellationToken);

            return Result.Success();
        }

        private Task AuditUserAccountEvent(string eventName, UserAccount userAccount, CancellationToken cancellationToken = default) =>

            _auditService.PublishAuditEventAsync(new AuditEvent(
                eventName,
                userAccount.Id,
                userAccount.Id,
                _requestContext,
                new
                {
                    AccountType = userAccount.AccountType,
                    userAccount.IdentityProviderId
                })
                , cancellationToken
           );
    }
}

