using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Services.Applicants.Repositories;

public interface IWoodlandOwnerRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database  
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Retrieve a woodland owner by the given id 
    /// </summary>
    /// <param name="id">The woodland owner Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result object with a found woodland owner or an error reason</returns>
    Task<Result<WoodlandOwner, UserDbErrorReason>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve all Woodland Owners
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A <see cref="Result{T}"/> with enumerable list of entities, or an <see cref="UserDbErrorReason"/>.</returns>
    Task<Result<IEnumerable<WoodlandOwner>, UserDbErrorReason>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Removes given Woodland Owner from the database table
    /// </summary>
    /// <param name="woodlandOwner">Woodland Owner to remove</param>
    void Remove(WoodlandOwner woodlandOwner);

    /// <summary>
    /// Gets the applicant users for a woodland owner ID, either directly linked or by agency
    /// </summary>
    /// <param name="woodlandOwnerId">The woodland owner identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<IEnumerable<UserAccount>> GetActiveApplicantUsers(Guid woodlandOwnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new <see cref="WoodlandOwner"/> entity to the repository.
    /// </summary>
    /// <param name="entity">The <see cref="WoodlandOwner"/> to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> with the entity, or an <see cref="UserDbErrorReason"/>.</returns>
    Task<Result<WoodlandOwner, UserDbErrorReason>> AddWoodlandOwnerAsync(WoodlandOwner entity, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all woodland owners with an external applicant account linked to them.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="WoodlandOwner"/> entities with at least one linked active external account.</returns>
    /// <remarks>This will only return for woodland owner type applicant accounts, not any linked to agencies.</remarks>
    Task<IList<WoodlandOwner>> GetWoodlandOwnersForActiveAccountsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all woodland owners with an id not in the given list of ids.
    /// </summary>
    /// <param name="ids">A list of ids to exclude from the result set.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all <see cref="WoodlandOwner"/> entities in the repository with an id not in the given set.</returns>
    Task<IList<WoodlandOwner>> GetWoodlandOwnersWithIdNotIn(List<Guid> ids, CancellationToken cancellationToken);
}