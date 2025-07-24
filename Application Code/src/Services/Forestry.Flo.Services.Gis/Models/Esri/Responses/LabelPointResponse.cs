using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses
{
    public class LabelPointResponse
    {
        [JsonProperty("labelPoints")]
        public List<Point> Points { get; set; }

        public LabelPointResponse()
        {
            Points = new List<Point>();
        }
    }
}
