using CSharpFunctionalExtensions;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.InternalUsers.Repositories;

public interface IUserAccountRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Adds given User account to the database table.
    /// </summary>
    /// <param name="userAccount">User account to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The added <see cref="UserAccount"/> entity.</returns>
    Task<UserAccount> AddAsync(UserAccount userAccount, CancellationToken cancellationToken);

    /// <summary>
    /// Updates given User account in the database table
    /// </summary>
    /// <param name="userAccount">User account to update</param>
    void Update(UserAccount userAccount);

    /// <summary>
    /// Retrieves a user account by the given id
    /// </summary>
    /// <param name="id">The user account Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result object with the requested user account or an error reason</returns>
    Task<Result<UserAccount, UserDbErrorReason>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a user account by the given email address
    /// </summary>
    /// <param name="email">The user account email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result object with the requested user account or an error reason</returns>
    Task<Result<UserAccount, UserDbErrorReason>> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a user account by the given identity provider ID.
    /// </summary>
    /// <param name="identityProviderId">The identity provider ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Result object with the requested user account or an error reason.</returns>
    Task<Result<UserAccount, UserDbErrorReason>> GetByIdentityProviderIdAsync(string identityProviderId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves user accounts by a given full name.
    /// </summary>
    /// <param name="firstName">The user account first name.</param>
    /// <param name="lastName">The user account last name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="UserAccount"/> entities, if located with the given values, otherwise an empty list.</returns>
    /// <remarks>This should only be used to match users that we can't retrieve any unique identifiers for.</remarks>
    Task<IList<UserAccount>> GetByFullnameAsync(string firstName, string lastName, CancellationToken cancellationToken);
    /// <summary>
    /// Retrieves user accounts by a given full name including the title.
    /// </summary>
    /// <param name="title">The user account title.</param>
    /// <param name="firstName">The user account first name.</param>
    /// <param name="lastName">The user account last name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="UserAccount"/> entities, if located with the given values, otherwise an empty list.</returns>
    /// <remarks>This should only be used to match users that we can't retrieve any unique identifiers for.</remarks>
    Task<IList<UserAccount>> GetByFullnameAsync(string title, string firstName, string lastName, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve users where ID is in userIds list
    /// </summary>
    /// <param name="userIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithIdsInAsync(IList<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all confirmed internal user accounts.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="excludeIds">A list of IDs to exclude.</param>
    Task<IEnumerable<UserAccount>> GetConfirmedUserAccountsAsync(CancellationToken cancellationToken, List<Guid>? excludeIds = null);

    /// <summary>
    /// Retrieves all unconfirmed internal user accounts.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IEnumerable<UserAccount>> GetUnconfirmedUserAccountsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of confirmed user accounts that match given account type filter parameters.
    /// </summary>
    /// <param name="accountType">The account type to filter the retrieved accounts by.</param>
    /// <param name="accountTypeOther">The account type other to filter the retrieved accounts by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="UserAccount"/> matching the given filters.</returns>
    Task<IEnumerable<UserAccount>> GetConfirmedUserAccountsByAccountTypeAsync(
        AccountTypeInternal accountType,
        AccountTypeInternalOther? accountTypeOther,
        CancellationToken cancellationToken);
}