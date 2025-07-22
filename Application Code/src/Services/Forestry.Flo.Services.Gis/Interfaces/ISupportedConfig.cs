namespace Forestry.Flo.Services.Gis.Interfaces
{
    /// <summary>
    /// For accessing information from the configuration file
    /// </summary>
    public interface ISupportedConfig
    {
        /// <summary>
        /// The supported file types that can be uploaded by the 
        /// </summary>
        /// <returns>The values that the system will allow to be uploaded</returns>
        public string[] GetSupportedFileTypes();
    }
}
