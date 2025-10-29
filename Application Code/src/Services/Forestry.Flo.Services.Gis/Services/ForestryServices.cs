using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using GeoUK;
using GeoUK.Coordinates;
using GeoUK.Ellipsoids;
using GeoUK.Projections;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Convert = GeoUK.Convert;

namespace Forestry.Flo.Services.Gis.Services;

public class ForestryServices : BaseServices, IForestryServices
{
    private readonly ForestryConfig _config;
    private readonly ILogger<ForestryServices> _logger;

    public ForestryServices(EsriConfig config, IHttpClientFactory httpClientFactory, ILogger<ForestryServices> logger)
        : base(httpClientFactory, "ForestryServices", logger)
    {
        Guard.Against.Null(config);
        Guard.Against.Null(config.Forestry, "Forestry not configured");
        Guard.Against.Null(httpClientFactory);
        _config = config.Forestry;
        _logger = logger;
        TokenRequest = new GetTokenParameters(_config.GenerateTokenService.ClientID, _config.GenerateTokenService.ClientSecret, true);
        GetTokenPath = $"{_config.BaseUrl}{_config.GenerateTokenService.Path}";
        SpatialReference = config.SpatialReference;
        LayerSettings = config.Forestry.LayerServices ?? new();
        GeometryService = config.Forestry.GeometryService;
    }

