using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Services.Applicants.Services
{
    /// <summary>
    /// Contract for a service that orchestrates the creation of a <see cref="WoodlandOwner"/> entity.
    /// </summary>
    public interface IWoodlandOwnerCreationService
    {
        /// <summary>
        /// Adds a new <see cref="WoodlandOwner"/> entity to the system.
        /// </summary>
        /// <param name="request">A populated <see cref="AddWoodlandOwnerDetailsRequest"/> model containing details of the woodland owner to be added and the user who is requesting its addition.</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns></returns>
        Task<Result<AddWoodlandOwnerDetailsResponse>> AddWoodlandOwnerDetails(
            AddWoodlandOwnerDetailsRequest request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates a <see cref="WoodlandOwner"/> entity using a populated <see cref="WoodlandOwnerModel"/>.
        /// </summary>
        /// <param name="model">A populated <see cref="WoodlandOwnerModel"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A result indicating whether the entity has been updated.</returns>
        /// <remarks>True will only be returned if the entity has been changed and the update is successful.</remarks>
        Task<Result<bool>> AmendWoodlandOwnerDetailsAsync(
            WoodlandOwnerModel model,
            CancellationToken cancellationToken);
    }
}