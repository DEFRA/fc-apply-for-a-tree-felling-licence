using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Services.Applicants.Repositories;

public interface IAgencyRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database  
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Adds a new <see cref="Agency"/> entity to the repository.
    /// </summary>
    /// <param name="entity">The <see cref="Agency"/> to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> with the entity, or an <see cref="UserDbErrorReason"/>.</returns>
    Task<Result<Agency, UserDbErrorReason>> AddAgencyAsync(Agency entity, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve an agency by the given identifier.
    /// </summary>
    /// <param name="id">The agency Id</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Result struct with a found agency or an error reason.</returns>
    Task<Result<Agency, UserDbErrorReason>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an agency with the given identifier.
    /// </summary>
    /// <param name="id">The identifier for the agency.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the agency has been deleted, or an error if unsuccessful.</returns>
    Task<Result> DeleteAgencyAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve an <see cref="AgentAuthority"/> entity by the given id.
    /// </summary>
    /// <param name="id">The agent authority id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Result struct with the found entity or an error reason.</returns>
    Task<Result<AgentAuthority, UserDbErrorReason>> GetAgentAuthorityAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to locate an <see cref="AgentAuthority"/> entity by the given Agency id and Woodland Owner id
    /// and returns the <see cref="AgentAuthorityStatus"/> of it if one is found.
    /// </summary>
    /// <param name="agencyId">The id of the agency to locate the <see cref="AgentAuthority"/> for.</param>
    /// <param name="woodlandOwnerId">The id of the woodland owner to locate the <see cref="AgentAuthority"/> for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="AgentAuthorityStatus"/> value of the located <see cref="AgentAuthority"/>, or <see cref="Maybe.None"/>.</returns>
    Task<Maybe<AgentAuthorityStatus>> FindAgentAuthorityStatusAsync(Guid agencyId, Guid woodlandOwnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to locate an <see cref="AgentAuthority"/> entity by the given Agency id and Woodland Owner id.
    /// </summary>
    /// <param name="agencyId">The id of the agency to locate the <see cref="AgentAuthority"/> for.</param>
    /// <param name="woodlandOwnerId">The id of the woodland owner to locate the <see cref="AgentAuthority"/> for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The located <see cref="AgentAuthority"/>, or <see cref="Maybe.None"/>.</returns>
    Task<Maybe<AgentAuthority>> FindAgentAuthorityAsync(Guid agencyId, Guid woodlandOwnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve a list of <see cref="AgentAuthority"/> entities for a particular Agent Id.
    /// </summary>
    /// <param name="agencyId">The Id of the agency to retrieve agent authority entities for.</param>
    /// <param name="filter">An optional filter to restrict the returned results to those that match an array of specified <see cref="AgentAuthorityStatus"/> values.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="AgentAuthority"/> entities for the agency, or a <see cref="UserDbErrorReason"/> if the operation failed.</returns>
    Task<Result<IEnumerable<AgentAuthority>, UserDbErrorReason>> ListAuthoritiesByAgencyAsync(Guid agencyId, AgentAuthorityStatus[]? filter, CancellationToken cancellationToken);

    /// <summary>
    /// Attempt to find an active <see cref="AgentAuthority"/> entity for a particular Woodland Owner Id.
    /// </summary>
    /// <param name="woodlandOwnerId">The id of the woodland owner to look up the authority for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="AgentAuthority"/> entities for the agency, or a <see cref="UserDbErrorReason"/> if the operation failed.</returns>
    Task<Maybe<AgentAuthority>> GetActiveAuthorityByWoodlandOwnerIdAsync(Guid woodlandOwnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new <see cref="AgentAuthority"/> entity to the repository.
    /// </summary>
    /// <param name="entity">The <see cref="AgentAuthority"/> to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The added entity.</returns>
    /// <remarks>Also adds the woodland owner attached to the entity if required.</remarks>
    Task<Result<AgentAuthority, UserDbErrorReason>> AddAgentAuthorityAsync(AgentAuthority entity, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an <see cref="AgentAuthority"/> entity from the repository.
    /// </summary>
    /// <param name="agentAuthorityId">The id of the <see cref="AgentAuthority"/> to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="UserDbErrorReason"/> if the operation failed.</returns>
    Task<UnitResult<UserDbErrorReason>> DeleteAgentAuthorityAsync(Guid agentAuthorityId, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to locate an <see cref="Agency"/> with an active <see cref="AgentAuthority"/> entity for
    /// the given woodland owner id.
    /// </summary>
    /// <param name="woodlandOwnerId">The id of the woodland owner to find the agency for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="Agency"/> if found, otherwise <see cref="Maybe.None"/>.</returns>
    Task<Maybe<Agency>> FindAgencyForWoodlandOwnerAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to locate the <see cref="Agency"/> with the IsFcAgency flag set to true.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="Agency"/> if found, otherwise <see cref="Maybe.None"/>.</returns>
    Task<Maybe<Agency>> FindFcAgency(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of all active <see cref="AgentAuthority"/> entries.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The list of <see cref="AgentAuthority"/> entities that aren't deactivated.</returns>
    Task<List<AgentAuthority>> GetActiveAgentAuthoritiesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all agencies with an external applicant account linked to them.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="Agency"/> entities with at least one linked active external account.</returns>
    Task<List<Agency>> GetAgenciesForActiveAccountsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all agencies with an id not in the given list of ids.
    /// </summary>
    /// <param name="ids">A list of ids to exclude from the result set.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all <see cref="Agency"/> entities in the repository with an id not in the given set.</returns>
    Task<List<Agency>> GetAgenciesWithIdNotIn(List<Guid> ids, CancellationToken cancellationToken);
}