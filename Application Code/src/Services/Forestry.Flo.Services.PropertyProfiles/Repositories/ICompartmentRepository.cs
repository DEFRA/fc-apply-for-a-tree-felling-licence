using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;

namespace Forestry.Flo.Services.PropertyProfiles.Repositories;

public interface ICompartmentRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database  
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Adds a given compartment to the compartments collection
    /// </summary>
    /// <param name="compartment">Compartment object to add</param>
    /// <returns>Added property profile</returns>
    Compartment Add(Compartment compartment);

    /// <summary>
    /// Removes a given compartment to the compartments collection
    /// </summary>
    /// <param name="compartment">Compartment object to add</param>
    /// <returns>Added property profile</returns>
    Compartment Remove(Compartment compartment);

    /// <summary>
    /// Updates a  given compartment record
    /// </summary>
    /// <param name="compartment">Compartment object to update</param>
    /// <returns>Result object including the compartment and the status of th operation</returns>
    Task<Result<Compartment, UserDbErrorReason>> UpdateAsync(Compartment compartment);

    /// <summary>
    /// Retrieves a compartment by the given id 
    /// </summary>
    /// <param name="id">The compartment Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result object including the compartment and the status of the operation</returns>
    Task<Result<Compartment, UserDbErrorReason>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a compartment by the given parameters
    /// </summary>
    /// <param name="id">The compartment Id</param>
    /// <param name="woodlandOwnerId"></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result object including the compartment and the status of the operation</returns>
    Task<Result<Compartment, UserDbErrorReason>> GetAsync(Guid id, Guid woodlandOwnerId, CancellationToken cancellationToken);

    Task<IEnumerable<Compartment>> ListAsync(Guid propertyProfileId, Guid woodlandOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of Compartments that match any in a list of given compartment ids.
    /// </summary>
    /// <param name="ids">A list of ids to return the compartments matching.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="Compartment"/> entities matching the input parameters.</returns>
    Task<IEnumerable<Compartment>> ListAsync(IList<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a given user is allowed to access the property with the given Id.
    /// </summary>
    /// <param name="compartmentId">The id of the property to check.</param>
    /// <param name="userAccessModel">A representation of the users access to data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the user can access the compartment, otherwise false.</returns>
    Task<Result<bool>> CheckUserCanAccessCompartmentAsync(
        Guid compartmentId, 
        UserAccessModel userAccessModel, 
        CancellationToken cancellationToken);
}