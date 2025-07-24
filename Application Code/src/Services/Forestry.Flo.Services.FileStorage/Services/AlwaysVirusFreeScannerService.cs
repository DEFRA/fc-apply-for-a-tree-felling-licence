using Forestry.Flo.Services.FileStorage.ResultModels;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FileStorage.Services;

/// <summary>
/// An implementation of <see cref="IVirusScannerService"/> that always returns with no virus found.
/// <para>
/// Note, this implementation should not be used in UAT or Production environments.
/// </para>
/// </summary>
public class AlwaysVirusFreeScannerService : IVirusScannerService
{
    private readonly ILogger<AlwaysVirusFreeScannerService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="AlwaysVirusFreeScannerService"/>.
    /// </summary>
    /// <param name="logger"></param>
    public AlwaysVirusFreeScannerService(ILogger<AlwaysVirusFreeScannerService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// This implementation will always complete with the result that no virus was found.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="bytes"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AntiVirusScanResult> ScanAsync(
        string fileName, 
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("About to scan file named {fileName}, with length of [{byteLength}] bytes."
            , fileName, bytes.Length);

        _logger.LogDebug("Completed scan of file named {fileName}, is virus free? [true].", fileName);

        return await Task.FromResult(AntiVirusScanResult.VirusFree);
    }
}