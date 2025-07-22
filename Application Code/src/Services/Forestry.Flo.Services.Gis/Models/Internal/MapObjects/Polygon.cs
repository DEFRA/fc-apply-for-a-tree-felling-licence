using Newtonsoft.Json;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using CSharpFunctionalExtensions;


namespace Forestry.Flo.Services.Gis.Models.Internal.MapObjects
{
    /// <summary>
    /// Representation of a Polygon in Flo-v2
    /// </summary>
    public class Polygon : BaseShape
    {
        /// <summary>
        /// The Rings that make up a polygon
        /// </summary>
        [JsonProperty("rings")]
        public List<List<List<float>>>? Rings { get; set; }

        ///<inheritdoc />
        public override string GetGeometrySimple()
        {
            return JsonConvert.SerializeObject(new
            {
                rings = Rings
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
                    rings = Rings
                }
            });
        }

        public Polygon() : base("esriGeometryPolygon")
        {
        }


        public override Maybe<Extent> GetExtent()
        {
            if(Rings== null)
            {
                return Maybe<Extent>.None;
            }
            Extent result = new();
            List<Point> points = (from outerPoint in Rings from innerPoint in outerPoint select new Point(innerPoint[0], innerPoint[1])).ToList();

            result.X_min = (float)points.Min(p => p.X);
            result.Y_min = (float)points.Min(p => p.Y);
            result.X_max = (float)points.Max(p => p.X);
            result.Y_max = (float)points.Max(p => p.Y);
            return Maybe.From(result);
        }
    }
}
