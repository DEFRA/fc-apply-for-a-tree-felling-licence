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
        var path = $"{_landInformationSearchOptions.BaseUrl}{_landInformationSearchOptions.FeaturePath}/addFeatures";

        _logger.LogDebug("About to send geometries for felling licence having id of [{id}] to feature service at [{featurePath}]"
            , fellingLicenceId, path);

        var geometryResult = ShapeHelper.MakeMultiPart(compartments.Select(c => c.ShapeGeometry).ToList());


        if (geometryResult.IsFailure)
        {
            _logger.LogDebug("Unable to convert compartment geometries to MultiPart Polygon - for felling licence having id of [{id}] to feature service at [{featurePath}], error is [{error}]."
                , fellingLicenceId, path, geometryResult.Error);

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

        _logger.LogDebug("Serialized data as: [{compartmentData}]", compartmentData);

        var data = new EditFeaturesParameter(compartmentData);

        var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(data, path, true, cancellationToken);


        if (result.IsFailure)
        {
            return result;
        }

        if (result.Value?.AddResults == null || result.Value.AddResults!.Count == 0 || result.Value.AddResults!.Count((ar) => ar.WasSuccessful == false) != 0)
        {
            return Result.Failure<CreateUpdateDeleteResponse<int>>("Unable to add compartment geometries");
        }

        return Result.Success(result.Value);
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
        var path = $"{_landInformationSearchOptions.BaseUrl}{_landInformationSearchOptions.FeaturePath}/query";

        _logger.LogDebug("About to query case [{id}] to feature service at [{featurePath}]"
         , fellingLicenceId, path);

        var query = new QueryFeatureServiceParameters()
        {
            WhereString = $"Case_Reference = '{fellingLicenceId}'",
            ReturnGeometry = false,
            OutFields = { "OBJECTID" }
        };

        var result = await PostQueryWithConversionAsync<BaseQueryResponse<BaseAttribute<int>>>(query, path, true, cancellationToken);

        return result.IsFailure ? result.ConvertFailure<List<int>>() :
            Result.Success(!result.Value.Results.Any() ? [] :
                result.Value.Results.Select(r => r.Record.ObjectId).ToList());
    }

    /// <inheritdoc />
    public async Task<Result> ClearLayerAsync(
        string fellingLicenceId,
        CancellationToken cancellationToken)
    {
        var items = await GetCompartmentIdsAsync(fellingLicenceId, cancellationToken);
        if (items.IsFailure)
        {
            return items.ConvertFailure();
        }

        if (items.Value.Count == 0)
        {
            return Result.Success();
        }

        var path = $"{_landInformationSearchOptions.BaseUrl}{_landInformationSearchOptions.FeaturePath}/deleteFeatures";
        _logger.LogDebug("About to delete case [{id}] to feature service at [{featurePath}]"
         , fellingLicenceId, path);

        var data = new DeleteFeatureByObjectId<int>(items.Value.Select(i => i).ToList());

        var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<BaseCreateDeleteResult<int>>>(data, path, true, cancellationToken);

        if (result.IsFailure || result.Value == null || result.Value.WasSuccess != true)
        {
            return Result.Failure("Invalid Result from Esri");
        }

        return Result.Success();

    }
}