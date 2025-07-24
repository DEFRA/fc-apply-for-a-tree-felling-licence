using Forestry.Flo.Services.Gis.Models.Esri.Common;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    public class UnionParameter: CommonParameters
    {
        public List<string> Shapes { get; set; } = null!;

        public int SpatialReference { get; set; }

        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {
            var data = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("geometries", "{\"geometryType\":\"esriGeometryPolygon\",\"geometries\":[" + string.Join(", ", Shapes) +"]}"),
                new KeyValuePair<string, string>("sr", SpatialReference.ToString()),
                new KeyValuePair<string, string>("f", RequestFormat)
            };
            if (TokenString.HasValue)
            {
                data.Add(new KeyValuePair<string, string>("token", TokenString.Value));
            }
            return new FormUrlEncodedContent(data.ToArray());
        }
    }
}
