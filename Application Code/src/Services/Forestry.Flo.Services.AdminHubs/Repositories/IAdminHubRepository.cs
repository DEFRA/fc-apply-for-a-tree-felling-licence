using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Entities;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Services.AdminHubs.Repositories;

public interface IAdminHubRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database  
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Gets all Admin Hubs on the system
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Collection of all Admin Hubs</returns>
    Task<Result<IReadOnlyCollection<AdminHub>>> GetAllAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a User as an Admin Officer to the specified Admin Hub.
    /// </summary>
    /// <param name="adminHubId">The Unique identifier of the Admin Hub to add the Admin Officer to</param>
    /// <param name="adminOfficerUserId">Internal User Id of the user who is be added as an admin officer at this admin hub</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result object</returns>
    Task<UnitResult<UserDbErrorReason>> AddAdminOfficerAsync(
        Guid adminHubId,
        Guid adminOfficerUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes a User as an Admin Officer to the specified Admin Hub.
    /// </summary>
    /// <param name="adminHubId">The Unique identifier of the Admin Hub to remove the Admin Officer from</param>
    /// <param name="adminOfficerUserId">Internal User Id of the user who is the admin officer to remove from this admin hub</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result object</returns>
    Task<UnitResult<UserDbErrorReason>> RemoveAdminOfficerAsync(
        Guid adminHubId, 
        Guid adminOfficerUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets new details for the specified Admin Hub.
    /// </summary>
    /// <param name="adminHubId">The Unique identifier of the Admin Hub to operate against</param>
    /// <param name="newManager">New manager for Admin Hub</param>
    /// <param name="name">New name for Admin Hub</param>
    /// <param name="address">New address for the Admin Hub</param>
    /// <param name="cancellationToken"></param>
    Task<UnitResult<UserDbErrorReason>> UpdateAdminHubDetailsAsync(
        Guid adminHubId,
        Guid newManager,
        string name,
        string address,
        CancellationToken cancellationToken);
    
}