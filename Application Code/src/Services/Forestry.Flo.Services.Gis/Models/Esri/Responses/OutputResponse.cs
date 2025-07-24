using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses;
public class OutputResponse
{
    /// <summary>
    /// The name of the output
    /// </summary>
    [JsonProperty("paramName")]
    public string Name { get; set; }

    /// <summary>
    /// The data type of the output
    /// </summary>
    [JsonProperty("dataType")]
    public string DateType { get; set; }

    /// <summary>
    /// The value of the output
    /// </summary>
    [JsonProperty("value")]
    public UrlDetails Value { get; set; }
}


public class UrlDetails
{
    /// <summary>
    /// The URL of the file
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; } = null!;
}



