using Forestry.Flo.Services.FileStorage.ResultModels;

namespace Forestry.Flo.Services.FileStorage.Services
{
    /// <summary>
    /// Defines the contract for a service that implements the virus scanning of a given file.
    /// </summary>
    public interface IVirusScannerService
    {
        /// <summary>
        /// Determine if a file is virus free according to the provided implementation.
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="bytes">The file's bytes.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AntiVirusScanResult> ScanAsync(
            string fileName, 
            byte[] bytes,
            CancellationToken cancellationToken);
    }
}
