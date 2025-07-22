using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers
{
    public class CountryBoundary 
    {
        [JsonProperty("ctry23cd", NullValueHandling = NullValueHandling.Ignore)]
        public string? Code { get; set; }
    }
}
