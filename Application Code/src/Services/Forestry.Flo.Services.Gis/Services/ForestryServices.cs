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

        try {
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
        catch (HttpRequestException request) {
            _logger.LogError(request, message: "Request Failure");
            return Result.Failure<string>(request.Message);
        }
        catch (Exception ex) {
            _logger.LogError(ex, message: "Other type");
            return Result.Failure<string>(ex.Message);
        }
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

        try {
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
        catch (HttpRequestException request) {
            _logger.LogError(request, message: "Request Failure");
            return Result.Failure<string>(request.Message);
        }
        catch (Exception ex) {
            _logger.LogError(ex, message: "Other type");
            return Result.Failure<string>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<SiteVisitNotes<Guid>>>> GetVisitNotesAsync(string caseRef,
        CancellationToken cancellationToken)
    {
        //FLOV2-1317 Disabling integration with mobile apps for now

        return Result.Success(new List<SiteVisitNotes<Guid>>(0));

        //Guard.Against.Null(_config.LayerServices);

        //var layer = GetLayerDetails("SiteVisitCompartments");
        //if (layer.HasNoValue)
        //{
        //    return Result.Failure<List<SiteVisitNotes<Guid>>>("Unable to find layer details");
        //}

        //var query = new QueryFeatureServiceParameters()
        //{
        //    WhereString = $"case_reference = '{caseRef}'",
        //    OutFields = layer.Value.Fields
        //};
        //var path = $"{layer.Value.ServiceURI}/query";

        //var result = await PostQueryWithConversionAsync<BaseQueryResponse<MobileCompartment<int>>>(query, path, layer.Value.NeedsToken, cancellationToken);

        //if (result.IsFailure)
        //{
        //    return result.ConvertFailure<List<SiteVisitNotes<Guid>>>();
        //}

        //if (!result.Value.Results.Any())
        //{
        //    return Result.Success(new List<SiteVisitNotes<Guid>>(0));
        //}

        //var visitNotes =
        //    result.Value.Results.Select(r => new SiteVisitNotes<Guid>
        //    {
        //        CaseReference = r.Record.CaseReference,
        //        ObjectID = (Guid)r.Record.EsriId!,
        //        VisitDateTime = r.Record.DateOfVisit,
        //        VisitOfficer = r.Record.Editor,
        //        Notes = string.Format("{0}{1}{2}", 
        //            (r.Record.NotesForFile == null ? "" : $"File Notes:\r\n{r.Record.NotesForFile}"), 
        //            (r.Record.NotesForFile != null && r.Record.NotesForLicence != null ? "\r\n\r\n" : ""),
        //            (r.Record.NotesForLicence == null ? "" : $"Licence Notes:\r\n{r.Record.NotesForLicence}"))
        //    }).ToList();


        //foreach (var visitNote in visitNotes)
        //{
        //    var attachmentsInfoPath = $"{layer.Value.ServiceURI}/{visitNote.ObjectID}/attachments";
        //    var attachments = await PostQueryWithConversionAsync<AttachmentResponse<Guid>>(new AttachmentQuery(), attachmentsInfoPath, layer.Value.NeedsToken, cancellationToken);
        //    if (attachments.IsFailure)
        //    {
        //        continue;
        //    }

        //    if (attachments.Value.Attachments.Count == 0)
        //    {
        //        continue;
        //    }

        //    foreach (var details in attachments.Value.Attachments)
        //    {
        //        var attachmentPath = $"{layer.Value.ServiceURI}/{visitNote.ObjectID}/attachments/{details.ID}";
        //        var file = await GetAttachmentAsync(new AttachmentQuery(), attachmentPath, layer.Value.NeedsToken, cancellationToken);
        //        if (file.IsFailure)
        //        {
        //            _logger.Log(LogLevel.Warning, file.Error);
        //            continue;
        //        }

        //        details.File = file.Value;
        //        visitNote.AttachmentDetails.Add(details);
        //    }
        //}
        //return await Task.FromResult(Result.Success(visitNotes));
    }

    ///<inheritdoc />
    public async Task<Result> SavesCaseToMobileLayersAsync(string caseRef, List<InternalFullCompartmentDetails> compartmentDetailsList,
        CancellationToken cancellationToken)
    {
        //FLOV2-1317 Disabling integration with mobile apps for now
        return Result.Success();

        //if (compartmentDetailsList.Count < 1)
        //{
        //    return Result.Success();
        //}

        //var layer = GetLayerDetails("SiteVisitCompartments");
        //if (layer.HasNoValue)
        //{
        //    return Result.Failure<AdminBoundary>("Unable to find layer details");
        //}

        //AddFeaturesParameter<List<Feature<Point, MobileCompartment<int?>>>> query =
        //    new(compartmentDetailsList
        //        .Select(cd => 
        //            new Feature<Point, MobileCompartment<int?>>(
        //                cd.ShapeGeometry == null 
        //                    ? null
        //                    : cd.ShapeGeometry.GetCenterPoint(), 
        //                new()
        //{
        //    ObjectId = null,
        //    CaseReference = caseRef,
        //    CreatedBy = "Flow-V2",
        //    WoodlandName = cd.WoodlandName,
        //    PropertyName = cd.CompartmentNumber + "-" + cd.SubCompartmentNo,
        //})).ToList());
        //var path = $"{layer.Value.ServiceURI}/addFeatures";
        //var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(query, path, layer.Value.NeedsToken, cancellationToken);

        //if (result.IsFailure)
        //{
        //    return Result.Failure(result.Error);
        //}

        //return (result.Value.AddResults == null || !result.Value.AddResults!.Any()) 
        //    ? Result.Failure("No Results found")
        //    : Result.Success();
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

