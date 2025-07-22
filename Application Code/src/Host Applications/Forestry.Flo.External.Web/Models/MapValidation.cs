using Newtonsoft.Json;

namespace Forestry.Flo.External.Web.Models
{
    public class MapValidation
    {
        [JsonProperty("isValid")]
        public bool IsValid { get; set; }

        [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
        public string? Message { get; set; } = null;
    }
}
