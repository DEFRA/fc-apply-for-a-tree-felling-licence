using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Models;

namespace Forestry.Flo.Services.InternalUsers.Services
{
    public interface IUserAccountService
    {
        Task<UserAccount> CreateFcUserAccountAsync(string? identityProviderId, string email);

        Task<Maybe<UserAccount>> GetUserAccountAsync(Guid userAccountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a <see cref="UserAccount"/> from an identity provider ID.
        /// </summary>
        /// <param name="identityProviderId">The user account's identity provided ID.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="UserAccount"/> entity.</returns>
        Task<Maybe<UserAccount>> GetUserAccountByIdentityProviderIdAsync(
            string identityProviderId,
            CancellationToken cancellationToken);

        Task<IEnumerable<UserAccount>> ListNonConfirmedUserAccountsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all confirmed user accounts.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <param name="excludeUsers">A list of user IDs to exclude from the list.</param>
        /// <returns>A populated list of <see cref="UserAccount"/></returns>
        Task<IEnumerable<UserAccount>> ListConfirmedUserAccountsAsync(CancellationToken cancellationToken = default, List<Guid>? excludeUsers = null);

        /// <summary>
        /// Updates the registration details for a given user account.
        /// </summary>
        /// <param name="userAccountModel">The updated user account details</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A result containing an updated <see cref="UserAccount"/>, if successful.</returns>
        Task<Result<UserAccount>> UpdateUserAccountDetailsAsync(UpdateRegistrationDetailsModel userAccountModel,
            CancellationToken cancellationToken);

        Task UpdateUserAccountConfirmedAsync(Guid userAccountId, bool? setCanApproveApplications, CancellationToken cancellationToken = default);

        Task UpdateUserAccountDeniedAsync(Guid userAccountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a list of <see cref="UserAccountModel"/> models from a list of IDs.
        /// </summary>
        /// <param name="ids">A list of user account ids.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of populated <see cref="UserAccountModel"/> instances.</returns>
        Task<Result<List<UserAccountModel>>> RetrieveUserAccountsByIdsAsync(List<Guid> ids, CancellationToken cancellationToken);

        /// <summary>
        /// Sets a user account's status to a requested <see cref="Status"/>.
        /// </summary>
        /// <param name="userId">The id of the user to set the status for.</param>
        /// <param name="requestedStatus">The requested <see cref="Status"/> for the user.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns></returns>
        Task<Result<UserAccountModel>> SetUserAccountStatusAsync(
            Guid userId,
            Status requestedStatus,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a list of confirmed user accounts, filtered by account type (and optionally account type other).
        /// </summary>
        /// <param name="accountType">The <see cref="AccountTypeInternal"/> to find users for.</param>
        /// <param name="accountTypeOther">The <see cref="AccountTypeInternalOther"/> to find users for; should only be provided if the
        /// given <see cref="AccountTypeInternal"/> is <see cref="AccountTypeInternal.Other"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of populated <see cref="UserAccountModel"/> representing the user accounts matching the filter parameters.</returns>
        Task<Result<IEnumerable<UserAccountModel>>> GetConfirmedUsersByAccountTypeAsync(
            AccountTypeInternal accountType,
            AccountTypeInternalOther? accountTypeOther,
            CancellationToken cancellationToken);
    }
}
