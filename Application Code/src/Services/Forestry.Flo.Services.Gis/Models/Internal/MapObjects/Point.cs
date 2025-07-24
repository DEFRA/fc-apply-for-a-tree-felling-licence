using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Internal.MapObjects
{
    /// <summary>
    /// Representation of a Point in Flo-v2
    /// </summary>
    public class Point : BaseShape
    {
        /// <summary>
        /// The Longitude of the point
        /// </summary>
        [JsonProperty("x")]
        public float? X { get; set; }

        /// <summary>
        /// The Latitude of the point
        /// </summary>
        [JsonProperty("y")]
        public float? Y { get; set; }

        ///<inheritdoc />
        public override string GetGeometrySimple()
        {
            return JsonConvert.SerializeObject(new
            {
                x = X,
                y = Y
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
                    x = X,
                    y = Y
                }
            });
        }

        public Point(float x, float y) : base("esriGeometryPoint")
        {
            X = x;
            Y = y;
        }

        public Point() : base("esriGeometryPoint")
        {
        }

        public override Maybe<Extent> GetExtent()
        {
            if (X == null | Y == null)
            {
                return Maybe<Extent>.None;
            }
            Extent result = new();
            List<Point> points = new();

            result.X_min = (float)X;
            result.Y_min = (float)Y;
            result.X_max = (float)X;
            result.Y_max = (float)Y;
            return Maybe.From(result);
        }
    }

}
