using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminHub;
using Forestry.Flo.Services.AdminHubs.Model;

namespace Forestry.Flo.Internal.Web.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for managing admin hub operations such as retrieving details,
    /// adding/removing admin officers, and editing admin hub information.
    /// </summary>
    public interface IManageAdminHubUseCase
    {
        /// <summary>
        /// Retrieves the details of the admin hub for the specified internal user.
        /// </summary>
        /// <param name="user">The internal user requesting the details.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A result containing the <see cref="ViewAdminHubModel"/> if successful.</returns>
        Task<Result<ViewAdminHubModel>> RetrieveAdminHubDetailsAsync(
            InternalUser user,
            CancellationToken cancellationToken);

        /// <summary>
        /// Adds an admin officer to the specified admin hub.
        /// </summary>
        /// <param name="model">The admin hub model containing the officer to add.</param>
        /// <param name="user">The internal user performing the operation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A unit result indicating the outcome of the operation.</returns>
        Task<UnitResult<ManageAdminHubOutcome>> AddAdminOfficerAsync(
            ViewAdminHubModel model,
            InternalUser user,
            CancellationToken cancellationToken);

        /// <summary>
        /// Removes an admin officer from the specified admin hub.
        /// </summary>
        /// <param name="model">The admin hub model containing the officer to remove.</param>
        /// <param name="user">The internal user performing the operation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A unit result indicating the outcome of the operation.</returns>
        Task<UnitResult<ManageAdminHubOutcome>> RemoveAdminOfficerAsync(
            ViewAdminHubModel model,
            InternalUser user,
            CancellationToken cancellationToken);

        /// <summary>
        /// Edits the details of the specified admin hub.
        /// </summary>
        /// <param name="model">The admin hub model containing the new details.</param>
        /// <param name="user">The internal user performing the operation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A unit result indicating the outcome of the operation.</returns>
        Task<UnitResult<ManageAdminHubOutcome>> EditAdminHub(
            ViewAdminHubModel model,
            InternalUser user,
            CancellationToken cancellationToken);
    }
}
