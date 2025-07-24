using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Services.Applicants.Services
{
    /// <summary>
    /// Contract for a service that retrieves woodland owners.
    /// </summary>
    public interface IRetrieveWoodlandOwners
    {
        /// <summary>
        /// Retrieves a <see cref="WoodlandOwnerModel"/> by ID.
        /// </summary>
        /// <param name="id">The woodland owner ID.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A populated <see cref="WoodlandOwnerModel"/>.</returns>
        Task<Result<WoodlandOwnerModel>> RetrieveWoodlandOwnerByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets a list of all woodland owners in the system for the FC dashboard.
        /// </summary>
        /// <param name="performingUserId">The id of the performing user.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of <see cref="WoodlandOwnerFcModel"/> representing the woodland owners.</returns>
        Task<Result<List<WoodlandOwnerFcModel>>> GetAllWoodlandOwnersForFcAsync(
            Guid performingUserId,
            CancellationToken cancellationToken);
    }
}