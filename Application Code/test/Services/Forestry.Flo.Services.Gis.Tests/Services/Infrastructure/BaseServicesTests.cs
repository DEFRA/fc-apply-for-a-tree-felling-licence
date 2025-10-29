using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Services;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Common;
using Microsoft.Extensions.Logging;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Esri.Common;

namespace Forestry.Flo.Services.Gis.Tests.Services.Infrastructure;

/// <summary>
/// This class is access the protected methods in the base class.
/// Normally this class wouldn't exist, The aim of this class to cut down on the duplication of testing in each call
/// </summary>
public class BaseServicesTests : BaseServices
{
    public BaseServicesTests(IHttpClientFactory httpClientFactory, string clientName, ILogger<BaseServicesTests> logger,
        GetTokenParameters? getTokenParameters, string? path, List<FeatureLayerConfig>? layerSettings, GeometryServiceSettings? geometryService, int? spatialReference) : base(
        httpClientFactory, clientName, logger,
        getTokenParameters, path)
    {
        base.LayerSettings = layerSettings ?? [];
        base.GeometryService = geometryService ?? new GeometryServiceSettings();
        if(spatialReference != null)
            base.SpatialReference = (int)spatialReference;
    }


    public Maybe<FeatureLayerConfig> GetLayerDetail(string name)
    {
        return base.GetLayerDetails(name);
    }


    public async Task<Result> GetTokenAsync(CancellationToken cancellationToken)
    {
        return await base.GetTokenAsync(cancellationToken);
    }

    public async Task<Result<Maybe<string>>> GetTokenString()
    {
        return await base.GetTokenString(default);
    }

    public Maybe<string> CheckForEsriErrors(string content)
    {
        return BaseServices.CheckForEsriErrors(content);
    }

    public async Task<Result<T>> PostQueryWithConversionAsync<T>(BaseParameter query, string path, bool needsToken)
    {
        return await base.PostQueryWithConversionAsync<T>(query, path, needsToken, default);
    }

    public async Task<Result<string>> PostQueryAsync(BaseParameter query, string path, bool htmlIsValid,
        bool needsToken)
    {
        return await base.PostQueryAsync(query, path, needsToken, true, default);
    }

    public async Task<Result<Stream>> GetEsriGeneratedImageAsync(string url, int waitTime,
        CancellationToken cancellationToken)
    {
        return await base.GetEsriGeneratedImageAsync(url, waitTime, cancellationToken);
    }

    public async Task<Result<LatLongObj>> ConvertPointToLatLongAsync(Point point,
        CancellationToken cancellationToken)

    {
        return await base.ConvertPointToLatLongAsync(point, "https://base.com", cancellationToken);
    }

    public async Task<Result<Geometry<Polygon>>> GetUnionedPolygonAsync(List<string> compartments,
        CancellationToken cancellationToken)
    {
        return await base.UnionPolygonsAsync(compartments, "https://base.com", cancellationToken);
    }

    public async Task<Result<Geometry<List<Polygon>>>> GetIntersectsAsync(Polygon compartment, Polygon layerShape,
        CancellationToken cancellationToken)
    {
        return await base.GetIntersectsAsync(compartment, layerShape, "https://base.com", cancellationToken);
    }

    public async Task<Result<AreasAndLengthsParameters>> GetAreasAsync(Polygon compartment,
        CancellationToken cancellationToken)
    {
        return await base.GetAreasAsync(compartment, "https://base.com", cancellationToken);
    }

    public Maybe<EsriTokenResponse> Token {
        get { return base.Token; }
        set { base.Token = value; }
    }

}
