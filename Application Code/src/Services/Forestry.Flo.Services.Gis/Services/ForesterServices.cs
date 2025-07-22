
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Microsoft.Extensions.Logging;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;

namespace Forestry.Flo.Services.Gis.Services;

public class ForesterServices : BaseServices, IForesterServices
{
    private readonly ForesterConfig _config;
    private readonly ILogger<IForesterServices> _logger;
    private readonly string _layoutTemplate;

    /// <summary>
    /// The Constructor for the class.
    /// Here we also prep the token set up as its likely that if you are using this class then you'll want to log into the AGOL Platform
    /// </summary>
    /// <param name="config">The ESRI config Options</param>
    /// <param name="httpClientFactory">The Factory for the http requests</param>
    /// <param name="logger">The Logger object</param>
    public ForesterServices(
        EsriConfig config,
        IHttpClientFactory httpClientFactory,
        ILogger<IForesterServices> logger)
        : base(httpClientFactory, "LandRegister", logger)
    {
        Guard.Against.Null(config.Forester, "Forester Settings not configured");
        _config = config.Forester;
        SpatialReference = config.SpatialReference;
        _layoutTemplate = config.LayoutTemplate;

        _logger = logger;
        LayerSettings = config.Forester.LayerServices ?? [];
        GeometryService = config.Forester.GeometryService;

        TokenRequest = new GetTokenParameters(_config.GenerateTokenService.Username, _config.GenerateTokenService.Password);
        GetTokenPath = $"{_config.BaseUrl}{_config.GenerateTokenService.Path}";
    }

    ///<inheritdoc />
    public async Task<Result<AdminBoundary>> GetAdminBoundaryIdAsync(BaseShape shape, CancellationToken cancellationToken)
    {
        Guard.Against.Null(_config.LayerServices);

        var layer = GetLayerDetails("LocalAuthority_Areas");
        if (layer.HasNoValue) {
            return Result.Failure<AdminBoundary>("Unable to find layer details");
        }

        var query = new QueryFeatureServiceParameters() {
            QueryGeometry = shape,
            OutFields = layer.Value.Fields
        };
        var path = $"{layer.Value.ServiceURI}/query";
        var result = await PostQueryWithConversionAsync<BaseQueryResponse<AdminBoundary>>(query, path, true, cancellationToken);

        if (result.IsFailure) {
            return Result.Failure<AdminBoundary>(result.Error);
        }

        return !result.Value.Results.Any() ? Result.Failure<AdminBoundary>("No Results found")
            : Result.Success(result.Value.Results[0].Record);
    }

    ///<inheritdoc />
    public async Task<Result<bool>> IsInEnglandAsync(BaseShape shape, CancellationToken cancellationToken)
    {
        Guard.Against.Null(_config.LayerServices);

        var layer = GetLayerDetails("Country_Boundaries_Generalised");
        if (layer.HasNoValue) {
            return Result.Failure<bool>("Unable to find layer details");
        }

        var query = new QueryFeatureServiceParameters() {
            QueryGeometry = shape,
            OutFields = layer.Value.Fields,
            SpatialRelationship = "intersects"
        };
        var path = $"{layer.Value.ServiceURI}/query";
        var result = await PostQueryWithConversionAsync<BaseQueryResponse<CountryBoundary>>(query, path, true, cancellationToken);

        if (result.IsFailure)
        {
            Result.Failure<bool>(result.Error);
        }

        return Result.Success(
            result.IsSuccess &&
            result.Value.Results.Count == 1 &&
            result.Value.Results.Any(x => x.Record.Code == _config.CountryCode)
        );
    }

    ///<inheritdoc />
    public async Task<Result<WoodlandOfficer>> GetWoodlandOfficerAsync(List<string> compartments, CancellationToken cancellationToken)
    {
        var pointResult = await base.CalculateCentrePointAsync(compartments, _config.BaseUrl, cancellationToken);
        if (pointResult.IsFailure) {
            return Result.Failure<WoodlandOfficer>(pointResult.Error);
        }
        return await GetWoodlandOfficerAsync(pointResult.Value, cancellationToken);
    }


