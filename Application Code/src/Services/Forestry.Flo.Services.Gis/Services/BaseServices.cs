using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Forestry.Flo.Services.Gis.Services;

public class BaseServices
{
    private readonly ILogger _logger;
    private readonly HttpClient _client;

    protected int SpatialReference;
    public Maybe<EsriTokenResponse> Token { get; protected set; } = Maybe<EsriTokenResponse>.None;

    protected Maybe<GetTokenParameters> TokenRequest = Maybe<GetTokenParameters>.None;
    protected string GetTokenPath;
    protected List<FeatureLayerConfig> LayerSettings = [];
    protected GeometryServiceSettings GeometryService = null!;

    public BaseServices(IHttpClientFactory httpClientFactory, string clientName, ILogger logger,
        GetTokenParameters? getTokenParameters = null, string? path = null)
    {
        Guard.Against.Null(httpClientFactory);

        _logger = logger;
        _client = httpClientFactory.CreateClient(clientName);

        GetTokenPath = string.Empty;

        if (getTokenParameters != null) {
            TokenRequest = getTokenParameters;
        }

        if (path != null) {
            GetTokenPath = path;
        }
    }

    /// <summary>
    /// This Method Generates the token to use for all the requests.
    /// Note: The Land Register and AGOL will have different objects because they will 
    /// have different credentials
    /// </summary>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns>If the job was successful the actual token is not returned</returns>
    protected async Task<Result> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (TokenRequest.HasNoValue) {
            return Result.Failure("Token Service not set");
        }

        try {
            var response = await _client.PostAsync(GetTokenPath, TokenRequest.Value.ToFormUrlEncodedContentFormData(),
                cancellationToken);
            if (!response.IsSuccessStatusCode) {
                return Result.Failure("Unable to connect to the esri service");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (content.Contains("error")) {
                var error = JsonConvert.DeserializeObject<EsriErrorResponse<string>>(content);
                return Result.Failure(error!.Error!.Message);
            }

            var token = JsonConvert.DeserializeObject<EsriTokenResponse>(content);
            if (token != null && !String.IsNullOrEmpty(token.TokenString)) {
                Token = token;
                token.Expiry = DateTime.Now.AddMinutes(60);


                return Result.Success();
            }


            var message = $"Unable to read content:{content}";
            _logger.LogError(message);
            return Result.Failure(message);
        }
        catch (Exception ex) {
            _logger.LogError(ex.Message);
            return Result.Failure(ex.Message);
        }
    }


