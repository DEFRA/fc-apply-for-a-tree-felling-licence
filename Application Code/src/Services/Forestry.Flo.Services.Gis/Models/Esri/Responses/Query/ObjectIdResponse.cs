using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Query
{
    public class ObjectIdResponse<T>
    {
        [JsonProperty("objectid", NullValueHandling = NullValueHandling.Ignore)]
        public T ObjectID { get; set; } = default(T)!;
    }
}
