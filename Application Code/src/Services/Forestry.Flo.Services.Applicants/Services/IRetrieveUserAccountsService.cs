using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Contract for a service that implements tasks to retrieve user accounts for different scenarios.
/// </summary>
public interface IRetrieveUserAccountsService
{
    /// <summary>
    /// Retrieves a list of <see cref="UserAccountModel"/> models linked to a specific woodland owner.
    /// </summary>
    /// <param name="woodlandOwnerId">The id of the woodland owner to retrieve accounts for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of populated <see cref="UserAccountModel"/> instances.</returns>
    /// <remarks>This will return either a list of user accounts that are linked directly to the
    /// woodland owner, or a list of agent user accounts at an agency with an approved AAF for
    /// the woodland owner.</remarks>
    Task<Result<List<UserAccountModel>>> RetrieveUserAccountsForWoodlandOwnerAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a <see cref="UserAccountModel"/> model linked to the specified email address.
    /// </summary>
    /// <param name="emailAddress">The email address to retrieve the account for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns a <see cref="Maybe"/> result containing a <see cref="UserAccountModel"/> model
    /// if a match with the specified email address is found.</returns>
    Task<Maybe<UserAccountModel>> RetrieveUserAccountByEmailAddressAsync(
        string emailAddress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of <see cref="UserAccountModel"/> models linked to the <see cref="Agency"/> that is
    /// identified as FC.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of populated <see cref="UserAccountModel"/> instances.</returns>
    Task<Result<List<UserAccountModel>>> RetrieveUserAccountsForFcAgencyAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a <see cref="UserAccountModel"/> model from a given ID.
    /// </summary>
    /// <param name="id">The id of the user account.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="UserAccountModel"/> instance.</returns>
    Task<Result<UserAccountModel>> RetrieveUserAccountByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the user account with the given id is linked to the FC agency.
    /// </summary>
    /// <param name="userAccountId">The id of the user account to check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the identified user account is linked to the FC Agency, otherwise <see langword="false"/>.</returns>
    Task<Result<bool>> IsUserAccountLinkedToFcAgencyAsync(Guid userAccountId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all active external user accounts with a specified <see cref="AccountTypeExternal"/>.
    /// </summary>
    /// <param name="accountTypes">A list of <see cref="AccountTypeExternal"/> to filter user accounts by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="UserAccount"/> entities representing active external users.</returns>
    Task<IList<UserAccount>> RetrieveActiveExternalUsersByAccountTypeAsync(IList<AccountTypeExternal> accountTypes, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a <see cref="UserAccount"/> entity from a given ID.
    /// </summary>
    /// <param name="id">The id of the user account.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="UserAccount"/> entity.</returns>
    Task<Result<UserAccount>> RetrieveUserAccountEntityByIdAsync(Guid id, CancellationToken cancellationToken);


    /// <summary>
    /// Retrieves a list of <see cref="UserAccount"/> entities associated with a given agency ID.
    /// </summary>
    /// <param name="agencyId">The id of the agency.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<Result<List<UserAccount>>> RetrieveUsersLinkedToAgencyAsync(Guid agencyId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a representation of the access to woodland owners available to the user.
    /// </summary>
    /// <param name="userId">The id of the user to look up access for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="UserAccessModel"/> for the user.</returns>
    Task<Result<UserAccessModel>> RetrieveUserAccessAsync(Guid userId, CancellationToken cancellationToken);
}