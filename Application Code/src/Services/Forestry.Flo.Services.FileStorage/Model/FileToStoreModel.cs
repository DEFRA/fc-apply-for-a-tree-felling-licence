namespace Forestry.Flo.Services.FileStorage.Model
{
    /// <summary>
    /// Model for a file to be stored in the system with the File Storage service.
    /// </summary>
    public class FileToStoreModel
    {
        /// <summary>
        /// Gets or sets the file name of the file.
        /// </summary>
        public string FileName  { get; set;}

        /// <summary>
        /// Gets or sets the byte array representing the file
        /// </summary>
        public byte[] FileBytes { get; set; }
        
        /// <summary>
        /// Gets or sets the raw Content-Type header of the file.
        /// </summary>
        public string ContentType { get; set; }
    }
}
