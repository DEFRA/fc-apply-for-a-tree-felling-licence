using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Internal.MapObjects
{
    /// <summary>
    /// Representation of a Polyline in Flo-v2
    /// </summary>
    public class Line : BaseShape
    {

        /// <summary>
        /// The Path shape
        /// </summary>
        [JsonProperty("paths")]
        public List<List<List<float>>>? Path { get; set; }

        ///<inheritdoc />
        public override string GetGeometrySimple()
        {
            return JsonConvert.SerializeObject(new
            {
                paths = Path
            });
        }

        ///<inheritdoc />
        public override string GetGeometryRequest()
        {
            return JsonConvert.SerializeObject(new
            {
                geometryType = GeometryType,
                geometry = new
                {
                    paths = Path
                }
            });
        }

        public Line() : base("esriGeometryPolyline")
        {
        }

        public override Maybe<Extent> GetExtent()
        {
            if (Path == null)
            {
                return Maybe<Extent>.None;
            }
            Extent result = new();
            List<Point> points = (from outerPoint in Path from innerPoint in outerPoint select new Point(innerPoint[0], innerPoint[1])).ToList();

            result.X_min = (float)points.Min(p => p.X);
            result.Y_min = (float)points.Min(p => p.Y);
            result.X_max = (float)points.Max(p => p.X);
            result.Y_max = (float)points.Max(p => p.Y);
            return Maybe.From(result);
        }
    }
}
