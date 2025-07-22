using Forestry.Flo.Services.Gis.Models.Esri.Responses.Common;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses;

/// <summary>
/// The response that comes back from either Creating, Updating or deleting
/// </summary>
/// <typeparam name="T"></typeparam>
public record CreateUpdateDeleteResponse<T>
{
    /// <summary>
    /// A list of the results
    /// </summary>
    [JsonProperty("addResults", NullValueHandling = NullValueHandling.Ignore)]
    public List<BaseCreateDeleteResult<T>>? AddResults { get; set; }

    /// <summary>
    /// A list of the results
    /// </summary>
    [JsonProperty("deleteResults", NullValueHandling = NullValueHandling.Ignore)]
    public List<BaseCreateDeleteResult<T>>? DeleteResults { get; set; }

    /// <summary>
    /// A list of the results
    /// </summary>
    [JsonProperty("updateResults", NullValueHandling = NullValueHandling.Ignore)]
    public List<BaseCreateDeleteResult<T>>? UpdateResults { get; set; }

    [JsonProperty("success", NullValueHandling = NullValueHandling.Ignore)]
    public bool? WasSuccess { get; set; }
}

/// <summary>
/// The result of the create, update or delete
/// </summary>
/// <typeparam name="T">The data type of the ID</typeparam>
public record BaseCreateDeleteResult<T>
{
    /// <summary>
    /// The Id of the object. If it failed then the ID is null
    /// </summary>
    [JsonProperty("objectId", NullValueHandling = NullValueHandling.Ignore)]
    public T? ObjectId { get; set; }

    [JsonProperty("uniqueId", NullValueHandling = NullValueHandling.Ignore)]
    public int? RowId { get; set; }

    /// <summary>
    /// The Id of the object subjected to the action
    /// </summary>
    [JsonProperty("globalId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? GlobalId { get; set; }

    /// <summary>
    /// If it was successful
    /// </summary>
    [JsonProperty("success")]
    public bool WasSuccessful { get; set; }

    /// <summary>
    /// The Error details of the result. Will be null if successful
    /// </summary>
    [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
    public EsriError<string>? ErrorDetails { get; set; }
}

public record WasSuccessful
{
    /// <summary>
    /// If it was successful
    /// </summary>
    [JsonProperty("success")]
    public bool IsSuccessful { get; set; }
    /// <summary>
    /// The Error details of the result. Will be null if successful
    /// </summary>
    [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
    public EsriError<string>? ErrorDetails { get; set; }
}