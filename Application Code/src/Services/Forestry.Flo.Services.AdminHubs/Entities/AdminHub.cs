using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.AdminHubs.Entities
{
    /// <summary>
    /// Entity class representing an Admin hub within the system.
    /// </summary>
    public class AdminHub
    {
        /// <summary>
        /// Gets the unique internal identifier for the Admin Hub record on the system.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets the name of the Admin Hub
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets the Internal User Account Id of the Administrative Manager for this Admin Hub.
        /// </summary>
        [Required]
        public Guid AdminManagerId { get; set; }

        /// <summary>
        /// Gets and sets the address of the Admin Hub
        /// </summary>
        [Required]
        public string Address { get; set; }

        /// <summary>
        /// Collection of <see cref="Areas"/> assigned to this Admin hub
        /// </summary>
        public ICollection<Area> Areas { get; set; }

        /// <summary>
        /// Collection of <see cref="AdminHubOfficer"/> assigned to this Admin hub
        /// </summary>
        public ICollection<AdminHubOfficer> AdminOfficers { get; set; }
    }
}
