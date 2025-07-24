using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Gis.Models.Internal
{
    public class InternalCompartmentDetails<T>
    {
        public T ShapeGeometry { get; set; } = default(T);
        /// <summary>
        /// The name of the compartment
        /// </summary>
        [MaxLength(50)]
        public string CompartmentLabel { get; set; } = null!;

        /// <summary>
        /// The compartment Number
        /// </summary>
        [MaxLength(50)]
        public string CompartmentNumber { get; set; } = null!;

        /// <summary>
        /// The sub compartment number
        /// </summary>
        [MaxLength(50)]
        public string SubCompartmentNo { get; set; } = null!;
    }

    public class InternalFullCompartmentDetails : InternalCompartmentDetails<Polygon>
    {
        /// <summary>
        /// The woodland name
        /// </summary>
        public string? WoodlandName { get; set; }

        /// <summary>
        /// The designation name
        /// </summary>
        public string? Designation { get; set; }

        //The GIS Shape object
        public string GISData { get; set; }
    }
}
