using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;

public class PhytophthoraRamorumRiskZone
{
    /// <summary>
    /// The name of the Zone
    /// </summary>
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? ZoneName { get; set; }
}
