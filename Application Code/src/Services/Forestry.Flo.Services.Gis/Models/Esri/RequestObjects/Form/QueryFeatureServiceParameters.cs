using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    public class QueryFeatureServiceParameters : CommonParameters
    {
        public string WhereString { get; set; } = string.Empty;

        public List<string> OutFields { get; set; } = [];

        public bool ReturnGeometry { get; set; } = false;

        public bool ReturnCountOnly { get; set; } = false;

        public BaseShape? QueryGeometry { get; set; }


        public string SpatialRelationship { get; set; } = "intersects";

        /// <inheritdoc />
        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {

            var data = new List<KeyValuePair<string, string>>()
            {
                new("returnGeometry", ReturnGeometry.ToString()),
                new("returnCountOnly", ReturnCountOnly.ToString()),
                new("spatialRelationship", SpatialRelationship),
                new("outfields", OutFields.Count == 0? "*": $"{string.Join(", ", OutFields)}"),
                new("f", RequestFormat)
            };

            if (TokenString.HasValue)
            {
                data.Add(new KeyValuePair<string, string>("token", TokenString.Value));
            }
            if (!string.IsNullOrEmpty(WhereString))
            {
                data.Add(new KeyValuePair<string, string>("where", WhereString));
            }
            else if(QueryGeometry != null)
            {
                data.Add(new KeyValuePair<string, string>("geometry", QueryGeometry.GetGeometrySimple()));
                data.Add(new KeyValuePair<string, string>("geometryType", QueryGeometry.GeometryType));
            }

            return new FormUrlEncodedContent(data.ToArray());
        }

        /// <summary>
        /// Use for get
        /// </summary>
        /// <returns></returns>
        public string ToQuerystring()
        {
            return RequestFormat;
        }
    }
}
