using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json;

public record BaseFeatureObject<A>
{

    /// <summary>
    /// The description of the "thing" to write to the layer
    /// </summary>
    [JsonProperty("attributes")]
    public A Attributes { get; set; } = default!;
}

public record BaseFeatureWithGeometryObject<G,A>: BaseFeatureObject<A>
{
    /// <summary>
    /// The json of the Geometry to send to the server
    /// </summary>
    [JsonProperty("geometry", NullValueHandling = NullValueHandling.Ignore)]
    public G? GeometryObject { get; set; }

}