    ///<inheritdoc />
    public async Task<Result<WoodlandOfficer>> GetWoodlandOfficerAsync(Point centralCasePoint, CancellationToken cancellationToken)
    {
        Guard.Against.Null(_config.LayerServices);

        var layer = GetLayerDetails("Woodland_Officers");
        if (layer.HasNoValue) {
            return Result.Failure<WoodlandOfficer>("Unable to find layer details");
        }

        var query = new QueryFeatureServiceParameters() {
            QueryGeometry = centralCasePoint,
            OutFields = layer.Value.Fields
        };
        var path = $"{layer.Value.ServiceURI}/query";
        var result = await PostQueryWithConversionAsync<BaseQueryResponse<WoodlandOfficer>>(query, path, true, cancellationToken);

        if (result.IsFailure) {
            return Result.Failure<WoodlandOfficer>(result.Error);
        }

        return !result.Value.Results.Any() ? Result.Failure<WoodlandOfficer>("No Results found")
            : Result.Success(result.Value.Results[0].Record);
    }

    ///<inheritdoc />
    public async Task<Result<List<PhytophthoraRamorumRiskZone>>> GetPhytophthoraRamorumRiskZonesAsync(BaseShape shape, CancellationToken cancellationToken)
    {
        Guard.Against.Null(_config.LayerServices);

        var layer = GetLayerDetails("Phytophthora_Ramorum_Risk_Zones");
        if (layer.HasNoValue) {
            return Result.Failure<List<PhytophthoraRamorumRiskZone>>("Unable to find layer details");
        }

        var query = new QueryFeatureServiceParameters() {
            QueryGeometry = shape,
            OutFields = layer.Value.Fields
        };
        var path = $"{layer.Value.ServiceURI}/query";
        var result = await PostQueryWithConversionAsync<BaseQueryResponse<PhytophthoraRamorumRiskZone>>(query, path, true, cancellationToken);

        return result.IsFailure ? Result.Failure<List<PhytophthoraRamorumRiskZone>>(result.Error) : Result.Success(result.Value.Results.Select((x) => x.Record).ToList());
    }

