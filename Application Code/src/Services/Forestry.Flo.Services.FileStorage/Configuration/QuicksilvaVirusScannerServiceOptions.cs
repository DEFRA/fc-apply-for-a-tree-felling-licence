using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FileStorage.Services;

namespace Forestry.Flo.Services.FileStorage.Configuration
{
    
    /// <summary>
    /// Configuration class used in conjunction with a <see cref="QuicksilvaVirusScannerService"/>
    /// implementation.
    /// </summary>
    public class QuicksilvaVirusScannerServiceOptions
    {
        /// <summary>
        /// Gets and sets the location of the Quicksilva Anti Virus scanner endpoint which is used by the system
        /// </summary>
        [Required]
        public string AvEndpoint { get; set; } = "http://localhost:55560/scan";

        
        /// <summary>
        /// Gets or sets whether the injected Quicksilva Anti Virus scanner service should actually be used to scan files being
        /// uploaded/sent to the system
        /// </summary>
        public bool IsEnabled {get; set;} = true;
    }
}
