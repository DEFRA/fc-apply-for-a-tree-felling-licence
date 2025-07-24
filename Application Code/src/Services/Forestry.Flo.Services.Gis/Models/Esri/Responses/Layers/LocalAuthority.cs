using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers
{
    public class LocalAuthority
    {
        [JsonProperty("area_code", NullValueHandling = NullValueHandling.Ignore)] 
        public string? Code { get; set; }

        [JsonProperty("admin_hub")] 
        public string Name { get; set; } = null!;
    }
}
