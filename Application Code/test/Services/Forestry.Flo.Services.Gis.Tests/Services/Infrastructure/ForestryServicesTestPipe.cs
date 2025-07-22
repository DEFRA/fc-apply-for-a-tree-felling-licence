using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Services;
using Forestry.Flo.Services.Gis.Models.Esri.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Gis.Tests.Access.Infrastructure;
public class ForestryServicesTestPipe : ForestryServices

{
    public ForestryServicesTestPipe(EsriConfig config, IHttpClientFactory httpClientFactory, ILogger<ForestryServices> logger) :
        base(config, httpClientFactory, logger)
    {
    }
    public async Task<Result<Point>> CalculateCentrePointAsync(List<string> compartments,
        CancellationToken cancellationToken)
    {
        return await base.CalculateCentrePointAsync(compartments, cancellationToken);
    }

    public async Task<Result<LatLongObj>> ConvertPointToLatLongAsync(Point point,
        CancellationToken cancellationToken)
    {
        return await base.ConvertPointToLatLongAsync(point, "https://base.com", cancellationToken);
    }

    public Result<string> ConvertLatLongToOSGrid(LatLongObj latLongObj, int gridLength, bool includeSpaces, CancellationToken cancellationToken)
    {
        return base.ConvertLatLongToOSGrid(latLongObj, gridLength, includeSpaces, cancellationToken);
    }

    public async Task<Result<LabelPointResponse>> GetLabelPoint(Geometry<Polygon> polygon,
        CancellationToken cancellationToken)
    {
        return await GetLabelPoint(polygon, cancellationToken);
    }
}
