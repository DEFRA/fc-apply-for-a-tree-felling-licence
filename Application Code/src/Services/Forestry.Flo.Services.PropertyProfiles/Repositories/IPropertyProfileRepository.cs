using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;

namespace Forestry.Flo.Services.PropertyProfiles.Repositories;

public interface IPropertyProfileRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database  
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Adds given Property profile to profiles collection
    /// </summary>
    /// <param name="propertyProfile">Property profile object to add</param>
    /// <returns>Added property profile</returns>
    PropertyProfile Add(PropertyProfile propertyProfile);

    /// <summary>
    /// Updates given Property profile record
    /// </summary>
    /// <param name="propertyProfile">Property profile object to update</param>
    Task<Result<PropertyProfile, UserDbErrorReason>> UpdateAsync(PropertyProfile propertyProfile);

    /// <summary>
    /// Retrieves a property profile by the given id 
    /// </summary>
    /// <param name="id">The property profile Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result object including the property profile and the status of th operation</returns>
    Task<Result<PropertyProfile, UserDbErrorReason>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a property profile by the given parameters
    /// </summary>
    /// <param name="id">The property profile Id</param>
    /// <param name="woodlandOwnerId">The woodland owner Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A Result object including a requested property profile or an error reason</returns>
    Task<Result<PropertyProfile, UserDbErrorReason>> GetAsync(Guid id, Guid woodlandOwnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a property profile by the given parameters
    /// </summary>
    /// <param name="woodlandOwnerId">The woodland owner Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A Result object including a requested property profile or an error reason</returns>
    Task<Result<IEnumerable<PropertyProfile>, UserDbErrorReason>> ListAsync(Guid woodlandOwnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of Property profile items selected by a given query parameters
    /// </summary>
    /// <param name="query">A query object including the Woodland Owner Id and a list of property profile Ids to select property profiles</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A collection of property profile items</returns>
    Task<IEnumerable<PropertyProfile>> ListAsync(ListPropertyProfilesQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of Property profile items based purely on the IDs passed in the ids parameter
    /// </summary>
    /// <param name="ids">The ids.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<IEnumerable<PropertyProfile>> ListAsync(IList<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a given user is allowed to access the property with the given Id.
    /// </summary>
    /// <param name="propertyProfileId">The id of the property to check.</param>
    /// <param name="userAccess">A representation of the users access to data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the user can access the property, otherwise false.</returns>
    Task<Result<bool>> CheckUserCanAccessPropertyProfileAsync(
        Guid propertyProfileId, 
        UserAccessModel userAccess, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a given user is allowed to access the property with the supplied Ids.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="userAccess">A representation of the users access to data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the user can access the properties, otherwise false.</returns>
    Task<Result<bool>> CheckUserCanAccessPropertyProfilesAsync(
        ListPropertyProfilesQuery query,
        UserAccessModel userAccess,
        CancellationToken cancellationToken);
}