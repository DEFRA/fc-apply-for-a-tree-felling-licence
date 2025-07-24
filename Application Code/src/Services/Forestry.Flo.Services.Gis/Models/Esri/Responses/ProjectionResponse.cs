using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses;

public class ProjectionResponse<T>
{
    [JsonProperty("geometries")]
    public List<T> Geometries { get; set; }
}

public class LatLongObj
{
    [JsonProperty("x")]
    public float Longitude { get; set; }

    [JsonProperty("y")]
    public float Latitude { get; set; }
}
