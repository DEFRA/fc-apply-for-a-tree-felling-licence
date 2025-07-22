using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Query
{
    public class ObjectIdResponse<T>
    {
        [JsonProperty("objectid")]
        public T ObjectID { get; set; } = default(T)!;
    }
}
