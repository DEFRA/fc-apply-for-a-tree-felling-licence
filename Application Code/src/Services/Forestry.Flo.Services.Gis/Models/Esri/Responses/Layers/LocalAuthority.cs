using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers
{
    public class LocalAuthority
    {
        [JsonProperty("name")] 
        public string Name { get; set; } = null!;
    }
}
