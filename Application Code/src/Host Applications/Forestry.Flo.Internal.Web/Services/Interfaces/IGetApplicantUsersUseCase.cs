using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for use case to retrieve external user accounts for account administrators.
/// </summary>
public interface IGetApplicantUsersUseCase
{
    /// <summary>
    /// Populates an <see cref="ExternalUserListModel"/> with all active external user accounts.
    /// </summary>
    /// <param name="internalUser">The internal user requesting the list.</param>
    /// <param name="returnUrl">A link to redirect to if the user selects cancel.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ExternalUserListModel"/>.</returns>
    Task<Result<ExternalUserListModel>> RetrieveListOfActiveExternalUsersAsync(
        InternalUser internalUser,
        string returnUrl,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a populated <see cref="ExternalUserModel"/> for a given user account.
    /// </summary>
    /// <param name="userId">The identifier for the external user account.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing a populated <see cref="ExternalUserModel"/> if successful, or an error if not.</returns>
    Task<Result<ExternalUserModel>> RetrieveExternalUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
