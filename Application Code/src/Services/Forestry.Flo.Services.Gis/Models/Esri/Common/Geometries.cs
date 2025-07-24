using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Common
{
    public class Geometries<T>
    {
        [JsonProperty("geometryType")]
        public string GeometryType { get; set; }

        [JsonProperty("geometries")]
        public List<T> Shapes { get; set; }

        public Geometries()
        {
            Shapes = new List<T>();
            GeometryType = "";
        }

        public Geometries(List<T> shapes, string geoType)
        {
            Shapes = shapes;
            GeometryType = geoType;
        }
    }
}
