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

        if (getTokenParameters != null)
        {
            TokenRequest = getTokenParameters;
        }

        if (path != null)
        {
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
        _logger.LogInformation("GetTokenAsync called. TokenPath: {TokenPath}", GetTokenPath);

        if (TokenRequest.HasNoValue)
        {
            _logger.LogWarning("Token Service not set");
            return Result.Failure("Token Service not set");
        }

        try
        {
            var response = await _client.PostAsync(GetTokenPath, TokenRequest.Value.ToFormUrlEncodedContentFormData(),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GIS token request failed with response status code {ResponseCode}", response.StatusCode);
                return Result.Failure("Unable to connect to the esri service");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (content.Contains("error"))
            {
                var error = JsonConvert.DeserializeObject<EsriErrorResponse<string>>(content);
                _logger.LogError("GIS token request returned error {Error}", error?.Error?.Message);
                return Result.Failure(error!.Error!.Message);
            }

            var token = JsonConvert.DeserializeObject<EsriTokenResponse>(content);
            if (token != null && !String.IsNullOrEmpty(token.TokenString))
            {
                Token = token;
                token.Expiry = DateTime.Now.AddMinutes(60);
                _logger.LogInformation("Token successfully retrieved and set.");
                return Result.Success();
            }

            var message = $"Unable to deserialize content as ESRI token response: {content}";
            _logger.LogError("Unable to deserialize content as ESRI token response: {Content}", content);
            return Result.Failure(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetTokenAsync");
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
        _logger.LogInformation("GetLayerDetails called with name: {Name}", name);

        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Cannot get layer configuration with no name");
            return Maybe<FeatureLayerConfig>.None;
        }

        var item = LayerSettings.FirstOrDefault(x =>
            x.Name.Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase));

        if (item == null)
        {
            _logger.LogWarning("No layer configuration found for name: {Name}", name);
        }
        else
        {
            _logger.LogInformation("Layer configuration found for name: {Name}", name);
        }

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
        _logger.LogInformation("Requesting ESRI token from: {Path} | Parameters: {@Parameters}", GetTokenPath, TokenRequest);

        if (Token.HasNoValue || DateTime.Now > Token.Value.Expiry)
        {
            var tokenRefresh = await GetTokenAsync(cancellationToken);
            if (tokenRefresh.IsFailure || Token.HasNoValue)
            {
                _logger.LogError("Unable to log into server for token.");
                return Result.Failure<Maybe<string>>("Unable to log into server");
            }
        }

        _logger.LogInformation("Token string successfully retrieved.");
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
        _logger.LogInformation("Posting query to ESRI: {Path} | NeedsToken: {NeedsToken} | Parameters: {@Parameters}", path, needsToken, query);

        if (needsToken)
        {
            var tokenCheck = await GetTokenString(cancellationToken);
            if (tokenCheck.IsFailure)
            {
                _logger.LogError("Failed to get token for query.");
                return tokenCheck.ConvertFailure<HttpResponseMessage>();
            }

            query.TokenString = tokenCheck.Value;
        }

        var response = await _client.PostAsync(path, query.ToFormUrlEncodedContentFormData(), cancellationToken);
        _logger.LogInformation("Received response from ESRI: {Path} | StatusCode: {StatusCode}", path, response.StatusCode);
        return Result.Success(response);
    }

    protected static Maybe<string> CheckForEsriErrors(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return Maybe<string>.From("Empty response");
        }
        if (content == "{}")
        {
            return Maybe.From("Empty message from ESRI");
        }

        if (content.TryParseJson(out EsriErrorResponse<string> eError))
        {
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
        _logger.LogInformation("PostQueryWithConversionAsync called for type {Type} at path {Path}", typeof(T).Name, path);

        try
        {
            var postResult = await PostQueryAsync(query, path, needsToken, false, cancellationToken);

            if (postResult.IsFailure)
            {
                _logger.LogError("PostQueryAsync failed: {Error}", postResult.Error);
                return postResult.ConvertFailure<T>();
            }

            _logger.LogDebug("ESRI raw response: {RawResponse}", postResult.Value);

            var result = JsonConvert.DeserializeObject<T>(postResult.Value, new JavaScriptDateTimeConverter());
            if (result != null)
            {
                _logger.LogInformation("Successfully deserialized ESRI response to {Type}", typeof(T).Name);
                return Result.Success(result);
            }

            var message = $"ESRI: Unable to deserialise content as {typeof(T)} : {postResult.Value}";
            _logger.LogError("ESRI: Unable to deserialise content as {TypeName} : {Content}", typeof(T), postResult.Value);
            return Result.Failure<T>(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in PostQueryWithConversionAsync");
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["Path"] = path
        }))
        {
            _logger.LogInformation("PostQueryAsync called at path {Path}", path);

            try
            {
                var postResult = await PostQueryFromUrlEncodedAsync(query, path, needsToken, cancellationToken);

                if (postResult.IsFailure)
                {
                    _logger.LogError("PostQueryFromUrlEncodedAsync failed: {Error}", postResult.Error);
                    return postResult.ConvertFailure<string>();
                }

                _logger.LogDebug("ESRI Response data: [{Value}], from [{path}]", postResult.Value, path);

                var response = postResult.Value;
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ESRI responded with unsuccessful status code: {ResponseCode}", response.StatusCode);
                    return Result.Failure<string>("Unable to connect to the esri service");
                }

                if (!htmlIsValid && response.Content.Headers.ContentType!.MediaType == "text/html")
                {
                    _logger.LogWarning("ESRI returned HTML content when not expected");
                    return Result.Failure<string>("Message returned in html");
                }
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var maybeErrors = CheckForEsriErrors(content);
                if (maybeErrors.HasValue)
                {
                    _logger.LogError("ESRI returned error: {Error}", maybeErrors.Value);
                    return Result.Failure<string>(maybeErrors.Value);
                }

                _logger.LogInformation("PostQueryAsync succeeded at path {Path}", path);
                return Result.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in PostQueryAsync");
                return Result.Failure<string>(ex.Message);
            }
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["Path"] = path
        }))
        {
            _logger.LogInformation("GetAttachmentAsync called at path {Path}", path);

            try
            {
                var postResult = await PostQueryFromUrlEncodedAsync(query, path, needsToken, cancellationToken);

                if (postResult.IsFailure)
                {
                    _logger.LogError("PostQueryFromUrlEncodedAsync failed: {Error}", postResult.Error);
                    return postResult.ConvertFailure<byte[]>();
                }

                _logger.LogDebug("ESRI Response data: [{Value}], from [{path}]", postResult.Value, path);

                var response = postResult.Value;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ESRI responded with unsuccessful status code: {ResponseCode}", response.StatusCode);
                    return Result.Failure<byte[]>("Unable to connect to the esri service");
                }

                var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                _logger.LogInformation("Attachment successfully retrieved from ESRI.");
                return Result.Success(content);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetAttachmentAsync");
                return Result.Failure<byte[]>(ex.Message);
            }
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["Path"] = path,
            ["FileName"] = parameters.PublishParameters?.FileName
        }))
        {
            _logger.LogInformation("Uploading file to ESRI: {Path} | FileName: {FileName} | Parameters: {@Parameters}", path, parameters.PublishParameters?.FileName, parameters);
            try
            {
                if (needsToken)
                {
                    if (Token.HasNoValue || DateTime.Now > Token.Value.Expiry)
                    {
                        var tokenRefresh = await GetTokenAsync(cancellationToken);
                        if (tokenRefresh.IsFailure || Token.HasNoValue)
                        {
                            _logger.LogError("Unable to log into server for file upload.");
                            return Result.Failure<string>("Unable to log into server");
                        }
                    }

                    if (Token.HasValue || Token.Value.TokenString != null)
                    {
                        parameters.TokenString = Maybe<string>.From(Token.Value.TokenString!);
                    }
                }

                _logger.LogDebug("Posting file to ESRI: {Path}", path);

                path = $"{path}{parameters.GetQuery()}";

                var multipartForm = new MultipartFormDataContent
                {
                        { file, "file", parameters.PublishParameters!.FileName! }
                    };

                var response = await _client.PostAsync(path, multipartForm, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ESRI responded with unsuccessful status code: {ResponseCode}", response.StatusCode);
                    return Result.Failure<string>("Unable to connect to the esri service");
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var maybeErrors = CheckForEsriErrors(content);
                if (maybeErrors.HasValue)
                {
                    _logger.LogError("ESRI returned error: {Error}", maybeErrors.Value);
                }
                else
                {
                    _logger.LogInformation("File uploaded to ESRI successfully.");
                }

                return maybeErrors.HasValue
                    ? Result.Failure<string>(maybeErrors.Value)
                    : Result.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in PostFileToEsriAsync");
                return Result.Failure<string>(ex.Message);
            }
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["FieldName"] = fieldName,
            ["FieldValue"] = fieldValue,
            ["PathToLayer"] = pathToLayer
        }))
        {
            _logger.LogInformation("GetEsriIDs_ByFieldAsync called for field {FieldName} with value {FieldValue} at {PathToLayer}", fieldName, fieldValue, pathToLayer);

            var path = $"{pathToLayer}/query";

            var query = new QueryFeatureServiceParameters
            {
                OutFields = ["objectID"],
                WhereString = $"{fieldName} = '{fieldValue}'"
            };

            var result = await PostQueryWithConversionAsync<BaseQueryResponse<ObjectIdResponse<T>>>(query, path, true, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to get ESRI IDs by field: {Error}", result.Error);
                return Result.Failure<List<T>>(result.Error);
            }

            var ids = result.Value.Results.Select(f => f.Record.ObjectID).ToList();
            _logger.LogInformation("Successfully retrieved {Count} IDs from ESRI.", ids.Count);

            return Result.Success(ids);
        }
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["CompartmentCount"] = compartments?.Count ?? 0
        }))
        {
            _logger.LogInformation("UnionPolygonsAsync called with {Count} compartments.", compartments?.Count ?? 0);

            Guard.Against.Null(GeometryService);
            Guard.Against.Null(GeometryService.UnionService);
            Guard.Against.Zero(SpatialReference);

            List<Polygon> list = [];
            foreach (var item in compartments)
            {
                if (!item.Contains("rings", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                var workingItem = JsonConvert.DeserializeObject<Polygon>(item);

                if (workingItem == null)
                {
                    continue;
                }
                if (workingItem.SpatialSettings!.ID != SpatialReference)
                {
                    continue;
                }
                list.Add(workingItem);
            }

            var unionParameters = new UnionParameter
            {
                Shapes = list.Select(l => l.GetGeometrySimple()).ToList(),
                SpatialReference = SpatialReference
            };

            var path = GeometryService.IsPublic
                  ? $"{GeometryService.Path}{GeometryService.UnionService.Path}"
                  : $"{baseUrl}{GeometryService.Path}/{GeometryService.UnionService.Path}";

            var result = await PostQueryWithConversionAsync<Geometry<Polygon>>(unionParameters, path, GeometryService.NeedsToken, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to union polygons: {Error}", result.Error);
            }
            else
            {
                _logger.LogInformation("Polygons unioned successfully.");
            }

            return result;
        }
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
        _logger.LogInformation("ConvertPointToLatLongAsync called with point: {@Point}", point);

        Guard.Against.Null(GeometryService);
        Guard.Against.Null(GeometryService.ProjectService);
        Guard.Against.Zero(SpatialReference);

        ProjectParameters<Point> parameters = new()
        {
            OutSR = GeometryService.ProjectService.OutSR,
            InSR = SpatialReference,
            Shapes = new Geometries<Point>()
            {
                GeometryType = "esriGeometryPoint",
                Shapes = [point]
            }
        };

        var path = GeometryService.IsPublic
            ? $"{GeometryService.Path}{GeometryService.ProjectService.Path}"
            : $"{baseUrl}{GeometryService.Path}/{GeometryService.ProjectService.Path}";

        var result = await PostQueryWithConversionAsync<ProjectionResponse<LatLongObj>>(parameters, path, GeometryService.NeedsToken, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError("Failed to convert point to lat/long: {Error}", result.Error);
            return result.ConvertFailure<LatLongObj>();
        }

        if (result.Value.Geometries.Count == 0)
        {
            _logger.LogWarning("Projection response returned empty array.");
            return Result.Failure<LatLongObj>("Array was empty");
        }

        _logger.LogInformation("Point converted to lat/long successfully.");
        return Result.Success(result.Value.Geometries[0]);
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["CompartmentCount"] = compartments?.Count ?? 0
        }))
        {
            _logger.LogInformation("CalculateCentrePointAsync called with {Count} compartments.", compartments?.Count ?? 0);

            var mergeResult = await UnionPolygonsAsync(compartments, url, cancellationToken);
            if (mergeResult.IsFailure)
            {
                _logger.LogError("Failed to merge polygons for centre point: {Error}", mergeResult.Error);
                return Result.Failure<Point>(mergeResult.Error);
            }

            var center = mergeResult.Value.Shape.GetCenterPoint();
            if (center == null)
            {
                _logger.LogWarning("No center point could be calculated from merged polygons.");
                return Result.Failure<Point>("No center point found");
            }

            _logger.LogInformation("Centre point calculated successfully.");
            return Result.Success(center);
        }
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
        _logger.LogInformation("GetEsriGeneratedImageAsync called for url: {Url} with waitTime: {WaitTime}", url, waitTime);

        try
        {
            for (var i = 0; i < 3; i++)
            {
                Thread.Sleep(waitTime);
                var response = await _client.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Image stream successfully retrieved from ESRI.");
                    return Result.Success(await response.Content.ReadAsStreamAsync(cancellationToken));
                }
            }
            _logger.LogError("Unable to read file from ESRI after 3 attempts.");
            return Result.Failure<Stream>("Unable to read File");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetEsriGeneratedImageAsync");
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
        // No logging needed for this simple static calculation.
        const float bufferPercentage = 0.05f;
        return (max - min) * bufferPercentage;
    }

    protected LayerDefinitionDetails GetLayerDefinition(string shapeType)
    {
        _logger.LogInformation("GetLayerDefinition called with shapeType: {ShapeType}", shapeType);

        DrawinginfoDetails? info = null;
        LayerDefinitionDetails layer;
        switch (shapeType)
        {
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
            case "esriGeometryPoint":
                {
                    info = new();
                    info.RenderSettings.RenderType = "simple";
                    info.RenderSettings.SymbolSettings.SymbolType = "esriSMS";
                    info.RenderSettings.SymbolSettings.SymbolColour = new() { 34, 139, 34, 179 };
                    info.RenderSettings.SymbolSettings.Angle = 0;
                    info.RenderSettings.SymbolSettings.XOffset = 0;
                    info.RenderSettings.SymbolSettings.YOffset = 0;
                    info.RenderSettings.SymbolSettings.Size = 14;
                    info.RenderSettings.SymbolSettings.SymbolStyle = "esriSMSCircle";
                    if (info.RenderSettings.SymbolSettings.OutLineSettings != null)
                    {
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

        _logger.LogInformation("Layer definition created for shapeType: {ShapeType}", shapeType);
        return layer;
    }
}
