using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.FileStorage.Services
{
    /// <summary>
    /// An implementation of <see cref="IFileStorageService"/> that stores files to a disk location.
    /// </summary>
    public class PersistentDiskFileStorageService : IFileStorageService
    {
        private readonly IVirusScannerService _virusScannerService;
        private readonly FileValidator _fileValidator;
        private readonly PersistentDiskStorageOptions _options;
        private readonly ILogger<PersistentDiskFileStorageService> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="PersistentDiskFileStorageService"/>.
        /// </summary>
        /// <param name="fileValidator">Class to validate a file</param>
        /// <param name="virusScannerService">The implementation of a <see cref="IVirusScannerService"/> to check files before storing.</param>
        /// <param name="options">The <see cref="PersistentDiskStorageOptions"/> configuration to use with this implementation</param>
        /// <param name="logger"></param>
        public PersistentDiskFileStorageService(
            FileValidator fileValidator,
            IVirusScannerService virusScannerService,
            IOptions<PersistentDiskStorageOptions> options,
            ILogger<PersistentDiskFileStorageService> logger
            )
        {
            _virusScannerService = Guard.Against.Null(virusScannerService);
            _fileValidator = Guard.Against.Null(fileValidator);
            _options = Guard.Against.Null(options.Value);
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Result<StoreFileSuccessResult, StoreFileFailureResult>> StoreFileAsync(
            string originalFileName,
            byte[] fileBytes,
            string storeLocation,
            bool receivedByApi,
            FileUploadReason reason,
            CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(originalFileName);
            Guard.Against.NullOrWhiteSpace(storeLocation);
            Guard.Against.Null(fileBytes);

            var validationResult = _fileValidator.Validate(fileBytes, originalFileName, receivedByApi, reason);

            if (validationResult.IsFailure)
            {
                _logger.LogWarning("File for upload was not successfully validated. Validator returned with [{fileValidationError}].", validationResult.Error);

                return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(
                    StoreFileFailureResult.CreateWithInvalidFileReason(validationResult.Error));
            }

            _logger.LogDebug("File for upload having name [{fileName}] was successfully validated.", originalFileName);

            var virusScanResult = await _virusScannerService.ScanAsync(originalFileName, fileBytes, cancellationToken);

            if (virusScanResult is not AntiVirusScanResult.DisabledInConfiguration and not AntiVirusScanResult.VirusFree)
            {
                _logger.LogWarning("File for upload named [{fileName}] failed the virus scan with result of [{virusScanResult}]."
                    , originalFileName, virusScanResult);

                return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(
                    StoreFileFailureResult.CreateWithInvalidFileReason(FileInvalidReason.FailedVirusScan));
            }

            _logger.LogDebug("File for upload named [{fileName}] successfully passed the virus scan.", originalFileName);

            EnsureDirectoryContainer(storeLocation);

            var filePath = Path.Combine(_options.StorageRootPath, storeLocation, Path.GetRandomFileName());

            _logger.LogDebug("Writing [{fileLength}] bytes to disk at location [{filePath}].",
                fileBytes.Length, filePath);

            if (File.Exists(filePath))
            {
                //Should not ordinarily happen - given use of Path.GetRandomFileName() especially. within a sub-dir of the storage root
                _logger.LogWarning("File at {filePath} already exists.", filePath);
                return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileFailureResult(StoreFileFailureResultReason.Duplicate));
            }

            try
            {
                await using (var fileStream = File.Create(filePath))
                {
                    await fileStream.WriteAsync(fileBytes, cancellationToken);
                }

                _logger.LogDebug("Successfully wrote [{fileLength}] bytes to [{filePath}].", fileBytes.Length, filePath);
                return Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileSuccessResult(filePath, fileBytes.Length));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to write the requested file bytes to disk.");
            }

            return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileFailureResult(StoreFileFailureResultReason.Unknown));
        }

        /// <inheritdoc />
        public async Task<UnitResult<FileAccessFailureReason>> RemoveFileAsync(string fileLocation, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(fileLocation);

            var failureReason = FileAccessFailureReason.Unknown;
            
            if (File.Exists(fileLocation))
            {
                _logger.LogDebug("Removing file on disk at location [{fileLocation}].", fileLocation);

                try
                {
                    File.Delete(fileLocation);
                    _logger.LogDebug("File was removed at location [{fileLocation}].", fileLocation);
                    return UnitResult.Success<FileAccessFailureReason>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to delete the requested file at [{fileLocation}] from disk.", fileLocation);
                }
            }
            else
            {
                _logger.LogWarning("Request made to remove file which does not exist at specified location of [{fileLocation}].", fileLocation);
                failureReason = FileAccessFailureReason.NotFound;
            }
            return UnitResult.Failure(failureReason);
        }

        /// <inheritdoc />
        public async Task<Result<GetFileSuccessResult, FileAccessFailureReason>> GetFileAsync(string fileLocation, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(fileLocation);

            var failureReason = FileAccessFailureReason.Unknown;

            if (File.Exists(fileLocation))
            {
                _logger.LogDebug("Getting bytes for file on disk at location [{fileLocation}].", fileLocation);

                try
                {
                    var fileBytes = await File.ReadAllBytesAsync(fileLocation, cancellationToken);
                    _logger.LogDebug("File was accessed at location [{fileLocation}].", fileLocation);
                    return Result.Success<GetFileSuccessResult, FileAccessFailureReason>(new GetFileSuccessResult(fileBytes));

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to get the requested file bytes from location of [{fileLocation}] on disk.", fileLocation);
                }
            }
            else
            {
                _logger.LogWarning("Request made to get file which does not exist at specified location of [{fileLocation}].", fileLocation);
                failureReason = FileAccessFailureReason.NotFound;
            }
            return Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(failureReason);
        }

        private void EnsureDirectoryContainer(string path)
        {
            var fullDirectoryHierarchy = Path.Combine(_options.StorageRootPath, path);

            if (Directory.Exists(fullDirectoryHierarchy)) return;

            _logger.LogDebug("Creating new directory for file as directory [{fullDirectoryHierarchy}] does not exist."
                , fullDirectoryHierarchy);

            Directory.CreateDirectory(fullDirectoryHierarchy);
            _logger.LogDebug("New directory for file as directory [{fullDirectoryHierarchy}] created."
                , fullDirectoryHierarchy);
        }
    }
}