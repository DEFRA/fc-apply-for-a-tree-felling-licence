using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service implementation than can retrieve and return
/// the list of configured FC Areas.
/// </summary>
public interface IGetConfiguredFcAreas
{
    /// <summary>
    /// Method to retrieve and return the list of all configured FC Areas.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result object containing the list of configured areas on success,
    /// otherwise a failure accompanied with the error reason</returns>
    public Task<Result<List<ConfiguredFcArea>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Method to retrieve the address of the admin hub associated with the given admin hub name.
    /// </summary>
    /// <param name="adminHubName">The name of the admin hub to retrieve the address for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The name of the admin hub if found, otherwise returns the given admin hub name.</returns>
    public Task<string> TryGetAdminHubAddress(string adminHubName, CancellationToken cancellationToken);
}
