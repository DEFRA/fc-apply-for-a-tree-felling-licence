using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FileStorage.Configuration
{
  public class PersistentDiskStorageOptions
    {
        /// <summary>
        /// Gets and sets the Path to the location on the persistent disk where all files will be saved under.
        /// </summary>
        [Required]
        public string StorageRootPath { get; set; } = "C:\\temp\\fla";
    }
}
