using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.AdminHubs.Entities
{
    /// <summary>
    /// Entity class representing an area which is assigned to an <see cref="AdminHub"/>
    /// </summary>
    public class Area
    {
        /// <summary>
        /// Gets and sets the unique internal identifier for the Area.
        /// </summary>
        [Key]
        public Guid Id { get;   set; }

        /// <summary>
        /// Gets the name of the Area
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets the code of the Area.
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// The Admin hub belonging to this Area.
        /// </summary>
        public AdminHub AdminHub { get; set; }

        public Guid AdminHubId { get;  set; }
    }
}
