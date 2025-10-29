using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Publish;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Services;

/// <summary>
/// Implementation of <see cref="IForesterServices"/>
/// </summary>
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid()
        }))
        {
            _logger.LogInformation("GetAdminBoundaryIdAsync called with shape: {@Shape}", shape);

            Guard.Against.Null(_config.LayerServices);

            var layer = GetLayerDetails("LocalAuthority_Areas");
            if (layer.HasNoValue)
            {
                _logger.LogError("Layer LocalAuthority_Areas has not been set");
                return Result.Failure<AdminBoundary>("Unable to find layer details");
            }

            var query = new QueryFeatureServiceParameters()
            {
                QueryGeometry = shape,
                OutFields = layer.Value.Fields
            };
            var path = $"{layer.Value.ServiceURI}/query";

            _logger.LogDebug("Querying ESRI at {Path} with parameters: {@Query}", path, query);

            var result =
                await PostQueryWithConversionAsync<BaseQueryResponse<AdminBoundary>>(query, path, true,
                    cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("ESRI query failed: {Error}", result.Error);
                return Result.Failure<AdminBoundary>(result.Error);
            }

            if (!result.Value.Results.Any())
            {
                _logger.LogWarning("No admin boundary results found for shape: {@Shape}", shape);
                return Result.Failure<AdminBoundary>("No Results found");
            }

            _logger.LogInformation("Admin boundary found: {@Boundary}", result.Value.Results[0].Record);
            return Result.Success(result.Value.Results[0].Record);
        }
    }

    ///<inheritdoc />
    public async Task<Result<bool>> IsInEnglandAsync(BaseShape shape, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid()
        }))
        {
            _logger.LogInformation("IsInEnglandAsync called with shape: {@Shape}", shape);

            Guard.Against.Null(_config.LayerServices);

            var layer = GetLayerDetails("Country_Boundaries_Generalised");
            if (layer.HasNoValue)
            {
                _logger.LogError("Layer Country_Boundaries_Generalised has not been set");
                return Result.Failure<bool>("Unable to find layer details");
            }

            var query = new QueryFeatureServiceParameters()
            {
                QueryGeometry = shape,
                OutFields = layer.Value.Fields,
                SpatialRelationship = "intersects"
            };
            var path = $"{layer.Value.ServiceURI}/query";

            _logger.LogDebug("Querying ESRI at {Path} with parameters: {@Query}", path, query);

            var result =
                await PostQueryWithConversionAsync<BaseQueryResponse<CountryBoundary>>(query, path, true,
                    cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("ESRI query failed: {Error}", result.Error);
                return Result.Failure<bool>(result.Error);
            }

            _logger.LogInformation("England checks found: {@Results}", result.Value.Results);

            var isInEngland = result.IsSuccess &&
                result.Value.Results.Count == 1 &&
                result.Value.Results.Any(x => x.Record.Code == _config.CountryCode);

            if (!isInEngland)
            {
                _logger.LogWarning("Shape is not in England: {@Shape}", shape);
            }
            else
            {
                _logger.LogInformation("Shape is in England: {@Shape}", shape);
            }

            return Result.Success(isInEngland);
        }
    }

    ///<inheritdoc />
    public async Task<Result<WoodlandOfficer>> GetWoodlandOfficerAsync(List<string> compartments, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid()
        }))
        {
            _logger.LogInformation("GetWoodlandOfficerAsync called with compartments: {@Compartments}", compartments);

            var pointResult = await CalculateCentrePointAsync(compartments, _config.BaseUrl, cancellationToken);

            if (pointResult.IsFailure)
            {
                _logger.LogError("ESRI query failed: {Error}", pointResult.Error);
                return Result.Failure<WoodlandOfficer>(pointResult.Error);
            }

            _logger.LogInformation("Centre point calculated: {@Point}", pointResult.Value);

            var officerResult = await GetWoodlandOfficerAsync(pointResult.Value, cancellationToken);

            if (officerResult.IsFailure)
            {
                _logger.LogWarning("No woodland officer found for compartments: {@Compartments}", compartments);
                return Result.Failure<WoodlandOfficer>(officerResult.Error);
            }

            _logger.LogInformation("Woodland officer found: {@Officer}", officerResult.Value);
            return officerResult;
        }
    }

    ///<inheritdoc />
    public async Task<Result<WoodlandOfficer>> GetWoodlandOfficerAsync(Point centralCasePoint, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid()
        }))
        {
            _logger.LogInformation("GetWoodlandOfficerAsync called with Point: {@Point}", centralCasePoint);
            Guard.Against.Null(_config.LayerServices);

            var layer = GetLayerDetails("Woodland_Officers");
            if (layer.HasNoValue)
            {
                _logger.LogError("Layer Woodland_Officers has not been set");
                return Result.Failure<WoodlandOfficer>("Unable to find layer details");
            }

            var query = new QueryFeatureServiceParameters()
            {
                QueryGeometry = centralCasePoint,
                OutFields = layer.Value.Fields
            };
            var path = $"{layer.Value.ServiceURI}/query";
            _logger.LogDebug("Querying ESRI at {Path} with parameters: {@Query}", path, query);

            var result =
                await PostQueryWithConversionAsync<BaseQueryResponse<WoodlandOfficer>>(query, path, true,
                    cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("ESRI query failed: {Error}", result.Error);
                return Result.Failure<WoodlandOfficer>(result.Error);
            }

            if (!result.Value.Results.Any())
            {
                _logger.LogWarning("No woodland officer results found for point: {@Point}", centralCasePoint);
                return Result.Failure<WoodlandOfficer>("No Results found");
            }

            _logger.LogInformation("Woodland officer found: {@Officer}", result.Value.Results[0].Record);
            return Result.Success(result.Value.Results[0].Record);
        }
    }

    ///<inheritdoc />
    public async Task<Result<List<PhytophthoraRamorumRiskZone>>> GetPhytophthoraRamorumRiskZonesAsync(BaseShape shape, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid()
        }))
        {
            _logger.LogInformation("GetPhytophthoraRamorumRiskZonesAsync called with shape: {@Shape}", shape);

            Guard.Against.Null(_config.LayerServices);

            var layer = GetLayerDetails("Phytophthora_Ramorum_Risk_Zones");
            if (layer.HasNoValue)
            {
                _logger.LogError("Layer Phytophthora_Ramorum_Risk_Zones has not been set");
                return Result.Failure<List<PhytophthoraRamorumRiskZone>>("Unable to find layer details");
            }

            var query = new QueryFeatureServiceParameters()
            {
                QueryGeometry = shape,
                OutFields = layer.Value.Fields
            };
            var path = $"{layer.Value.ServiceURI}/query";
            _logger.LogDebug("Querying ESRI at {Path} with parameters: {@Query}", path, query);

            var result = await PostQueryWithConversionAsync<BaseQueryResponse<PhytophthoraRamorumRiskZone>>(query, path, true, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("ESRI query failed: {Error}", result.Error);
                return Result.Failure<List<PhytophthoraRamorumRiskZone>>(result.Error);
            }

            var zones = result.Value.Results.Select((x) => x.Record).ToList();

            if (!zones.Any())
            {
                _logger.LogWarning("No Phytophthora Ramorum Risk Zones found for shape: {@Shape}", shape);
            }
            else
            {
                _logger.LogInformation("Phytophthora Ramorum Risk Zones found: {@Zones}", zones);
            }

            return Result.Success(zones);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<AncientWoodland>>> GetAncientWoodlandAsync(BaseShape shape, CancellationToken cancellationToken)
    {
        Guard.Against.Null(_config.LayerServices);

        var layer = GetLayerDetails("Ancient_Woodland");
        if (layer.HasNoValue)
        {
            return Result.Failure<List<AncientWoodland>>("Unable to find layer details");
        }

        var query = new QueryFeatureServiceParameters()
        {
            QueryGeometry = shape,
            OutFields = layer.Value.Fields
        };
        var path = $"{layer.Value.ServiceURI}/query";
        var result = await PostQueryWithConversionAsync<BaseQueryResponse<AncientWoodland>>(query, path, layer.Value.NeedsToken, cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<List<AncientWoodland>>(result.Error);
        }

        return !result.Value.Results.Any() ? Result.Failure<List<AncientWoodland>>("No Results found")
            : Result.Success(result.Value.Results.Select(x => x.Record).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<List<AncientWoodland>>> GetAncientWoodlandsRevisedAsync(BaseShape shape, CancellationToken cancellationToken)
    {
        Guard.Against.Null(_config.LayerServices);

        var layer = GetLayerDetails("Ancient_Woodlands_Revised");
        if (layer.HasNoValue)
        {
            return Result.Failure<List<AncientWoodland>>("Unable to find layer details");
        }

        var query = new QueryFeatureServiceParameters()
        {
            QueryGeometry = shape,
            OutFields = layer.Value.Fields
        };
        var path = $"{layer.Value.ServiceURI}/query";
        var result = await PostQueryWithConversionAsync<BaseQueryResponse<AncientWoodland>>(query, path, layer.Value.NeedsToken, cancellationToken);
        if (result.IsFailure)
        {
            return Result.Failure<List<AncientWoodland>>(result.Error);
        }

        return !result.Value.Results.Any() ? Result.Failure<List<AncientWoodland>>("No Results found")
            : Result.Success(result.Value.Results.Select(x => x.Record).ToList());
    }

    ///<inheritdoc />
    public async Task<Result<LocalAuthority>> GetLocalAuthorityAsync(Point centralCasePoint, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid()
        }))
        {
            _logger.LogInformation("GetLocalAuthorityAsync called with Point: {@Point}", centralCasePoint);

            Guard.Against.Null(_config.LayerServices);

            var layer = GetLayerDetails("LocalAuthority_Areas");
            if (layer.HasNoValue)
            {
                _logger.LogError("Layer LocalAuthority_Areas has not been set");
                return Result.Failure<LocalAuthority>("Unable to find layer details");
            }

            var query = new QueryFeatureServiceParameters()
            {
                QueryGeometry = centralCasePoint,
                OutFields = layer.Value.Fields
            };
            var path = $"{layer.Value.ServiceURI}/query";
            _logger.LogDebug("Querying ESRI at {Path} with parameters: {@Query}", path, query);

            var result = await PostQueryWithConversionAsync<BaseQueryResponse<LocalAuthority>>(query, path, layer.Value.NeedsToken, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError("ESRI query failed: {Error}", result.Error);
                return Result.Failure<LocalAuthority>(result.Error);
            }

            if (!result.Value.Results.Any())
            {
                _logger.LogWarning("No local authority results found for point: {@Point}", centralCasePoint);
                return Result.Failure<LocalAuthority>("No Results found");
            }

            _logger.LogInformation("Local authority found: {@Authority}", result.Value.Results[0].Record);
            return Result.Success(result.Value.Results[0].Record);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> GenerateImage_SingleCompartmentAsync(
        InternalCompartmentDetails<BaseShape> compartment, 
        CancellationToken cancellationToken,
        int delay = 30000,
        MapGeneration generationType = MapGeneration.Other,
        string title = "")
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid()
        }))
        {
            _logger.LogInformation("GenerateImage_SingleCompartmentAsync called with compartment: {@Compartment}, delay: {Delay}, generationType: {GenerationType}, title: {Title}", compartment, delay, generationType, title);

            Guard.Against.Null(_config.UtilitiesService);

            var extend = compartment.ShapeGeometry.GetExtent();

            if (extend.HasNoValue)
            {
                _logger.LogError("Unable to calculate extent for compartment: {@Compartment}", compartment);
                return Result.Failure<Stream>("Unable to calculate Extend");
            }

            var bufferX = CalculateBuffer(extend.Value.X_min, extend.Value.X_max);
            var bufferY = CalculateBuffer(extend.Value.Y_min, extend.Value.Y_max);

            WebMap_LayoutOptions templateLayoutOptions = new()
            {
                Copyright_Text = _config.UtilitiesService.ExportService.TextOverrides.Copyright
            };

            if (generationType != MapGeneration.Other)
            {
                templateLayoutOptions.Title_Text = generationType == MapGeneration.Restocking ?
                    _config.UtilitiesService.ExportService.TextOverrides.RestockingTitle.Replace("{0}", title)
                    : _config.UtilitiesService.ExportService.TextOverrides.FellingTitle.Replace("{0}", title);
            }
            else
            {
                if (!string.IsNullOrEmpty(title))
                {
                    templateLayoutOptions.Title_Text = title;
                }
            }

            WebMap_Json map = new()
            {
                ExportOptions =
                {
                    Size = [1100, 800]
                }
            };
            map.BaseMap[0].Layers.Add(new BaseMapLayer(_config.UtilitiesService.ExportService.BaseMap));
            map.MapOptions.MapExtent.X_min = extend.Value.X_min - bufferX;
            map.MapOptions.MapExtent.Y_min = extend.Value.Y_min - bufferY;
            map.MapOptions.MapExtent.X_max = extend.Value.X_max + bufferX;
            map.MapOptions.MapExtent.Y_max = extend.Value.Y_max + bufferY;
            map.LayoutOptions = templateLayoutOptions;

            map.OperationalLayers.Add(new WebMap_OperationalLayerMap
            {
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
            map.OperationalLayers.Add(new WebMapOperationalLayerFeatures(shapeLayer, new FeatureSetDetails(compartment.ShapeGeometry.GeometryType,
                [new FeatureDetails(compartment.ShapeGeometry)])));
            map.OperationalLayers.Add(new WebMapOperationalLayerFeatures(textLayer, new FeatureSetDetails("esriGeometryPoint",
                [new FeatureDetails(compartment.ShapeGeometry, compartment.CompartmentLabel)])));
            ExportParameter parameters = new()
            {
                LayoutTemplate = _layoutTemplate,
                MapJson = map,
                FileFormat = _config.UtilitiesService.ExportService.DefaultFormat
            };

            var path = _config.UtilitiesService.IsPublic
                  ? $"{_config.UtilitiesService.Path}{_config.UtilitiesService.ExportService.Path}"
                  : $"{_config.BaseUrl}{_config.UtilitiesService.Path}/{_config.UtilitiesService.ExportService.Path}";

            _logger.LogDebug("Posting export request to {Path} with parameters: {@Parameters}", path, parameters);

            var resx = await PostQueryWithConversionAsync<ExportResponse>(parameters, path, _config.UtilitiesService.NeedsToken, cancellationToken);

            if (resx.IsFailure)
            {
                _logger.LogError("Export request failed: {Error}", resx.Error);
                return resx.ConvertFailure<Stream>();
            }

            var jobResx = await CheckJobAsync(resx.Value.Id, delay, cancellationToken);
            if (jobResx.IsFailure)
            {
                _logger.LogError("Job check failed: {Error}", jobResx.Error);
                return jobResx.ConvertFailure<Stream>();
            }

            var outputResx = await GetOutputDetailsAsync(jobResx.Value, resx.Value.Id, cancellationToken);
            if (outputResx.IsFailure)
            {
                _logger.LogError("Output details retrieval failed: {Error}", outputResx.Error);
                return outputResx.ConvertFailure<Stream>();
            }

            _logger.LogInformation("Image generation succeeded for compartment: {@Compartment}", compartment);

            return await GetEsriGeneratedImageAsync(outputResx.Value, delay, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> GenerateImage_MultipleCompartmentsAsync(
        List<InternalCompartmentDetails<BaseShape>> compartments, 
        CancellationToken cancellationToken,
        int delay = 30000,
        MapGeneration generationType = MapGeneration.Other,
        string title = "")
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid()
        }))
        {
            _logger.LogInformation("GenerateImage_MultipleCompartmentsAsync called with compartments: {@Compartments}, delay: {Delay}, generationType: {GenerationType}, title: {Title}", compartments, delay, generationType, title);

            Guard.Against.Null(_config.UtilitiesService);

            WebMap_LayoutOptions templateLayoutOptions = new()
            {
                Copyright_Text = _config.UtilitiesService.ExportService.TextOverrides.Copyright
            };

            if (generationType != MapGeneration.Other)
            {
                templateLayoutOptions.Title_Text = generationType == MapGeneration.Restocking ?
                    _config.UtilitiesService.ExportService.TextOverrides.RestockingTitle.Replace("{0}", title)
                    : _config.UtilitiesService.ExportService.TextOverrides.FellingTitle.Replace("{0}", title);
            }
            else
            {
                if (!string.IsNullOrEmpty(title))
                {
                    templateLayoutOptions.Title_Text = title;
                }
            }

            if (!compartments.Any())
            {
                _logger.LogError("No shapes passed in for image generation");
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

            WebMap_Json map = new()
            {
                ExportOptions =
                {
                    Size = [1100, 800]
                }
            };
            map.BaseMap[0].Layers.Add(new BaseMapLayer(_config.UtilitiesService.ExportService.BaseMap));
            map.MapOptions.MapExtent.X_min = xMin - bufferX;
            map.MapOptions.MapExtent.Y_min = yMin - bufferY;
            map.MapOptions.MapExtent.X_max = xMax + bufferX;
            map.MapOptions.MapExtent.Y_max = yMax + bufferY;
            map.LayoutOptions = templateLayoutOptions;

            map.OperationalLayers.Add(new WebMap_OperationalLayerMap
            {
                ID = _config.UtilitiesService.ExportService.BaseMapID,
                Title = "BaseMap",
                URL = _config.UtilitiesService.ExportService.BaseMap,
                Token = _config.ApiKey,
                Opacity = 1,
                MaxScale = 250,
                MinScale = 500000000,
                LayerType = "ArcGISTiledMapServiceLayer"
            });

            foreach (var item in compartments.GroupBy(c => c.ShapeGeometry.GeometryType).ToList())
            {
                var builtLayer = GetLayerDefinition(item.Key);

                map.OperationalLayers.Add(new WebMapOperationalLayerFeatures(builtLayer,
                    new FeatureSetDetails(item.Key,
                        item.Select(s => new FeatureDetails(s.ShapeGeometry)).ToList())));
            }
            var textLayer = GetLayerDefinition("text");
            map.OperationalLayers.Add(new WebMapOperationalLayerFeatures(textLayer, new FeatureSetDetails("esriGeometryPoint", compartments.Select(c => new FeatureDetails(c.ShapeGeometry, c.CompartmentLabel)).ToList())));

            ExportParameter parameters = new()
            {
                LayoutTemplate = _layoutTemplate,
                MapJson = map,
                FileFormat = _config.UtilitiesService.ExportService.DefaultFormat
            };

            var path = _config.UtilitiesService.IsPublic
                  ? $"{_config.UtilitiesService.Path}{_config.UtilitiesService.ExportService.Path}"
                  : $"{_config.BaseUrl}{_config.UtilitiesService.Path}/{_config.UtilitiesService.ExportService.Path}";

            _logger.LogDebug("Posting export request to {Path} with parameters: {@Parameters}", path, parameters);

            var resx = await PostQueryWithConversionAsync<ExportResponse>(parameters, path, _config.UtilitiesService.NeedsToken, cancellationToken);

            if (resx.IsFailure)
            {
                _logger.LogError("Export request failed: {Error}", resx.Error);
                return resx.ConvertFailure<Stream>();
            }

            var jobResx = await CheckJobAsync(resx.Value.Id, delay, cancellationToken);
            if (jobResx.IsFailure)
            {
                _logger.LogError("Job check failed: {Error}", jobResx.Error);
                return jobResx.ConvertFailure<Stream>();
            }

            var outputResx = await GetOutputDetailsAsync(jobResx.Value, resx.Value.Id, cancellationToken);
            if (outputResx.IsFailure)
            {
                _logger.LogError("Output details retrieval failed: {Error}", outputResx.Error);
                return outputResx.ConvertFailure<Stream>();
            }

            _logger.LogInformation("Image generation succeeded for compartments: {@Compartments}", compartments);

            return await GetEsriGeneratedImageAsync(outputResx.Value, delay, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<Result> Publish_FLAToInternalAsync(
        string? propertyName,
        string applicationRef,
        string? gridReference,
        string? nearestTown,
        string? adminHubAreaName,
        DateTime? consultationPublicRegisterStartDate,
        int? consultationPublicRegisterPeriod,
        DateTime? consultationPublicRegisterEndDate,
        DateTime? decisionPublicRegisterStartDate,
        int? decisionPublicRegisterPeriod,
        DateTime? decisionPublicRegisterEndDateTime,
        string? compartmentLabel,
        float? confirmedTotalAreaHa,
        string? fsAreaName,
        string? currentApplicationStatus,
        string? applicant,
        bool hasConditions,
        DateTime? submittedDate,
        DateTime? decisionDate,
        DateTime? withdrawalDate,
        string? adminOfficer,
        string? woodLandOfficer,
        string? approvingOfficer,
        DateTime? expiredDate,
        string? expiryCategory,
        List<InternalCompartmentDetails<Polygon>> compartments,
        CancellationToken cancellationToken)
    {

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["Application Ref"] = applicationRef
        }))
        {

            _logger.LogInformation("Publish_FLAToInternal called for caseRef: {ApplicationReference}", applicationRef);

            if (compartments.Count == 0)
            {
                _logger.LogError("No compartments set for application having reference {ApplicationReference}",
                    applicationRef);
                return Result.Failure<int>("No compartments Set");
            }

            var layer = GetLayerDetails("ExternalFLA");
            if (layer.HasNoValue)
            {
                _logger.LogError("Layer LocalAuthority_Areas has not been set");
                return Result.Failure<LocalAuthority>("Unable to find layer details");
            }

            var geometryResult = ShapeHelper.MakeMultiPart(compartments.Select(c => c.ShapeGeometry).ToList());
            if (geometryResult.IsFailure)
            {
                _logger.LogError(
                    "Unable to create a multipart geometry for the compartments for application having reference {ApplicationReference} with error {Error}",
                    applicationRef, geometryResult.Error);
                return Result.Failure<int>(geometryResult.Error);
            }

            var bodyObj = new BaseFeatureWithGeometryObject<Polygon, InternalFellingLicenceApplication<int>>
            {
                GeometryObject = geometryResult.Value,
                Attributes = new InternalFellingLicenceApplication<int>()
                {
                    Conditions = hasConditions ? "Yes" : "No",
                    ExpiryCategory = expiryCategory,
                    AdminHubAreaName = adminHubAreaName,
                    AdminOfficer = adminOfficer,
                    Applicant = applicant,
                    ApplicationRef = applicationRef,
                    ApprovingOfficer = approvingOfficer,
                    CompartmentLabel = compartmentLabel,
                    ConfirmedTotalAreaHa = confirmedTotalAreaHa,
                    ConsultationPublicRegisterEndDate = consultationPublicRegisterEndDate,
                    ConsultationPublicRegisterPeriod = consultationPublicRegisterPeriod,
                    ConsultationPublicRegisterStartDate = consultationPublicRegisterStartDate,
                    CurrentApplicationStatus = currentApplicationStatus,
                    DecisionDate = decisionDate,
                    DecisionPublicRegisterEndDate = decisionPublicRegisterEndDateTime,
                    DecisionPublicRegisterPeriod = decisionPublicRegisterPeriod,
                    DecisionPublicRegisterStartDate = decisionPublicRegisterStartDate,
                    FsAreaName = fsAreaName,
                    ExpiredDate = expiredDate,
                    GridReference = gridReference,
                    NearestTown = nearestTown,
                    PropertyName = propertyName,
                    SubmittedDate = submittedDate,
                    WithdrawalDate = withdrawalDate,
                    WoodLandOfficer = woodLandOfficer
                }
            };

            var objToAdd = new EditFeaturesParameter($"{JsonConvert.SerializeObject(bodyObj)}");

            var path = $"{layer.Value.ServiceURI}/addFeatures";
            var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(objToAdd, path, layer.Value.NeedsToken, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Attempting to add application reference {ApplicationReference} to {Layer} failed with error {Error}", layer.Value.Name, applicationRef, result.Error);
                return Result.Failure<int>(result.Error);
            }

            if (result.Value.AddResults == null)
            {
                _logger.LogError("Attempting to add application reference {ApplicationReference}  to {Layer}  returned no results", layer.Value.Name, applicationRef);
                return Result.Failure<int>("No Results");
            }

            var errorResults = result.Value.AddResults.Where(r => r.ErrorDetails != null).Select(e => e.ErrorDetails).ToList();
            if (!errorResults.Any())
            {
                return Result.Success();
            }
            _logger.LogError("Attempting to add application reference {ApplicationReference} to {layer}  returned errors: {Errors}", layer.Value.Name, applicationRef, string.Join(", ", errorResults.Select(r => r!.Details)));
            return Result.Failure<int>(string.Join(", ", errorResults.Select(r => r!.Details)));

        }
    }

    /// <inheritdoc />
    public async Task<Result> Publish_FLAToExternalAsync(
        string applicationRef,
        string applicationStatus,
        bool hasConditions,
        string expiryCategory,
        List<InternalCompartmentDetails<Polygon>> compartments,
        DateTime? exDate,
        CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["Application Ref"] = applicationRef
        }))
        {
            _logger.LogInformation("Publish_FLAToExternal called for caseRef: {ApplicationReference}", applicationRef);

            if (compartments.Count == 0)
            {
                _logger.LogError("No compartments set for application having reference {ApplicationReference}",
                    applicationRef);
                return Result.Failure<int>("No compartments Set");
            }

            var layer = GetLayerDetails("ExternalFLA");
            if (layer.HasNoValue)
            {
                _logger.LogError("Layer LocalAuthority_Areas has not been set");
                return Result.Failure<LocalAuthority>("Unable to find layer details");
            }

            var geometryResult = ShapeHelper.MakeMultiPart(compartments.Select(c => c.ShapeGeometry).ToList());
            if (geometryResult.IsFailure)
            {
                _logger.LogError(
                    "Unable to create a multipart geometry for the compartments for application having reference {ApplicationReference} with error {Error}",
                    applicationRef, geometryResult.Error);
                return Result.Failure<int>(geometryResult.Error);
            }

            var bodyObj = new BaseFeatureWithGeometryObject<Polygon, ExternalFellingLicenceApplication<int>>
            {
                GeometryObject = geometryResult.Value,
                Attributes = new ExternalFellingLicenceApplication<int>
                {
                    ApplicationReference = applicationRef,
                    ApplicationStatus = applicationStatus,
                    Conditions = hasConditions ? "Yes" : "No",
                    ExpiryCategory = expiryCategory,
                    ExpiryDate = exDate
                }
            };


            var objToAdd = new EditFeaturesParameter($"{JsonConvert.SerializeObject(bodyObj)}");

            var path = $"{layer.Value.ServiceURI}/addFeatures";
            var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(objToAdd, path, layer.Value.NeedsToken, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Attempting to add application reference {ApplicationReference} to {Layer} failed with error {Error}", layer.Value.Name, applicationRef, result.Error);
                return Result.Failure<int>(result.Error);
            }

            if (result.Value.AddResults == null)
            {
                _logger.LogError("Attempting to add application reference {ApplicationReference}  to {Layer}  returned no results", layer.Value.Name, applicationRef);
                return Result.Failure<int>("No Results");
            }

            var errorResults = result.Value.AddResults.Where(r => r.ErrorDetails != null).Select(e => e.ErrorDetails).ToList();
            if (!errorResults.Any())
            {
                return Result.Success();
            }
            _logger.LogError("Attempting to add application reference {ApplicationReference} to {Layer}  returned errors: {Errors}", layer.Value.Name, applicationRef, string.Join(", ", errorResults.Select(r => r!.Details)));
            return Result.Failure<int>(string.Join(", ", errorResults.Select(r => r!.Details)));

        }
    }

    /// <summary>
    /// Checks the status of a job asynchronously, polling the job status service until the job succeeds, fails, or the operation is canceled.
    /// </summary>
    /// <param name="id">The unique identifier of the job to check.</param>
    /// <param name="waitTime">The delay in milliseconds between polling attempts.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the URL of the job's output if successful, or an error message if the job fails or an error occurs.
    /// </returns>
    private async Task<Result<string>> CheckJobAsync(string id, int waitTime, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CheckJobAsync started for JobId: {JobId} with waitTime: {WaitTime}", id, waitTime);

        var path = _config.UtilitiesService.IsPublic
            ? $"{_config.UtilitiesService.Path}{_config.UtilitiesService.JobStatusService.Path.Replace("{0}", id)}"
            : $"{_config.BaseUrl}{_config.UtilitiesService.Path}/{_config.UtilitiesService.JobStatusService.Path.Replace("{0}", id)}";

        Result<string> result;

        while (true)
        {
            _logger.LogDebug("Polling job status at {Path}", path);

            var resx = await PostQueryWithConversionAsync<JobResponse>(new CommonParameters(), path, _config.UtilitiesService.NeedsToken, cancellationToken);
            if (resx.IsFailure)
            {
                _logger.LogError("Failed to read response for job {JobId}: {Error}", id, resx.Error);
                result = Result.Failure<string>("Unable to read response");
                break;
            }

            _logger.LogDebug("Job {JobId} status: {Status}", id, resx.Value.Status);

            if (_config.UtilitiesService.JobStatusService.Status.FailedStates.Contains(resx.Value.Status))
            {
                _logger.LogError("Job {JobId} failed with status: {Status}", id, resx.Value.Status);
                result = Result.Failure<string>("Job Failed");
                break;
            }

            if (_config.UtilitiesService.JobStatusService.Status.SuccessStates.Contains(resx.Value.Status))
            {
                _logger.LogInformation("Job {JobId} succeeded with status: {Status}", id, resx.Value.Status);
                result = Result.Success($"{path}/{resx.Value.Results!.OutputPath.Url}");
                break;
            }

            // Add a delay between polling attempts
            await Task.Delay(waitTime, cancellationToken);
        }

        _logger.LogInformation("CheckJobAsync completed for JobId: {JobId} with result: {Result}", id, result.IsSuccess ? "Success" : "Failure");
        return result;
    }

    /// <summary>
    /// Retrieves the output details for a specific job by making an asynchronous request to the provided URL.
    /// </summary>
    /// <param name="url">The URL to query for output details.</param>
    /// <param name="id">The unique identifier of the job for which output details are being retrieved.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation. The task result contains a <see cref="Result{T}"/> 
    /// with the URL of the output if successful, or an error message if the operation fails.
    /// </returns>
    private async Task<Result<string>> GetOutputDetailsAsync(string url, string id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetOutputDetailsAsync started for JobId: {JobId} at url: {Url}", id, url);

        var resx = await PostQueryWithConversionAsync<OutputResponse>(new CommonParameters(), url, _config.UtilitiesService.NeedsToken, cancellationToken);

        if (!resx.IsFailure)
        {
            _logger.LogInformation("Output details retrieved for JobId: {JobId}, OutputUrl: {OutputUrl}", id, resx.Value.Value.Url);
            return Result.Success(resx.Value.Value.Url);
        }

        _logger.LogError("Failed to read response for Output {JobId}: {Error}", id, resx.Error);
        return Result.Failure<string>("Unable to read response");
    }


}

