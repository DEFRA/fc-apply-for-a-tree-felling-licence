using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.LandInformationSearch;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Services;

public class LandInformationSearch : BaseServices, ILandInformationSearch
{
    private readonly LandInformationSearchOptions _landInformationSearchOptions;
    private readonly ILogger<LandInformationSearch> _logger;

    public LandInformationSearch(
        IOptions<LandInformationSearchOptions> landInformationSearchOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<LandInformationSearch> logger)
        : base(httpClientFactory, "LandInformationSearch", logger)
    {
        _landInformationSearchOptions = Guard.Against.Null(landInformationSearchOptions.Value);
        _logger = logger;
        TokenRequest = new GetTokenParameters(_landInformationSearchOptions.ClientId, _landInformationSearchOptions.ClientSecret, true);
        GetTokenPath = $"{_landInformationSearchOptions.TokenUrl}{_landInformationSearchOptions.TokenPath}";
    }

    ///<inheritdoc/>
    public async Task<Result<CreateUpdateDeleteResponse<int>>> AddFellingLicenceGeometriesAsync(
        Guid fellingLicenceId,
        IReadOnlyList<InternalCompartmentDetails<Polygon>> compartments,
        CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = Guid.NewGuid(),
                   ["FellingLicenceId"] = fellingLicenceId
               }))
        {
            _logger.LogInformation("AddFellingLicenceGeometriesAsync called for application: {ApplicationId}", fellingLicenceId);

            var path = $"{_landInformationSearchOptions.BaseUrl}{_landInformationSearchOptions.FeaturePath}/addFeatures";

            _logger.LogDebug("About to send geometries for application [{ApplicationId}] to feature service at [{FeaturePath}]",
                fellingLicenceId, path);

            var geometryResult = ShapeHelper.MakeMultiPart(compartments.Select(c => c.ShapeGeometry).ToList());

            if (geometryResult.IsFailure)
            {
                _logger.LogError("Unable to convert compartment geometries to MultiPart Polygon for application {ApplicationId}: {Error}",
                    fellingLicenceId, geometryResult.Error);

                return Result.Failure<CreateUpdateDeleteResponse<int>>(geometryResult.Error);
            }

            var model = new BaseFeatureWithGeometryObject<Polygon, LandInformationSearchCompartmentAttributeModel<int>>
            {
                GeometryObject = geometryResult.Value,
                Attributes = new LandInformationSearchCompartmentAttributeModel<int>
                {
                    CaseReference = fellingLicenceId.ToString()
                }
            };

            var compartmentData = JsonConvert.SerializeObject(model);

            _logger.LogDebug("Serialized data as: [{CompartmentData}]", compartmentData);

            var data = new EditFeaturesParameter(compartmentData);

            var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(data, path, true, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to add felling licence geometries for application {ApplicationId}: {Error}", fellingLicenceId, result.Error);
                return result;
            }

            if (result.Value?.AddResults == null || result.Value.AddResults.Count == 0 || result.Value.AddResults.Any(ar => !ar.WasSuccessful))
            {
                _logger.LogError("Unable to add compartment geometries for application {ApplicationId}", fellingLicenceId);
                return Result.Failure<CreateUpdateDeleteResponse<int>>("Unable to add compartment geometries");
            }

            _logger.LogInformation("Successfully added felling licence geometries for application {ApplicationId}", fellingLicenceId);
            return Result.Success(result.Value);
        }
    }

    /// <summary>
    /// Gets the compartment ids for a felling licence from the feature service
    /// </summary>
    /// <param name="fellingLicenceId">Unique identifier for the felling licence (case Id)</param>
    /// <param name="cancellationToken">The Cancellation Token</param>
    /// <returns>A Result with a list The Global Ids</returns>
    public async Task<Result<List<int>>> GetCompartmentIdsAsync(
        string fellingLicenceId,
        CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = Guid.NewGuid(),
                   ["FellingLicenceId"] = fellingLicenceId
               }))
        {
            _logger.LogInformation("GetCompartmentIdsAsync called for application: {ApplicationId}", fellingLicenceId);

            var path = $"{_landInformationSearchOptions.BaseUrl}{_landInformationSearchOptions.FeaturePath}/query";

            _logger.LogDebug("About to query application [{ApplicationId}] to feature service at [{FeaturePath}]",
                fellingLicenceId, path);

            var query = new QueryFeatureServiceParameters()
            {
                WhereString = $"Case_Reference = '{fellingLicenceId}'",
                ReturnGeometry = false,
                OutFields = { "OBJECTID" }
            };

            var result = await PostQueryWithConversionAsync<BaseQueryResponse<BaseAttribute<int>>>(query, path, true, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to get compartment ids for application {ApplicationId}: {Error}", fellingLicenceId, result.Error);
                return result.ConvertFailure<List<int>>();
            }

            var ids = !result.Value.Results.Any() ? new List<int>() : result.Value.Results.Select(r => r.Record.ObjectId).ToList();
            _logger.LogInformation("Retrieved {Count} compartment ids for application {ApplicationId}", ids.Count, fellingLicenceId);

            return Result.Success(ids);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ClearLayerAsync(
        string fellingLicenceId,
        CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = Guid.NewGuid(),
                   ["FellingLicenceId"] = fellingLicenceId
               }))
        {
            _logger.LogInformation("ClearLayerAsync called for application: {ApplicationId}", fellingLicenceId);

            var items = await GetCompartmentIdsAsync(fellingLicenceId, cancellationToken);
            if (items.IsFailure)
            {
                _logger.LogError("Failed to get compartment ids for clearing layer for application {ApplicationId}: {Error}", fellingLicenceId, items.Error);
                return items.ConvertFailure();
            }

            if (items.Value.Count == 0)
            {
                _logger.LogInformation("No compartment ids found for application {ApplicationId}, nothing to delete.", fellingLicenceId);
                return Result.Success();
            }

            var path = $"{_landInformationSearchOptions.BaseUrl}{_landInformationSearchOptions.FeaturePath}/deleteFeatures";
            _logger.LogDebug("About to delete application [{ApplicationId}] from feature service at [{FeaturePath}]",
                fellingLicenceId, path);

            var data = new DeleteFeatureByObjectId<int>(items.Value.Select(i => i).ToList());

            var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<BaseCreateDeleteResult<int>>>(data, path, true, cancellationToken);

            if (result.IsFailure || result.Value == null || result.Value.WasSuccess != true)
            {

                _logger.LogError("Failed to clear layer for application {ApplicationId}: Invalid Result from Esri", fellingLicenceId);
                return Result.Failure("Invalid Result from Esri");
            }

            _logger.LogInformation("Successfully cleared layer for application {ApplicationId}", fellingLicenceId);
            return Result.Success();
        }
    }
}