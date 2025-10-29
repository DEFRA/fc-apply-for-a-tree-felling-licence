using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;

public class IntersectParameter : CommonParameters
{
    public List<Polygon> LayerShapes { get; set; } = null!;

    public Polygon Compartment { get; set; } = new Polygon();

    public int SpatialReference { get; set; }

    public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
    {
        var data = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("geometries", "{\"geometryType\":\"esriGeometryPolygon\",\"geometries\":[" + string.Join(", ", LayerShapes.Select(l => l.GetGeometrySimple())) +"]}"),
                new KeyValuePair<string, string>("geometry", "{\"geometryType\":\"esriGeometryPolygon\",\"geometries\":" + Compartment.GetGeometrySimple() +"}"),
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
