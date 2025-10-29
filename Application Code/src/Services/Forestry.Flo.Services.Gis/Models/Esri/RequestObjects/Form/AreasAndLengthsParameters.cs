using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;

public class AreasAndLengthsParameters : CommonParameters
{
    public Polygon Compartment { get; set; } = new Polygon();

    public int SpatialReference { get; set; }

    public int LengthUnit { get; set; } = 9035;

    public string AreaUnit { get; set; } = "esriAcres";

    public string CalculationType { get; set; } = "preserveShape";
    public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
    {
        var data = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("polygons", "[" + Compartment.GetGeometrySimple() +"]"),
                new KeyValuePair<string, string>("lengthUnit", LengthUnit.ToString()),
                new KeyValuePair<string, string>("areaUnit", "{\"areaUnit\":\"" +AreaUnit +"\"}"),
                new KeyValuePair<string, string>("calculationType", CalculationType),
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
