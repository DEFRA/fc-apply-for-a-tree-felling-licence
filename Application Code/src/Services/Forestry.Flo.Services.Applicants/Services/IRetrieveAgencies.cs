using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Contract for a service that retrieves agencies.
/// </summary>
public interface IRetrieveAgencies
{
    /// <summary>
    /// Gets a list of all agencies in the system for the FC dashboard.
    /// </summary>
    /// <param name="performingUserId">The id of the performing user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="AgencyFcModel"/> representing the Agencies.</returns>
    Task<Result<List<AgencyFcModel>>> GetAllAgenciesForFcAsync(
        Guid performingUserId,
        CancellationToken cancellationToken);
}