    ///<inheritdoc />
    public async Task<Result<LocalAuthority>> GetLocalAuthorityAsync(Point centralCasePoint, CancellationToken cancellationToken)
    {
        Guard.Against.Null(_config.LayerServices);

        var layer = GetLayerDetails("LocalAuthority_Areas");
        if (layer.HasNoValue) {
            return Result.Failure<LocalAuthority>("Unable to find layer details");
        }

        var query = new QueryFeatureServiceParameters() {
            QueryGeometry = centralCasePoint,
            OutFields = layer.Value.Fields
        };
        var path = $"{layer.Value.ServiceURI}/query";
        var result = await PostQueryWithConversionAsync<BaseQueryResponse<LocalAuthority>>(query, path, layer.Value.NeedsToken, cancellationToken);
        if (result.IsFailure) {
            return Result.Failure<LocalAuthority>(result.Error);
        }

        return !result.Value.Results.Any() ? Result.Failure<LocalAuthority>("No Results found")
            : Result.Success(result.Value.Results[0].Record);
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> GenerateImage_SingleCompartmentAsync(InternalCompartmentDetails<BaseShape> compartment, CancellationToken cancellationToken, int delay, MapGeneration generationType, string title)
    {
        Guard.Against.Null(_config.UtilitiesService);

        var extend = compartment.ShapeGeometry.GetExtent();

        if (extend.HasNoValue) {
        
            return Result.Failure<Stream>("Unable to calculate Extend");
        }

        var bufferX = CalculateBuffer(extend.Value.X_min, extend.Value.X_max);
        var bufferY = CalculateBuffer(extend.Value.Y_min, extend.Value.Y_max);

        WebMap_LayoutOptions templateLayoutOptions = new() {
            Copyright_Text = _config.UtilitiesService.ExportService.TextOverrides.Copyright
        };

        if (generationType != MapGeneration.Other) {
            templateLayoutOptions.Title_Text = generationType == MapGeneration.Restocking ?
                _config.UtilitiesService.ExportService.TextOverrides.RestockingTitle.Replace("{0}", title)
                : _config.UtilitiesService.ExportService.TextOverrides.FellingTitle.Replace("{0}", title);
        }
        else {
            if (!string.IsNullOrEmpty(title)) {
                templateLayoutOptions.Title_Text = title;
            }
        }


        WebMap_Json map = new();
        map.ExportOptions.Size = new List<int> { 1100, 800 };
        map.BaseMap[0].Layers.Add(new BaseMapLayer(_config.UtilitiesService.ExportService.BaseMap));
        map.MapOptions.MapExtent.X_min = extend.Value.X_min - bufferX;
        map.MapOptions.MapExtent.Y_min = extend.Value.Y_min - bufferY;
        map.MapOptions.MapExtent.X_max = extend.Value.X_max + bufferX;
        map.MapOptions.MapExtent.Y_max = extend.Value.Y_max + bufferY;
        map.LayoutOptions = templateLayoutOptions;

        map.OperationalLayers.Add(new WebMap_OperationalLayerMap {
            ID = _config.UtilitiesService.ExportService.BaseMapID,
            Title = "BaseMap",
            URL = _config.UtilitiesService.ExportService.BaseMap,
            Token = _config.ApiKey,
            Opacity = 1,
            MaxScale = 250,
            MinScale = 500000000,
            LayerType = "ArcGISTiledMapServiceLayer"
        });

        var shapeLayer = GetLayerDefinition(compartment.ShapeGeometry.GeometryType);
        var textLayer = GetLayerDefinition("text");
        map.OperationalLayers.Add(new WebMapOperationalLayerFeatures(shapeLayer, new FeatureSetDetails(compartment.ShapeGeometry.GeometryType, new() { new FeatureDetails(compartment.ShapeGeometry) })));
        map.OperationalLayers.Add(new WebMapOperationalLayerFeatures(textLayer, new FeatureSetDetails("esriGeometryPoint", new() { new FeatureDetails(compartment.ShapeGeometry, compartment.CompartmentLabel) })));
        ExportParameter parameters = new() {
            LayoutTemplate = _layoutTemplate,
            MapJson = map,
            FileFormat = _config.UtilitiesService.ExportService.DefaultFormat
        };

        var path = _config.UtilitiesService.IsPublic
              ? $"{_config.UtilitiesService.Path}{_config.UtilitiesService.ExportService.Path}"
              : $"{_config.BaseUrl}{_config.UtilitiesService.Path}/{_config.UtilitiesService.ExportService.Path}";


        var resx = await PostQueryWithConversionAsync<ExportResponse>(parameters, path, _config.UtilitiesService.NeedsToken, cancellationToken);

        if (resx.IsFailure) {
            return resx.ConvertFailure<Stream>();
        }

        var jobResx = await CheckJobAsync(resx.Value.Id, delay, cancellationToken);
        if (jobResx.IsFailure) {
            return resx.ConvertFailure<Stream>();
        }

        var outputResx = await GetOutputDetailsAsync(jobResx.Value, resx.Value.Id, cancellationToken);
        if (outputResx.IsFailure) {
            return resx.ConvertFailure<Stream>();
        }


        return await base.GetEsriGeneratedImageAsync(outputResx.Value, delay, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> GenerateImage_MultipleCompartmentsAsync(List<InternalCompartmentDetails<BaseShape>> compartments, CancellationToken cancellationToken, int delay, MapGeneration generationType, string title)
    {
        Guard.Against.Null(_config.UtilitiesService);

        WebMap_LayoutOptions templateLayoutOptions = new() {
            Copyright_Text = _config.UtilitiesService.ExportService.TextOverrides.Copyright
        };

        if (generationType != MapGeneration.Other) {
            templateLayoutOptions.Title_Text = generationType == MapGeneration.Restocking ?
                _config.UtilitiesService.ExportService.TextOverrides.RestockingTitle.Replace("{0}", title)
                : _config.UtilitiesService.ExportService.TextOverrides.FellingTitle.Replace("{0}", title);
        }
        else {
            if (!string.IsNullOrEmpty(title)) {
                templateLayoutOptions.Title_Text = title;
            }
        }



        if (!compartments.Any()) {
            return Result.Failure<Stream>("No shapes passed in");
        }

        var extents = compartments.Select(c => c.ShapeGeometry.GetExtent())
            .Where(shape => shape.HasValue).Select(shape => shape.Value).ToList();

        var xMin = extents.Min(r => r.X_min);
        var yMin = extents.Min(r => r.Y_min);
        var xMax = extents.Max(r => r.X_max);
        var yMax = extents.Max(r => r.Y_max);

        var bufferX = CalculateBuffer(xMin, xMax);
        var bufferY = CalculateBuffer(yMin, yMax);

        WebMap_Json map = new();
        map.ExportOptions.Size = new List<int> { 1100, 800 };
        map.BaseMap[0].Layers.Add(new BaseMapLayer(_config.UtilitiesService.ExportService.BaseMap));
        map.MapOptions.MapExtent.X_min = xMin - bufferX;
        map.MapOptions.MapExtent.Y_min = yMin - bufferY;
        map.MapOptions.MapExtent.X_max = xMax + bufferX;
        map.MapOptions.MapExtent.Y_max = yMax + bufferY;
        map.LayoutOptions = templateLayoutOptions;

        map.OperationalLayers.Add(new WebMap_OperationalLayerMap {
            ID = _config.UtilitiesService.ExportService.BaseMapID,
            Title = "BaseMap",
            URL = _config.UtilitiesService.ExportService.BaseMap,
            Token = _config.ApiKey,
            Opacity = 1,
            MaxScale = 250,
            MinScale = 500000000,
            LayerType = "ArcGISTiledMapServiceLayer"
        });


        foreach (var item in compartments.GroupBy(c => c.ShapeGeometry.GeometryType).ToList()) {
            var builtLayer = GetLayerDefinition(item.Key);

            map.OperationalLayers.Add(new WebMapOperationalLayerFeatures(builtLayer,
                new FeatureSetDetails(item.Key,
                    item.Select(s => new FeatureDetails(s.ShapeGeometry)).ToList())));

        }
        var textLayer = GetLayerDefinition("text");
        map.OperationalLayers.Add(new WebMapOperationalLayerFeatures(textLayer, new FeatureSetDetails("esriGeometryPoint", compartments.Select(c => new FeatureDetails(c.ShapeGeometry, c.CompartmentLabel)).ToList())));


        ExportParameter parameters = new() {
            LayoutTemplate = _layoutTemplate,
            MapJson = map,
            FileFormat = _config.UtilitiesService.ExportService.DefaultFormat
        };


        var path = _config.UtilitiesService.IsPublic
              ? $"{_config.UtilitiesService.Path}{_config.UtilitiesService.ExportService.Path}"
              : $"{_config.BaseUrl}{_config.UtilitiesService.Path}/{_config.UtilitiesService.ExportService.Path}";


        var resx = await PostQueryWithConversionAsync<ExportResponse>(parameters, path, _config.UtilitiesService.NeedsToken, cancellationToken);

        if (resx.IsFailure) {
            return resx.ConvertFailure<Stream>();
        }

        var jobResx = await CheckJobAsync(resx.Value.Id, delay, cancellationToken);
        if (jobResx.IsFailure) {
            return resx.ConvertFailure<Stream>();
        }

        var outputResx = await GetOutputDetailsAsync(jobResx.Value, resx.Value.Id, cancellationToken);
        if (outputResx.IsFailure) {
            return resx.ConvertFailure<Stream>();
        }


        return await base.GetEsriGeneratedImageAsync(outputResx.Value, delay, cancellationToken);
    }

    private async Task<Result<string>> CheckJobAsync(string id, int waitTime, CancellationToken cancellationToken)
    {
        var path = _config.UtilitiesService.IsPublic
            ? $"{_config.UtilitiesService.Path}{_config.UtilitiesService.JobStatusService.Path.Replace("{0}", id)}"
            : $"{_config.BaseUrl}{_config.UtilitiesService.Path}/{_config.UtilitiesService.JobStatusService.Path.Replace("{0}", id)}";

        Result<string> result;

        while (true) {
            var resx = await PostQueryWithConversionAsync<JobResponse>(new CommonParameters(), path, _config.UtilitiesService.NeedsToken, cancellationToken);
            if (resx.IsFailure) {
                _logger.LogError("Failed to read response for job {JobId}: {Error}", id, resx.Error);
                result = Result.Failure<string>("Unable to read response");
                break;
            }

            if (_config.UtilitiesService.JobStatusService.Status.FailedStates.Contains(resx.Value.Status)) {
                _logger.LogError("Job {JobId} failed with status: {Status}", id, resx.Value.Status);
                result = Result.Failure<string>("Job Failed");
                break;
            }

            if (_config.UtilitiesService.JobStatusService.Status.SuccessStates.Contains(resx.Value.Status)) {
                _logger.LogInformation("Job {JobId} succeeded with status: {Status}", id, resx.Value.Status);
                result = Result.Success($"{path}/{resx.Value.Results!.OutputPath.Url}");
                break;
            }

            // Add a delay between polling attempts
            await Task.Delay(waitTime, cancellationToken);
        }

        return result;
    }

    private async Task<Result<string>> GetOutputDetailsAsync(string url, string id, CancellationToken cancellationToken)
    {
        var resx = await PostQueryWithConversionAsync<OutputResponse>(new CommonParameters(), url, _config.UtilitiesService.NeedsToken, cancellationToken);
        if (!resx.IsFailure) {
            return Result.Success(resx.Value.Value.Url);
        }

        _logger.LogError("Failed to read response for Output {JobId}: {Error}", id, resx.Error);
        return Result.Failure<string>("Unable to read response");

    }
}

