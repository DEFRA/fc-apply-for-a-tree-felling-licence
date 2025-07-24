using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Common
{
    public class Geometry<T>
    {
        [JsonProperty("geometryType")]
        public string GeometryType { get; set; }

        [JsonProperty("geometry")]
        public T Shape { get; set; }

        public Geometry()
        {
            GeometryType = "";
        }

        public Geometry(T shape, string geoType)
        {
            Shape = shape;
            GeometryType = geoType;
        }
    }
}
