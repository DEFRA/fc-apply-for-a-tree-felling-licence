using Forestry.Flo.Services.Gis.Models.MapObjects;
using Newtonsoft.Json;


namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;


public class Extent
{
    [JsonProperty("spatialReference")]
    public SpatialReference Spatial_Reference { get; set; }

    [JsonProperty("xmin")]
    public float X_min { get; set; }

    [JsonProperty("ymin")]
    public float Y_min { get; set; }

    [JsonProperty("xmax")]
    public float X_max { get; set; }

    [JsonProperty("ymax")]
    public float Y_max { get; set; }

    public Extent()
    {
        Spatial_Reference = new(27700);
    }

    public Extent(float x_min, float y_min, float x_max, float y_max, int wkid, int? latestWKID = null)
    {
        Spatial_Reference = new(wkid, latestWKID);
        X_min = x_min;
        Y_min = y_min;
        X_max = x_max;
        Y_max = y_max;
    }
}

