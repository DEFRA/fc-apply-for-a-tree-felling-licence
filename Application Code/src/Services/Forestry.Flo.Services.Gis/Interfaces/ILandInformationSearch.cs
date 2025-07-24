using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Interfaces;

/// <summary>
/// Provides all methods required for FLOv2 operations on features/feature layers for the
/// Land Information Search (LIS) Tool. 
/// </summary>
public interface ILandInformationSearch
{
    /// <summary>
    /// Adds all compartment geometries for a felling licence to an
    /// esri Feature in the esri Feature service on the configured ArcGIS online.
    /// </summary>
    /// <param name="fellingLicenceId">Unique identifier for the felling licence (case Id)</param>
    /// <param name="compartments">A <see cref="IReadOnlyList&lt;InternalCompartmentDetails&gt;"/></param>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns>The result of the action</returns>
    Task<Result<CreateUpdateDeleteResponse<int>>> AddFellingLicenceGeometriesAsync(Guid fellingLicenceId,
        IReadOnlyList<InternalCompartmentDetails<Polygon>> compartments,
        CancellationToken cancellationToken);

    /// <summary>
    /// Clears the layer of the applications
    /// </summary>
    /// <param name="fellingLicenceId">Unique identifier for the felling licence (case Id)</param>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns>The result of the action</returns>
    Task<Result> ClearLayerAsync(
        string fellingLicenceId,
        CancellationToken cancellationToken);
}