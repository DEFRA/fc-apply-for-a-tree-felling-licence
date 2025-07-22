using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers
{
    public  class WoodlandOfficer
    {
        [JsonProperty("fieldmanager", NullValueHandling = NullValueHandling.Ignore)]
        public string?  OfficerName { get; set; }

        [JsonProperty("area_code", NullValueHandling = NullValueHandling.Ignore)]
        public string? Code { get; set; }

    }
}
