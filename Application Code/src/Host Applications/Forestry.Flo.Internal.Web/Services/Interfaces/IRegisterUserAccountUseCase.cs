using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Internal.Web.Services.Interfaces
{
    /// <summary>
    /// Contract for user account registration and management use case operations.
    /// </summary>
    public interface IRegisterUserAccountUseCase
    {
        /// <summary>
        /// Retrieves a populated <see cref="UserRegistrationDetailsModel"/> from an internal user's identity provider ID.
        /// </summary>
        /// <param name="user">An internal user.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A populated <see cref="UserRegistrationDetailsModel"/> model, or None if not found.</returns>
        Task<Maybe<UserRegistrationDetailsModel>> GetUserAccountModelAsync(InternalUser user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a <see cref="UpdateUserRegistrationDetailsModel"/> model from a user account ID.
        /// </summary>
        /// <param name="id">A user account ID.</param>
        /// <param name="actingUser">The user performing the operation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A populated <see cref="UpdateUserRegistrationDetailsModel"/> or failure result.</returns>
        Task<Result<UpdateUserRegistrationDetailsModel>> GetUserAccountModelByIdAsync(Guid id, InternalUser actingUser, CancellationToken cancellationToken);

        /// <summary>
        /// Updates registration details for a user account by ID.
        /// </summary>
        /// <param name="performingUser">The user performing the update.</param>
        /// <param name="model">The updated registration details model.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>Success or failure of the operation.</returns>
        Task<Result> UpdateAccountRegistrationDetailsByIdAsync(InternalUser performingUser, UpdateUserRegistrationDetailsModel model, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to update the name details in the local account for the given user with values from a UI model.
        /// </summary>
        /// <param name="user">An <see cref="InternalUser"/> representing the current user.</param>
        /// <param name="model">The model containing updated values.</param>
        /// <param name="confirmAccountBaseUrl">The base URL for the confirm account page, to go in the notification.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>Success or failure of the operation.</returns>
        Task<Result> UpdateAccountRegistrationDetailsAsync(InternalUser user, UserRegistrationDetailsModel model, string confirmAccountBaseUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the <see cref="Status"/> of an internal <see cref="UserAccount"/>.
        /// </summary>
        /// <param name="performingUser">The user performing this action.</param>
        /// <param name="userId">The identifier for the <see cref="UserAccount"/>.</param>
        /// <param name="requestedStatus">The <see cref="Status"/> to update the account with.</param>
        /// <param name="setCanApproveApplications">A flag indicating if the user should be allowed to approve applications.</param>
        /// <param name="loginUrl">A URL to allow the recipient of a notification to log in.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A result indicating whether the <see cref="Status"/> has been successfully updated for the <see cref="UserAccount"/>.</returns>
        Task<Result> UpdateUserAccountStatusAsync(
            InternalUser performingUser,
            Guid userId,
            Status requestedStatus,
            bool? setCanApproveApplications,
            string loginUrl,
            CancellationToken cancellationToken);
    }
}
