using Forestry.Flo.Services.Gis.Models.MapObjects;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;

public class WebMap_MapOptions
{
    [JsonProperty("extent")]
    public Extent MapExtent { get; set; }

    [JsonProperty("spatialReference")]
    public SpatialReference Spatial_Reference { get; set; } 

    [JsonProperty("showAttribution")]
    public bool ShowAttribution { get; set; } = false;

    public WebMap_MapOptions(float x_min, float y_min, float x_max, float y_max, int wkid, int? latestWKID = null)
    {
        Spatial_Reference = new(wkid, latestWKID);
        MapExtent = new(x_min, y_min, x_max, y_max, wkid, latestWKID);

    }

    public WebMap_MapOptions()
    {
        MapExtent = new();
        Spatial_Reference = new(27700, 27700);
    }
}
