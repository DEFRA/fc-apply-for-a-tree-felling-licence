using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json;

public record BaseAttribute<T>
{
    [JsonProperty("OBJECTID", NullValueHandling = NullValueHandling.Ignore)]
    public T? ObjectId { get; set; }
}
