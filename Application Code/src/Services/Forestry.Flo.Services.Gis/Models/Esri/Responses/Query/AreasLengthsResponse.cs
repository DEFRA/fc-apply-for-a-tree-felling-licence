using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;

public class AreasLengthsResponse
{
    [JsonProperty("areas")] 
    public List<double> Areas { get; set; } = [];
    
    [JsonProperty("lengths")]
    public List<double> Lengths { get; set; } = [];
}
