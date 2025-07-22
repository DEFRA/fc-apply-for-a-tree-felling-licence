using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FileStorage.Configuration
{
  public class AzureFileStorageOptions
    {
        /// <summary>
        /// Gets and sets the storage account connection string
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = "DefaultEndpointsProtocol=https;AccountName=devflo;AccountKey=eguoxpoIbUH6PSc4X9QNWFsdi5F3A+jlH09iVoB8/RV1yXz6LTBv2s3AxON2qsPXVuF6OZMxtTZz+AStqZhfjw==;EndpointSuffix=core.windows.net";

        /// <summary>
        /// Gets and sets the container to use for file storage
        /// </summary>
        [Required]
        public string Container { get; set; } = "devflo";

    }
}