    /// <inheritdoc /> 
    public async Task<Result<Point>> CalculateCentrePointAsync(List<string> compartments, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["Compartments"] = compartments
        }))
        {
            _logger.LogInformation("CalculateCentrePointAsync called with compartments: {@Compartments}", compartments);

            var result = await base.CalculateCentrePointAsync(compartments, _config.BaseUrl, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to calculate centre point: {Error}", result.Error);
            }
            else
            {
                _logger.LogInformation("Centre point calculated: {@Point}", result.Value);
            }

            return result;
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetOSGridReferenceAsync(Point point, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["Point"] = point
        }))
        {
            _logger.LogInformation("GetOSGridReferenceAsync called with point: {@Point}", point);

            var conversionResult = await ConvertPointToLatLongAsync(point, _config.BaseUrl, cancellationToken);

            if (conversionResult.IsFailure)
            {
                _logger.LogError("Failed to convert point to lat/long: {Error}", conversionResult.Error);
                return conversionResult.ConvertFailure<string>();
            }

            _logger.LogInformation("Lat/Long conversion result: {@LatLong}", conversionResult.Value);

            var osGridResult = ConvertLatLongToOSGrid(conversionResult.Value, _config.GeometryService.ProjectService.GridLength, _config.GeometryService.ProjectService.IncludeSpaces, cancellationToken);

            if (osGridResult.IsFailure)
            {
                _logger.LogError("Failed to convert lat/long to OS Grid: {Error}", osGridResult.Error);
            }
            else
            {
                _logger.LogInformation("OS Grid Reference: {OSGrid}", osGridResult.Value);
            }

            return osGridResult;
        }
    }

    ///<inheritdoc />
    public async Task<Result<string>> GetFeaturesFromFileAsync(string name, string ext, bool generalize, int offset, bool reduce, int roundTo,
          byte[] file, string uploadType, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["FileName"] = $"{name}.{ext}",
            ["UploadType"] = uploadType
        }))
        {
            _logger.LogInformation("GetFeaturesFromFileAsync called with file: {FileName}, uploadType: {UploadType}", $"{name}.{ext}", uploadType);

            Guard.Against.NullOrEmpty(file, "No valid file contents set");
            Guard.Against.Zero(SpatialReference);
            Guard.Against.Null(_config.FeaturesService);
            Guard.Against.Null(_config.FeaturesService.GenerateService);

            var parameters = new GenerateParameters()
            {
                FileType = uploadType,
                PublishParameters = new PublishParameters()
                {
                    SpatialReference = new Models.Esri.RequestObjects.Form.SpatialReference
                    {
                        Wkid = SpatialReference
                    },
                    EnforceInputFileSizeLimit = _config.FeaturesService.GenerateService.EnforceInputFileSizeLimit,
                    EnforceOutputJsonSizeLimit = _config.FeaturesService.GenerateService.EnforceOutputJsonSizeLimit,
                    MaxNumberOfRecords = _config.FeaturesService.GenerateService.MaxRecords ?? 100,
                    FileName = $"{name}.{ext}"
                }
            };
            var path = _config.FeaturesService.IsPublic
                ? $"{_config.FeaturesService.Path}{_config.FeaturesService.GenerateService.Path}"
                : $"{_config.BaseUrl}{_config.FeaturesService.Path}/{_config.FeaturesService.GenerateService.Path}";

            _logger.LogDebug("Posting file to ESRI at {Path} with parameters: {@Parameters}", path, parameters);

            var resx = await PostFileToEsriAsync(new ByteArrayContent(file), path, parameters, _config.FeaturesService.NeedsToken, cancellationToken);

            if (resx.IsFailure)
            {
                _logger.LogError("Failed to get features from file: {Error}", resx.Error);
            }
            else
            {
                _logger.LogInformation("Features generated from file successfully.");
            }

            return resx;
        }
    }

    ///<inheritdoc />
    public async Task<Result<string>> GetFeaturesFromStringAsync(string name, string ext, bool generalize,
        int offset, bool reduce, int roundTo,
        string conversionString, string uploadType, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["FileName"] = $"{name}.{ext}",
            ["UploadType"] = uploadType
        }))
        {
            _logger.LogInformation("GetFeaturesFromStringAsync called with file: {FileName}, uploadType: {UploadType}", $"{name}.{ext}", uploadType);

            Guard.Against.NullOrEmpty(conversionString, "No valid text set");
            Guard.Against.Zero(SpatialReference);
            Guard.Against.Null(_config.FeaturesService);
            Guard.Against.Null(_config.FeaturesService.GenerateService);

            var parameters = new GenerateParameters()
            {
                FileType = uploadType,
                Text = conversionString,
                PublishParameters = new PublishParameters()
                {
                    SpatialReference = new Models.Esri.RequestObjects.Form.SpatialReference
                    {
                        Wkid = SpatialReference
                    },
                    EnforceInputFileSizeLimit =
                        _config.FeaturesService.GenerateService.EnforceInputFileSizeLimit,
                    EnforceOutputJsonSizeLimit =
                        _config.FeaturesService.GenerateService.EnforceOutputJsonSizeLimit,
                    MaxNumberOfRecords = _config.FeaturesService.GenerateService.MaxRecords ?? 100
                }
            };
            var path =
                $"{_config.BaseUrl}{_config.FeaturesService.Path}/{_config.FeaturesService.GenerateService.Path}";

            _logger.LogDebug("Posting string to ESRI at {Path} with parameters: {@Parameters}", path, parameters);

            var resx = await PostQueryAsync(parameters, path, _config.FeaturesService.NeedsToken, false,
                cancellationToken);

            if (resx.IsFailure)
            {
                _logger.LogError("Failed to get features from string: {Error}", resx.Error);
            }
            else
            {
                _logger.LogInformation("Features generated from string successfully.");
            }

            return resx;
        }
    }

    /// <summary>
    /// Converts a set of lat longs to the OS grid
    /// </summary>
    /// <param name="latLongObj">The Lat Long Obj</param>
    /// <param name="gridLength">The number of characters for string</param>
    /// <param name="includeSpaces">Add spaces to the ref</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <returns></returns>
    protected Result<string> ConvertLatLongToOSGrid(LatLongObj latLongObj, int gridLength, bool includeSpaces,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("ConvertLatLongToOSGrid called with latLongObj: {@LatLongObj}, gridLength: {GridLength}, includeSpaces: {IncludeSpaces}", latLongObj, gridLength, includeSpaces);

        try
        {
            var cartesian = Convert.ToCartesian(new Wgs84(),
                new LatitudeLongitude(latLongObj.Latitude, latLongObj.Longitude));
            var bngEN = Convert.ToEastingNorthing(new Airy1830(), new BritishNationalGrid(),
                Transform.Etrs89ToOsgb36(cartesian));

            var osgb36EN = new Osgb36(bngEN);
            var mapReference = osgb36EN.MapReference;

            var space = string.Empty;
            if (includeSpaces)
            {
                space = " ";
            }

            if ((gridLength is < 8) || (gridLength % 2) != 0)
            {
                gridLength = 8;
            }

            if (!Regex.IsMatch(mapReference[..2], @"^[a-zA-Z]+$"))
            {
                _logger.LogWarning("The given points are not in the UK: {@LatLongObj}", latLongObj);
                return Result.Failure<string>("The given points are not in the uk");
            }

            var result = gridLength == 0
                ? $"{mapReference[..2]} {bngEN.Easting} {bngEN.Northing}"
                : $"{mapReference[..2]}{space}{bngEN.Easting.ToString().Substring(1, (gridLength - 2) / 2)}{space}{bngEN.Northing.ToString().Substring(1, (gridLength - 2) / 2)}";

            _logger.LogInformation("OS Grid Reference calculated: {OSGrid}", result);

            return Result.Success(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to calculate OS Grid for latLongObj: {@LatLongObj}", latLongObj);
            return Result.Failure<string>("Unable to calculate OS Grid");
        }
    }
}

