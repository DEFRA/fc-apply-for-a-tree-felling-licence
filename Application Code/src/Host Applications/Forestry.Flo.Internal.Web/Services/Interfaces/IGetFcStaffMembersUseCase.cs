using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Services.InternalUsers.Models;

namespace Forestry.Flo.Internal.Web.Services.Interfaces
{
    /// <summary>
    /// Interface for retrieving all FC staff user accounts for account administrators.
    /// </summary>
    public interface IGetFcStaffMembersUseCase
    {
        /// <summary>
        /// Retrieves a list of <see cref="UserAccountModel"/> models for every FC staff member.
        /// </summary>
        /// <param name="internalUser">The internal user requesting the list.</param>
        /// <param name="returnUrl">The return url to redirect to if the user presses cancel.</param>
        /// <param name="includeLoggedInUser">Whether the logged in user will appear in this list.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A populated list of <see cref="UserAccountModel"/> models.</returns>
        Task<Result<FcStaffListModel>> GetAllFcStaffMembersAsync(
            InternalUser internalUser,
            string returnUrl,
            bool includeLoggedInUser,
            CancellationToken cancellationToken);
    }
}
