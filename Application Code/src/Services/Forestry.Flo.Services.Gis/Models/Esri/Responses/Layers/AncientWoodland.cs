using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers
{
    public class AncientWoodland
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string? Status { get; set; }
    }
}
