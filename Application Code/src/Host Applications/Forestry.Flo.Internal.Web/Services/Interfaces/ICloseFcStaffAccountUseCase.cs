using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;

namespace Forestry.Flo.Internal.Web.Services.Interfaces
{
    /// <summary>
    /// Interface for use case to close FC staff user accounts.
    /// </summary>
    public interface ICloseFcStaffAccountUseCase
    {
        /// <summary>
        /// Retrieves user account details for closing.
        /// </summary>
        /// <param name="id">The user account id.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A result containing the user account details.</returns>
        Task<Result<CloseUserAccountModel>> RetrieveUserAccountDetailsAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the FC staff user account.
        /// </summary>
        /// <param name="userAccountId">The id of the user to close.</param>
        /// <param name="internalUser">The internal user performing the action.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A result representing whether the account has been closed.</returns>
        Task<Result> CloseFcStaffAccountAsync(Guid userAccountId, InternalUser internalUser, CancellationToken cancellationToken);
    }
}
