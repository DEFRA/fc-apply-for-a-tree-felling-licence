using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Common
{
    public class Feature<G, A>
    {
        [JsonProperty("geometry", NullValueHandling = NullValueHandling.Ignore)]
        public G Geometry { get; private set; }

        [JsonProperty("attributes", NullValueHandling = NullValueHandling.Ignore)]
        public A Attributes { get; private set; }

        public Feature(G geometry, A attributes)
        {
            Geometry = geometry;
            Attributes = attributes;
        }
    }
}
