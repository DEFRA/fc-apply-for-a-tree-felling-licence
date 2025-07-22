using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;

namespace Forestry.Flo.Services.PropertyProfiles.Services;

/// <summary>
/// Contract for a service that retrieves and performs operations against one or more <see cref="PropertyProfiles"/> for External applicant users.
/// </summary>
public interface IGetPropertyProfiles
{
    /// <summary>
    /// Retrieves a <see cref="PropertyProfile"/> by Id when the supplied <see cref="UserAccessModel"/> is satisfied.
    /// </summary>
    /// <param name="propertyProfileId">The id of the application.</param>
    /// <param name="userAccessModel">User access model to test access against</param>
    /// <param name="cancellationToken">A cancellation token.</param>

    /// <returns>A populated <see cref="PropertyProfile"/> entity.</returns>
    Task<Result<PropertyProfile>> GetPropertyByIdAsync(
        Guid propertyProfileId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve a list of Property Profiles matching the supplied Query.
    /// </summary>
    /// <param name="query">The <see cref="ListPropertyProfilesQuery"/> restriction to be executed as part of the query</param>
    /// <param name="userAccessModel">User access model to test access against</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    Task<Result<IEnumerable<PropertyProfile>>> ListAsync(
        ListPropertyProfilesQuery query,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    Task<Result<IEnumerable<PropertyProfile>>> ListByWoodlandOwnerAsync(
        Guid woodlandOwnerId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken
        );
}
