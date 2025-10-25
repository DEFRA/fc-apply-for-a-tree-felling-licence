using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for use cases related to amending external user accounts,
/// including retrieval, update, closure, and verification operations.
/// </summary>
public interface IAmendExternalUserUseCase
{
    /// <summary>
    /// Retrieves the external user account details for amendment.
    /// </summary>
    /// <param name="id">The unique identifier of the external user account.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the <see cref="AmendExternalUserAccountModel"/> if successful.</returns>
    Task<Result<AmendExternalUserAccountModel>> RetrieveExternalUserAccountAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the details of an external user account.
    /// </summary>
    /// <param name="loggedInUser">The internal user performing the update.</param>
    /// <param name="model">The model containing updated account details.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result indicating the success or failure of the update operation.</returns>
    Task<Result> UpdateExternalAccountDetailsAsync(InternalUser loggedInUser, AmendExternalUserAccountModel model, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the model required to close an external user account.
    /// </summary>
    /// <param name="id">The unique identifier of the external user account.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the <see cref="CloseExternalUserModel"/> if successful.</returns>
    Task<Result<CloseExternalUserModel>> RetrieveCloseExternalUserModelAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Closes an external user account.
    /// </summary>
    /// <param name="userAccountId">The unique identifier of the user account to close.</param>
    /// <param name="internalUser">The internal user performing the closure.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result indicating the success or failure of the closure operation.</returns>
    Task<Result> CloseExternalUserAccountAsync(Guid userAccountId, InternalUser internalUser, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies whether an agent can be closed based on user and agency identifiers.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="agencyId">The unique identifier of the agency.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result indicating whether the agent can be closed.</returns>
    Task<Result> VerifyAgentCanBeClosedAsync(Guid userId, Guid agencyId, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies whether a woodland owner can be closed based on user and woodland owner identifiers.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="woodlandOwnerId">The unique identifier of the woodland owner.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A boolean indicating whether the woodland owner can be closed.</returns>
    Task<bool> VerifyWoodlandOwnerCanBeClosedAsync(Guid userId, Guid woodlandOwnerId, CancellationToken cancellationToken);
}
    