    /// <summary>
    /// Gets the Layer details based on the name passed in.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected Maybe<FeatureLayerConfig> GetLayerDetails(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) {
            return Maybe<FeatureLayerConfig>.None;
        }

        name = name.Trim();
        var item = LayerSettings.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

        return item == null
            ? Maybe<FeatureLayerConfig>.None
            : Maybe<FeatureLayerConfig>.From(item);
    }

    /// <summary>
    /// Gets the current token
    /// </summary>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns></returns>
    protected async Task<Result<Maybe<string>>> GetTokenString(CancellationToken cancellationToken)
    {
        if (Token.HasNoValue || DateTime.Now > Token.Value.Expiry) {
            var tokenRefresh = await GetTokenAsync(cancellationToken);
            if (tokenRefresh.IsFailure || Token.HasNoValue) {
                return Result.Failure<Maybe<string>>("Unable to log into server");
            }
        }

        return Result.Success(Maybe.From(Token.Value.TokenString!));
    }


    /// <summary>
    /// This is the method that actually makes the relevant call to the API
    /// </summary>
    /// <param name="query"></param>
    /// <param name="path"></param>
    /// <param name="needsToken"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Result<HttpResponseMessage>> PostQueryFromUrlEncodedAsync(BaseParameter query, string path, bool needsToken,
            CancellationToken cancellationToken)
    {
        if (needsToken) {
            var tokenCheck = await GetTokenString(cancellationToken);
            if (tokenCheck.IsFailure) {
                return tokenCheck.ConvertFailure<HttpResponseMessage>();
            }

            query.TokenString = tokenCheck.Value;
        }

        return Result.Success(await _client.PostAsync(path, query.ToFormUrlEncodedContentFormData(), cancellationToken));
    }


    protected static Maybe<string> CheckForEsriErrors(string content)
    {
        if (string.IsNullOrEmpty(content)) {
            return Maybe<string>.From("Empty response");
        }
        if (content == "{}") {
            return Maybe.From("Empty message from ESRI");
        }

        //Its possible that the system returns either a List<string> or just string.
        if (content.TryParseJson(out EsriErrorResponse<string> eError)) {
            return Maybe.From($"ESRI -> {eError!.Error!.Message}");
        }
        return content.TryParseJson(out EsriErrorResponse<List<string>> fallbackError) ?
            Maybe.From($"ESRI Error-> {fallbackError!.Error!.Message}: {String.Join(", ", fallbackError!.Error!.Details!)})")
            : Maybe<string>.None;
    }

    /// <summary>
    /// Post should be the default call! Esri supports Get via URL encoded Strings, but this can get really long 
    /// </summary>
    /// <typeparam name="T">The Esri Response Object to get</typeparam>
    /// <param name="query">The Query object to use against the Query service</param>
    /// <param name="path">The path to the service</param>
    /// <param name="needsToken">If the service needs a token to access</param>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns>The Result of the query</returns>
    protected async Task<Result<T>> PostQueryWithConversionAsync<T>(BaseParameter query, string path, bool needsToken, CancellationToken cancellationToken)
    {
        try {
            var postResult = await PostQueryAsync(query, path, needsToken, false, cancellationToken);

            if (postResult.IsFailure) {
                return postResult.ConvertFailure<T>();
            }

            _logger.LogDebug("Response data: [{Value}], from [{path}]", postResult.Value, query);

            var result = JsonConvert.DeserializeObject<T>(postResult.Value, new JavaScriptDateTimeConverter());
            if (result != null) {
                return Result.Success(result);
            }

            var message = $"Unable to read content:{postResult.Value}";
            _logger.LogError(message);
            return Result.Failure<T>(message);
        }
        catch (Exception ex) {
            _logger.LogError(ex.Message);
            return Result.Failure<T>(ex.Message);
        }
    }

    /// <summary>
    /// Post should be the default call! Esri supports Get via URL encoded Strings, but this can get really long 
    /// </summary>
    /// <param name="query">The Query object to use against the Query service</param>
    /// <param name="path">The path to the service</param>
    /// <param name="needsToken">If the service needs a token to access</param>
    /// <param name="htmlIsValid">Is Html a valid value to return</param>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns>The Result of the query</returns>
    protected async Task<Result<string>> PostQueryAsync(BaseParameter query, string path, bool needsToken, bool htmlIsValid, CancellationToken cancellationToken)
    {
        try {
            var postResult = await PostQueryFromUrlEncodedAsync(query, path, needsToken, cancellationToken);

            if (postResult.IsFailure) {
                return postResult.ConvertFailure<string>();
            }

            var response = postResult.Value;
            if (!response.IsSuccessStatusCode) {
                return Result.Failure<string>("Unable to connect to the esri service");
            }

            if (!htmlIsValid && response.Content.Headers.ContentType!.MediaType == "text/html") {
                return Result.Failure<string>("Message returned in html");
            }
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var maybeErrors = CheckForEsriErrors(content);
            if (maybeErrors.HasValue) {
                return Result.Failure<string>(maybeErrors.Value);
            }

            return Result.Success(content);
        }
        catch (Exception ex) {
            _logger.LogError(ex.Message);
            return Result.Failure<string>(ex.Message);
        }
    }

    /// <summary>
    /// Posts an operation to the ESRI services
    /// </summary>
    /// <param name="query">The query to use on the service</param>
    /// <param name="needsToken">If the Service needs a token to access</param>
    /// <param name="path">The path to the service</param>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns>The object that the query request</returns>
    protected async Task<Result<byte[]>> GetAttachmentAsync(CommonParameters query, string path, bool needsToken, CancellationToken cancellationToken)
    {
        try {
            var postResult = await PostQueryFromUrlEncodedAsync(query, path, needsToken, cancellationToken);

            if (postResult.IsFailure) {
                return postResult.ConvertFailure<byte[]>();
            }

            var response = postResult.Value;

            if (!response.IsSuccessStatusCode) {
                return Result.Failure<byte[]>("Unable to connect to the esri service");
            }


            if (!response.IsSuccessStatusCode) {
                return Result.Failure<byte[]>("Unable to connect to the esri service");
            }


            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            return Result.Success(content);

        }
        catch (Exception ex) {
            _logger.LogError(message: ex.Message);
            return Result.Failure<byte[]>(ex.Message);
        }
    }


    /// <summary>
    /// Posts a file to the ESI Services
    /// </summary>
    /// <param name="file">The file object to send</param>
    /// <param name="path">The path to the service</param>
    /// <param name="needsToken">If the service needs a token to access</param>
    /// <param name="parameters">The parameters to append to the query</param>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns>The success message from esri</returns>
    protected async Task<Result<string>> PostFileToEsriAsync(ByteArrayContent file, string path, GenerateParameters parameters, bool needsToken, CancellationToken cancellationToken)
    {
        try {
            if (needsToken) {
                if (Token.HasNoValue || DateTime.Now > Token.Value.Expiry) {
                    var tokenRefresh = await GetTokenAsync(cancellationToken);
                    if (tokenRefresh.IsFailure || Token.HasNoValue) {
                        return Result.Failure<string>("Unable to log into server");
                    }
                }

                if (Token.HasValue || Token.Value.TokenString != null) {
                    parameters.TokenString = Maybe<string>.From(Token.Value.TokenString!);
                }
            }


            path = $"{path}{parameters.GetQuery()}";

            var multipartForm = new MultipartFormDataContent
            {
                    { file, "file", parameters.PublishParameters!.FileName! }
                };

            var response = await _client.PostAsync(path, multipartForm, cancellationToken);
            if (!response.IsSuccessStatusCode) {
                return Result.Failure<string>("Unable to connect to the esri service");
            }


            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var maybeErrors = CheckForEsriErrors(content);
            return maybeErrors.HasValue ?
                Result.Failure<string>(maybeErrors.Value)
                : Result.Success(content);
        }
        catch (Exception ex) {
            _logger.LogError(message: ex.Message);
            return Result.Failure<string>(ex.Message);
        }
    }


    /// <summary>
    /// Gets the IDs of the items in the layer, that match the lookup
    /// </summary>
    /// <typeparam name="T">The type value of the PK field (ObjectID)</typeparam>
    /// <param name="fieldValue">The value to look for in the field</param>
    /// <param name="fieldName">The Field name to use in the lookup</param>
    /// <param name="pathToLayer">The Layer URL</param>
    /// <param name="cancellationToken">The Cancellation token</param>
    /// <returns>The ID's of all the items that matched the lookup</returns>
    protected async Task<Result<List<T>>> GetEsriIDs_ByFieldAsync<T>(string fieldValue, string fieldName, string pathToLayer, CancellationToken cancellationToken)
    {
        var path = $"{pathToLayer}/query";

        var query = new QueryFeatureServiceParameters {
            OutFields = ["objectID"],
            WhereString = $"{fieldName} = '{fieldValue}'"
        };

        var result = await PostQueryWithConversionAsync<BaseQueryResponse<ObjectIdResponse<T>>>(query, path, true, cancellationToken);

        return result.IsFailure ? Result.Failure<List<T>>(result.Error) : Result.Success(result.Value.Results.Select(f => f.Record.ObjectID).ToList());
    }

    /// <summary>
    /// Unions the separate Polygons into one Multipart polygon.
    /// </summary>
    /// <param name="compartments">The compartments to join in to a multipart Polygon</param>
    /// <param name="baseUrl">THe base url to use</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <returns>The merged shape</returns>
    public async Task<Result<Geometry<Polygon>>> UnionPolygonsAsync(List<string> compartments, string baseUrl, CancellationToken cancellationToken)
    {
        Guard.Against.Null(GeometryService);
        Guard.Against.Null(GeometryService.UnionService);
        Guard.Against.Zero(SpatialReference);

        List<Polygon> list = [];
        foreach (var item in compartments) {
            if (!item.Contains("rings", StringComparison.CurrentCultureIgnoreCase)) {
                continue;
            }
            var workingItem = JsonConvert.DeserializeObject<Polygon>(item);

            if (workingItem == null) {
                continue;
            }
            if (workingItem.SpatialSettings!.ID != SpatialReference) {
                continue;
            }
            list.Add(workingItem);
        }

        var unionParameters = new UnionParameter {
            Shapes = list.Select(l => l.GetGeometrySimple()).ToList(),
            SpatialReference = SpatialReference
        };

        var path = GeometryService.IsPublic
              ? $"{GeometryService.Path}{GeometryService.UnionService.Path}"
              : $"{baseUrl}{GeometryService.Path}/{GeometryService.UnionService.Path}";

        return await PostQueryWithConversionAsync<Geometry<Polygon>>(unionParameters, path, GeometryService.NeedsToken, cancellationToken);
    }

    /// <summary>
    /// Converts the 27700 projected point into a Lat Long
    /// </summary>
    /// <param name="point">The point to convert</param>
    /// <param name="baseUrl">The base url to use</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <returns></returns>
    protected async Task<Result<LatLongObj>> ConvertPointToLatLongAsync(Point point,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(GeometryService);
        Guard.Against.Null(GeometryService.ProjectService);
        Guard.Against.Zero(SpatialReference);

        ProjectParameters<Point> parameters = new() {
            OutSR = GeometryService.ProjectService.OutSR,
            InSR = SpatialReference,
            Shapes = new Geometries<Point>() {
                GeometryType = "esriGeometryPoint",
                Shapes = [point]
            }
        };

        var path = GeometryService.IsPublic
            ? $"{GeometryService.Path}{GeometryService.ProjectService.Path}"
            : $"{baseUrl}{GeometryService.Path}/{GeometryService.ProjectService.Path}";

        var result = await PostQueryWithConversionAsync<ProjectionResponse<LatLongObj>>(parameters, path, GeometryService.NeedsToken, cancellationToken);
        if (result.IsFailure) {
            return result.ConvertFailure<LatLongObj>();
        }

        return result.Value.Geometries.Count == 0 ?
            Result.Failure<LatLongObj>("Array was empty")
            : Result.Success(result.Value.Geometries[0]);
    }

    /// <summary>
    /// Gets the Centre point of a collections of Polygons
    /// </summary>
    /// <param name="compartments">The Compartments to use</param>
    /// <param name="url">The URL to use</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The centre point of the map</returns>
    public async Task<Result<Point>> CalculateCentrePointAsync(List<string> compartments, string url, CancellationToken cancellationToken)
    {
        var mergeResult = await UnionPolygonsAsync(compartments, url, cancellationToken);
        return mergeResult.IsFailure ? Result.Failure<Point>(mergeResult.Error) : Result.Success(mergeResult.Value.Shape.GetCenterPoint()!);
    }


    /// <summary>
    /// Gets the image generated image from the ESRI Servers. As this is a batch job you will have to wait for it to complete.
    /// To do this in code we loop 3 times on the wait time
    /// </summary>
    /// <param name="url">The URL to Collect the image from</param>
    /// <param name="waitTime">How long to wait between tries</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The Result of the action</returns>
    protected async Task<Result<Stream>> GetEsriGeneratedImageAsync(string url, int waitTime, CancellationToken cancellationToken)
    {
        try {
            for (var i = 0; i < 3; i++) {
                Thread.Sleep(waitTime);
                var response = await _client.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode) {
                    return Result.Success(await response.Content.ReadAsStreamAsync(cancellationToken));
                }
            }
            return Result.Failure<Stream>("Unable to read File");
        }
        catch (Exception ex) {
            return Result.Failure<Stream>(ex.Message);
        }
    }

    /// <summary>
    /// Calculates the buffer for the point
    /// </summary>
    /// <param name="min">The smallest point</param>
    /// <param name="max">The largest point</param>
    /// <returns>The buffer to add to the image</returns>
    protected static float CalculateBuffer(float min, float max)
    {
        const float bufferPercentage = 0.05f;
        return (max - min) * bufferPercentage;
    }

    protected LayerDefinitionDetails GetLayerDefinition(string shapeType)
    {
        DrawinginfoDetails? info = null;
        LayerDefinitionDetails layer;
        switch (shapeType) {
            case "esriGeometryPolyline":
                info = new();
                info.RenderSettings.RenderType = "simple";
                info.RenderSettings.SymbolSettings.SymbolType = "esriSLS";
                info.RenderSettings.SymbolSettings.SymbolColour = new() { 0, 0, 0, 255 };
                info.RenderSettings.SymbolSettings.Width = 2;
                info.RenderSettings.SymbolSettings.SymbolStyle = "";
                info.RenderSettings.SymbolSettings.Cap = "butt";
                layer = new(shapeType, info);
                break;
            case "esriGeometryPoint": {
                    info = new();
                    info.RenderSettings.RenderType = "simple";
                    info.RenderSettings.SymbolSettings.SymbolType = "esriSMS";
                    info.RenderSettings.SymbolSettings.SymbolColour = new() { 34, 139, 34, 179 };
                    info.RenderSettings.SymbolSettings.Angle = 0;
                    info.RenderSettings.SymbolSettings.XOffset = 0;
                    info.RenderSettings.SymbolSettings.YOffset = 0;
                    info.RenderSettings.SymbolSettings.Size = 14;
                    info.RenderSettings.SymbolSettings.SymbolStyle = "esriSMSCircle";
                    if (info.RenderSettings.SymbolSettings.OutLineSettings != null) {
                        info.RenderSettings.SymbolSettings.OutLineSettings.OutlineType = "esriSLS";
                        info.RenderSettings.SymbolSettings.OutLineSettings.OutlineColour = new() { 0, 0, 0, 255 };
                        info.RenderSettings.SymbolSettings.OutLineSettings.OutlineWidth = 1;
                        info.RenderSettings.SymbolSettings.OutLineSettings.OutlineStyle = "esriSLSDash";
                    }
                    layer = new(shapeType, info);
                    break;
                }
            case "text":
                layer = new("esriGeometryPoint", info, shapeType);
                break;
            default:
                info = new();
                layer = new(shapeType, info);
                break;
        }


        return layer;
    }
}
