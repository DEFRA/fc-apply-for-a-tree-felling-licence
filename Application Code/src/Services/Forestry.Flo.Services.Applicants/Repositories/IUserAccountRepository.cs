using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Applicants.Repositories;

public interface IUserAccountRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database  
    /// </summary>
    IUnitOfWork UnitOfWork { get; }
    
    /// <summary>
    /// Adds given User account to the database table
    /// </summary>
    /// <param name="userAccount">User account to add</param>
    /// <returns>The added user account</returns>
    UserAccount Add(UserAccount userAccount);
    
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
    /// Retrieve a user account by the given Azure AD user identifier 
    /// </summary>
    /// <param name="userIdentifier">Azure AD user identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="email">An optional email address to match users during transition from ADB2C to One Login.</param>
    /// <returns>Result object with the requested user account or an error reason</returns>
    Task<Result<UserAccount, UserDbErrorReason>> GetByUserIdentifierAsync(string userIdentifier, CancellationToken cancellationToken, string? email = null);

    /// <summary>
    /// Retrieve users where ID is in userIds list
    /// </summary>
    /// <param name="userIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithIdsInAsync(IList<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve user accounts linked to the woodland owner with the given id
    /// </summary>
    /// <param name="woodlandOwnerId">The id of the woodland owner to retrieve accounts for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="UserAccount"/> entities linked to the woodland owner.</returns>
    Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithWoodlandOwnerIdAsync(Guid woodlandOwnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve user accounts linked to the agency with the given id
    /// </summary>
    /// <param name="agencyId">The id of the agency to retrieve accounts for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="UserAccount"/> entities linked to the agency.</returns>
    Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithAgencyIdAsync(Guid agencyId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all agent user accounts that are active.
    /// </summary>
    /// <param name="accountTypes">A list of <see cref="AccountTypeExternal"/> to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of active agent <see cref="UserAccount"/> entities.</returns>
    Task<IList<UserAccount>> GetExternalUsersByAccountTypeAsync(
        IList<AccountTypeExternal> accountTypes,
        CancellationToken cancellationToken);
}