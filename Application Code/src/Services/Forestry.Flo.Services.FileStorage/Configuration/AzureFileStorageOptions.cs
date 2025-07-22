using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FileStorage.Configuration
{
  public class AzureFileStorageOptions
    {
        /// <summary>
        /// Gets and sets the storage account connection string
        /// </summary>
        [Required]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets and sets the container to use for file storage
        /// </summary>
        [Required]
        public string Container { get; set; } = "devflo";

    }
}
