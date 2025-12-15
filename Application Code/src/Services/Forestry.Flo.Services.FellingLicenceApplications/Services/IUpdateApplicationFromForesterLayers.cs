using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for a service that updates a felling licence application based on querying
/// geographic layers within Forester.
/// </summary>
public interface IUpdateApplicationFromForesterLayers
{
    /// <summary>
    /// Updates the application with information from Forester layers for PAWS data.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="selectedCompartments">The GIS data for the selected compartments on the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the update was successful.</returns>
    public Task<Result> UpdateForPawsLayersAsync(
        Guid applicationId,
        Dictionary<Guid, string> selectedCompartments,
        CancellationToken cancellationToken);
}