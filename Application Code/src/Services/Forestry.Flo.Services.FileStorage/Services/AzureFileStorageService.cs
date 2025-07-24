using Ardalis.GuardClauses;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.FileStorage.Services
{
    /// <summary>
    /// An implementation of <see cref="IFileStorageService"/> that stores files to an Azure Storage Account.
    /// </summary>
    public class AzureFileStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IVirusScannerService _virusScannerService;
        private readonly FileValidator _fileValidator;
        private readonly AzureFileStorageOptions _options;
        private readonly ILogger<AzureFileStorageService> _logger;
        public AzureFileStorageService(
            BlobServiceClient blobServiceClient,
            FileValidator fileValidator,
            IVirusScannerService virusScannerService,
            IOptions<AzureFileStorageOptions> options,
            ILogger<AzureFileStorageService> logger)
        {
            _blobServiceClient = Guard.Against.Null(blobServiceClient);
            _virusScannerService = Guard.Against.Null(virusScannerService);
            _fileValidator = Guard.Against.Null(fileValidator);
            _options = Guard.Against.Null(options.Value);
            _logger = logger;
        }
        public async Task<Result<GetFileSuccessResult, FileAccessFailureReason>> GetFileAsync(string fileLocation, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(fileLocation);

            var failureReason = FileAccessFailureReason.Unknown;

            try
            {
                _logger.LogDebug("Getting bytes for file from azure file storage at location [{fileLocation}].", fileLocation);

                BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(_options.Container);
                BlobClient blob = container.GetBlobClient(fileLocation);
                BlobDownloadResult response = await blob.DownloadContentAsync();

                _logger.LogDebug("File was accessed at location [{fileLocation}].", fileLocation);
                return Result.Success<GetFileSuccessResult, FileAccessFailureReason>(new GetFileSuccessResult(response.Content.ToArray()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to get the requested file bytes from location of [{fileLocation}] on azure file storage.", fileLocation);
                failureReason = FileAccessFailureReason.NotFound;
            }
            return Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(failureReason);
        }

        public async Task<UnitResult<FileAccessFailureReason>> RemoveFileAsync(string fileLocation, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(fileLocation);

            var failureReason = FileAccessFailureReason.Unknown;

            try
            {
                _logger.LogDebug("Removing file from azure file storage at location [{fileLocation}].", fileLocation);

                var container = _blobServiceClient.GetBlobContainerClient(_options.Container);
                var blob = container.GetBlobClient(fileLocation);


                Response response = await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

                _logger.LogDebug("File was removed at location [{fileLocation}].", fileLocation);
                return UnitResult.Success<FileAccessFailureReason>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to delete the requested file at [{fileLocation}] from azure file storage.", fileLocation);
                failureReason = FileAccessFailureReason.NotFound;
            }

            return UnitResult.Failure(failureReason);
        }

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

            var filePath = Path.Combine(storeLocation, Path.GetRandomFileName());

            _logger.LogDebug("Writing [{fileBytesLength}] bytes to azure file storage at location [{filePath}].",
                fileBytes.Length, filePath);

            try
            {
                var container = _blobServiceClient.GetBlobContainerClient(_options.Container);
                var blob = container.GetBlobClient(filePath);
                var binaryData = new BinaryData(fileBytes);
                Response<BlobContentInfo>? response = await blob.UploadAsync(binaryData, false, cancellationToken);

                _logger.LogDebug("Successfully wrote [{fileLength}] bytes to [{filePath}].", fileBytes.Length, filePath);
                return Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileSuccessResult(filePath, fileBytes.Length));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to write the requested file bytes to azure file storage.");
            }

            return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileFailureResult(StoreFileFailureResultReason.Unknown));
        }
    }
}
