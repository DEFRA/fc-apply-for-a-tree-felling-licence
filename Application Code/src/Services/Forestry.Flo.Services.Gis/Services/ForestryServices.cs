using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal;
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
        LayerSettings = config.Forestry.LayerServices ?? new(); LayerSettings = config.Forestry.LayerServices ?? new();
        GeometryService = config.Forestry.GeometryService;
    }

    /// <inheritdoc /> 
    public async Task<Result<Point>> CalculateCentrePointAsync(List<string> compartments, CancellationToken cancellationToken)
    {
        return await base.CalculateCentrePointAsync(compartments, _config.BaseUrl, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetOSGridReferenceAsync(Point point, CancellationToken cancellationToken)
    {
        var conversionResult = await ConvertPointToLatLongAsync(point, _config.BaseUrl, cancellationToken);
        return conversionResult.IsFailure ?
            conversionResult.ConvertFailure<string>()
            : ConvertLatLongToOSGrid(conversionResult.Value, _config.GeometryService.ProjectService.GridLength, _config.GeometryService.ProjectService.IncludeSpaces, cancellationToken);
    }

    ///<inheritdoc />
    public async Task<Result<string>> GetFeaturesFromFileAsync(string name, string ext, bool generalize, int offset, bool reduce, int roundTo,
          byte[] file, string uploadType, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrEmpty(file, "No valid file contents set");
        Guard.Against.Zero(SpatialReference);
        Guard.Against.Null(_config.FeaturesService);
        Guard.Against.Null(_config.FeaturesService.GenerateService);

        var parameters = new GenerateParameters() {
            FileType = uploadType,
            PublishParameters = new PublishParameters() {
                SpatialReference = new Models.Esri.RequestObjects.Form.SpatialReference {
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

        var resx = await PostFileToEsriAsync(new ByteArrayContent(file), path, parameters, _config.FeaturesService.NeedsToken, cancellationToken);

        return resx;
    }


    ///<inheritdoc />
    public async Task<Result<string>> GetFeaturesFromStringAsync(string name, string ext, bool generalize,
        int offset, bool reduce, int roundTo,
        string conversionString, string uploadType, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrEmpty(conversionString, "No valid text set");
        Guard.Against.Zero(SpatialReference);
        Guard.Against.Null(_config.FeaturesService);
        Guard.Against.Null(_config.FeaturesService.GenerateService);

        var parameters = new GenerateParameters() {
            FileType = uploadType,
            Text = conversionString,
            PublishParameters = new PublishParameters() {
                SpatialReference = new Models.Esri.RequestObjects.Form.SpatialReference {
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

        var resx = await PostQueryAsync(parameters, path, _config.FeaturesService.NeedsToken, false,
            cancellationToken);

        return resx;
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
        try {
            var cartesian = Convert.ToCartesian(new Wgs84(), new LatitudeLongitude(latLongObj.Latitude, latLongObj.Longitude));
            var bngEN = Convert.ToEastingNorthing(new Airy1830(), new BritishNationalGrid(),
                Transform.Etrs89ToOsgb36(cartesian));

            var osgb36EN = new Osgb36(bngEN);
            var mapReference = osgb36EN.MapReference;

            var space = string.Empty;
            if (includeSpaces) {
                space = " ";
            }

            if ((gridLength is < 8) || (gridLength % 2) != 0) {
                gridLength = 8;
            }

            if (!Regex.IsMatch(mapReference[..2], @"^[a-zA-Z]+$")) {
                return Result.Failure<string>("The given points are not in the uk");
            }

            return gridLength == 0
                ? Result.Success($"{mapReference[..2]} {bngEN.Easting} {bngEN.Northing}")
                : Result.Success(
                    $"{mapReference[..2]}{space}{bngEN.Easting.ToString().Substring(1, (gridLength - 2) / 2)}{space}{bngEN.Northing.ToString().Substring(1, (gridLength - 2) / 2)}");
        }
        catch (Exception e) {
            _logger.LogError(e.Message);
            return Result.Failure<string>("Unable to calculate OS Grid");
        }
    }
}

