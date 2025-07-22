using Forestry.Flo.Services.Gis.Models.Esri.Responses.Common;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Query
{

    public class BaseQueryResponse<T>
    {
        /// <summary>
        /// The Name of the objectID field
        /// </summary>
        [JsonProperty("objectIdFieldName")]
        public string ObjectIdFieldName { get; set; } = null!;

        /// <summary>
        /// The Name of the GlobalID field 
        /// </summary>
        [JsonProperty("globalIdFieldName")]
        public string GlobalIdFieldName { get; set; } = null!;

        /// <summary>
        /// A collection of Field Descriptions 
        /// </summary>
        [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
        public List<EsriFieldDescription> Fields { get; set; } = new List<EsriFieldDescription>();

        /// <summary>
        /// All the objects that match the query
        /// </summary>
        [JsonProperty("features")]
        public List<EsriResult<T>> Results { get; set; } = null!;
    }

    /// <summary>
    /// The record object of the results
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EsriResult<T>
    {
        [JsonProperty("attributes")]
        public T Record { get; set; } = default!;

        [JsonProperty("geometry", NullValueHandling = NullValueHandling.Ignore)]
        public GeometryPolygonResponse? Geometry { get; set; }

    }
}
