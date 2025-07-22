using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers
{
    public class AdminBoundary
    {

        [JsonProperty("fieldmanager", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        [JsonProperty("area_code", NullValueHandling = NullValueHandling.Ignore)]
        public string? Code { get; set; }
    }
}
