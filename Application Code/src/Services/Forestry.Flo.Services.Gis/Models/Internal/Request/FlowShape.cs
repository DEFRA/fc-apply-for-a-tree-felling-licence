using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Internal.Request
{
    /// <summary>
    /// A shape object for the map to return to the server
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FlowShape<T>
    {
        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The type of the object being sent
        /// <value>Point | Polyline | Polygon </value>
        /// </summary>
        public string ShapeType { get; set; } = null!;

        /// <summary>
        /// The Shape of the object
        /// </summary>
        public T ShapeDetails { get; set; }

        public FlowShape()
        {

        }
    }
}
