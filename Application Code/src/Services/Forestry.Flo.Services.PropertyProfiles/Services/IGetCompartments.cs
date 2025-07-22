using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;

namespace Forestry.Flo.Services.PropertyProfiles.Services;

/// <summary>
/// Contract for a service that retrieves and performs operations against one or more <see cref="Compartment"/> for External applicant users.
/// </summary>
public interface IGetCompartments
{
    /// <summary>
    /// Retrieves a <see cref="Compartment"/> by Id when the supplied <see cref="UserAccessModel"/> is satisfied.
    /// </summary>
    /// <param name="compartmentId">The id of the application.</param>
    /// <param name="userAccessModel">User access model to test access against</param>
    /// <param name="cancellationToken">A cancellation token.</param>

    /// <returns>A populated <see cref="Compartment"/> entity.</returns>
    Task<Result<Compartment>> GetCompartmentByIdAsync(
        Guid compartmentId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);
}