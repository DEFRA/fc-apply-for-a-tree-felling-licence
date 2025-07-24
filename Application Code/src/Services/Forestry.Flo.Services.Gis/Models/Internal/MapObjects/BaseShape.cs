using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using Forestry.Flo.Services.Gis.Models.MapObjects;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Internal.MapObjects
{
    /// <summary>
    /// The Base-shape for the other incoming shapes to inherit from
    /// </summary>
    public abstract class BaseShape
    {
        [JsonProperty("spatialReference", NullValueHandling = NullValueHandling.Ignore)]
        public SpatialReference? SpatialSettings { get; set; }

        public string GeometryType { get; protected set; }

        /// <summary>
        /// This method will return a simple string representation to form a query with
        /// </summary>
        /// <returns>a string with only the information needed to make a geometry string</returns>
        public abstract string GetGeometrySimple();


        /// <summary>
        /// This method will return a a more complex string representation to form a query with
        /// </summary>
        /// <returns>a string with the information needed to make a geometry string for the API to consume</returns>
        public abstract string GetGeometryRequest();

        protected BaseShape(string geometryType)
        {
            GeometryType = geometryType;
            SpatialSettings = new SpatialReference();
        }

        /// <summary>
        /// Gets the Extent of the shape
        /// </summary>
        /// <returns>The Extent of the shape</returns>
        public abstract Maybe<Extent> GetExtent();

        public Point? GetCenterPoint()
        {
            var extent = GetExtent();
            if (extent.HasNoValue)
            {
                return null;
            }
            return new(((extent.Value.X_max + extent.Value.X_min) / 2), ((extent.Value.Y_max + extent.Value.Y_min) / 2));
        }
    }
}
