using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses
{
    /// <summary>
    /// The Response from the intersect response
    /// </summary>
    /// <typeparam name="T">The Shape that we expect back from the service</typeparam>
    public class IntersectResponse<T>
    {
        /// <summary>
        /// The name of the geometry type checked (This is controlled by the values in the geometries)
        /// </summary>
        [JsonProperty("geometryType")]
        public string GeometryType { get; set; } = null!;

        /// <summary>
        /// The Array of the shapes that appear of the intersects
        /// </summary>
        [JsonProperty("geometries")]
        public T[]? Geometries { get; set; } = null;
    }

    public abstract class GeometryResponse
    {

    }


    /// <summary>
    /// Point shape
    /// </summary>
    public class GeometryPointResponse : GeometryResponse
    {
        /// <summary>
        /// The Longitude co-ordinate
        /// </summary>
        [JsonProperty("x")]
        public float X { get; set; }

        /// <summary>
        /// The Latitude Co-ordinate
        /// </summary>
        [JsonProperty("y")]
        public float Y { get; set; }
    }

    /// <summary>
    /// Line shape
    /// </summary>
    public class GeometryLineResponse : GeometryResponse
    {
        /// <summary>
        /// The path of the shape
        /// </summary>
        [JsonProperty("paths")]
        public float[][][] Paths { get; set; } = null!;
    }

    /// <summary>
    /// Polygon shape
    /// </summary>
    public class GeometryPolygonResponse : GeometryResponse
    {
        /// <summary>
        /// The completed ring of the shape
        /// </summary>
        [JsonProperty("rings")]
        public float[][][] Rings { get; set; } = null!;
    }
}
