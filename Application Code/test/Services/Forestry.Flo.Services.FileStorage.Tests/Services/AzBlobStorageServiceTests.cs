using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileSignatures;
using FileSignatures.Formats;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.FileStorage.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Forestry.Flo.Services.FileStorage.Tests.Services
{
    public class AzBlobStorageServiceTests
    {
        private AzureFileStorageService? _sut;
        private readonly Mock<IVirusScannerService> _mockVirusScannerService;
        private readonly Mock<IFileFormatInspector> _mockFileFormatInspector;

        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
        private readonly Mock<BlobContainerClient> _mockBlobContainerClient;
        private readonly Mock<BlobClient> _mockBlobClient;

        private readonly string _testExecutionPath;
        private readonly string _permittedExtension = "rtf";
        private string _storeLocationUnderRoot = string.Empty;
        
        public AzBlobStorageServiceTests()
        {
            _testExecutionPath = Path.Combine(
                Path.GetDirectoryName( 
                    Path.GetDirectoryName(TestContext.CurrentContext.TestDirectory))!
                ,"tests");

            _mockVirusScannerService = new Mock<IVirusScannerService>();
            _mockFileFormatInspector = new Mock<IFileFormatInspector>();
            _mockBlobServiceClient = new Mock<BlobServiceClient>();

            _mockBlobContainerClient = new Mock<BlobContainerClient>();
            _mockBlobClient = new Mock<BlobClient>();


            _mockVirusScannerService.Setup(x => x.ScanAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AntiVirusScanResult.VirusFree);
        }

        [Theory]
        public async Task ValidFileIsStored()
        {
            //arrange
            _sut = CreateSut(storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test."+_permittedExtension,
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
            Assert.That(result.Value.FileSize, Is.EqualTo(10L));

            var storedFile = new FileInfo(result.Value.Location!);
            Assert.NotNull(storedFile);
        }

        [Theory]
        public async Task ValidFileIsStoredWithDifferentExtensionCase()
        {
            //arrange
            _sut = CreateSut(storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test."+_permittedExtension.ToUpper(),
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
            Assert.That(result.Value.FileSize, Is.EqualTo(10L));

            var storedFile = new FileInfo(result.Value.Location!);
            Assert.NotNull(storedFile);
        }

        [Theory]
        public async Task InvalidFileExtensionNotStored()
        {
            //arrange
            _sut = CreateSut(storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test."+_permittedExtension+"xyz",
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.That(result.Error.InvalidReason,  Is.EqualTo(FileInvalidReason.ExtensionNotSupported));
        }

        [Theory]
        public async Task WhenNoExtensionIsNotStored()
        {
            //arrange
            _sut = CreateSut(storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test",
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.That(result.Error.InvalidReason,  Is.EqualTo(FileInvalidReason.ExtensionNotSupported));
        }

        [Theory]
        public async Task EmptyFileIsNotStored()
        {
            //arrange
            _sut = CreateSut(storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test.csv",
                Array.Empty<byte>(),
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.That(result.Error.InvalidReason,  Is.EqualTo(FileInvalidReason.EmptyFile));
        }

        [Theory]
        public async Task FileTooLargeIsNotStored_WhenInitiateByUser()
        {
            //arrange
            var maxFileSize = 10000;
            _sut = CreateSut(userMaxFileSizeBytes: maxFileSize, storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test."+_permittedExtension,
                new byte[maxFileSize+1],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.That(result.Error.InvalidReason,  Is.EqualTo(FileInvalidReason.FileTooLarge));
        }

        [Theory]
        public async Task FileTooLargeIsNotStored_WhenInitiateByApi()
        {
            //arrange
            var maxFileSize = 100000000;
            _sut = CreateSut(apiMaxFileSizeBytes: maxFileSize, storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test."+_permittedExtension,
                new byte[maxFileSize+1],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.That(result.Error.InvalidReason,  Is.EqualTo(FileInvalidReason.FileTooLarge));
        }



        [Theory]
        public async Task FileFailingDueToFileSignatureMismatchWithExtension()
        {
            //arrange
            _sut = CreateSut(storageAction: StorageAction.Store);
           
            //act
            var result = await _sut.StoreFileAsync(
                "test.csv",
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.That(result.Error.InvalidReason,  Is.EqualTo(FileInvalidReason.FileSignatureDoesNotMatchSuppliedFileExtension));
        }

        [Theory]
        public async Task WhenFileIsNotAKnownSignatureButHasPermittedExtensionItShouldStillBeStored()
        {
            //arrange
            _sut = CreateSut(fileInspectorShouldMatch:false, storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test.csv",
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        public async Task FileFailingVirusScanIsNotStored()
        {
            //arrange
            _sut = CreateSut(antiVirusScanResult: AntiVirusScanResult.VirusFound, storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test."+_permittedExtension,
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.That(result.Error.InvalidReason,  Is.EqualTo(FileInvalidReason.FailedVirusScan));
        }

        [Theory]
        public async Task FileWhichCouldNotBeScannedByVirusScannerIsNotStored()
        {
            //arrange
            _sut = CreateSut(antiVirusScanResult: AntiVirusScanResult.Undetermined, storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test."+_permittedExtension,
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.That(result.Error.InvalidReason,  Is.EqualTo(FileInvalidReason.FailedVirusScan));
        }
        
        [Theory]
        public async Task WhenAvServiceIsDisabledThenFileIsStillProcessed()
        {
            //arrange
            _sut = CreateSut(antiVirusScanResult: AntiVirusScanResult.DisabledInConfiguration, storageAction: StorageAction.Store);

            //act
            var result = await _sut.StoreFileAsync(
                "test."+_permittedExtension,
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            //assert
            Assert.That(result.Value.FileSize, Is.EqualTo(10L));
            Assert.True(result.IsSuccess);
        }

        [Theory]
        public async Task WhenFileToRemoveIsFound()
        {
            //arrange
            _sut = CreateSut(storageAction: StorageAction.Remove);
            
            var actResult = await _sut.StoreFileAsync(
                "test."+_permittedExtension,
                new byte[10L],
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());
            
            Assert.True(actResult.IsSuccess);

            //act
            var result = await _sut.RemoveFileAsync("test.rtf", new CancellationToken());
            Assert.True(result.IsSuccess);
        }

        [Theory]
        public async Task WhenFileToRemoveIsNotFound()
        {
            _sut = CreateSut(storageAction: StorageAction.Remove, testResult: TestResult.Fail);
            var result = await _sut.RemoveFileAsync("test.rtf", new CancellationToken());

            Assert.True(result.IsFailure);

            Assert.That(result.Error, Is.EqualTo(FileAccessFailureReason.NotFound));
        }

        [Theory]
        public async Task GetFileWhenFoundReturnsBytes()
        {
            //arrange
            _sut = CreateSut(storageAction: StorageAction.Get);
            var docBytes = new byte[] { 0x20 };

            var actResult = await _sut.StoreFileAsync(
                "test."+_permittedExtension,
                docBytes,
                _storeLocationUnderRoot,
                receivedByApi:false,
                FileUploadReason.SupportingDocument,
                new CancellationToken());

            Assert.True(actResult.IsSuccess);

            //act
            var result = await _sut.GetFileAsync("test.rtf", new CancellationToken());

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        public async Task GetFileWhenNotFoundReturnsFailureWithNotFound()
        {
            _sut = CreateSut(storageAction: StorageAction.Get, testResult: TestResult.Fail);
            var result = await _sut.GetFileAsync("NoSuchFile", new CancellationToken());

            Assert.True(result.IsFailure);

            Assert.That(result.Error, Is.EqualTo(FileAccessFailureReason.NotFound));
        }

        private AzureFileStorageService CreateSut(
            int maxNumberOfDocuments=2, 
            int userMaxFileSizeBytes=1024,
            int apiMaxFileSizeBytes= 4096,
            AntiVirusScanResult antiVirusScanResult = AntiVirusScanResult.VirusFree,
            bool fileInspectorShouldMatch = true,
            TestResult testResult = TestResult.Success,
            StorageAction storageAction = StorageAction.Store)
        {
            _storeLocationUnderRoot = Guid.NewGuid().ToString();

            _mockVirusScannerService.Reset();
            _mockFileFormatInspector.Reset();
            _mockBlobServiceClient.Reset();
            _mockBlobContainerClient.Reset();
            _mockBlobClient.Reset();

            switch (storageAction)
            {
                case StorageAction.Get:
                    if (testResult == TestResult.Success)
                    {
                        var mockResponse = new Mock<Response>();
                        var mockValue = BlobsModelFactory.BlobDownloadResult(new BinaryData(new byte[10L]), null);
                        _mockBlobClient.Setup(x => x.DownloadContentAsync().Result)
                            .Returns(Response.FromValue(mockValue, mockResponse.Object));
                    }
                    else
                    {
                        _mockBlobClient.Setup(x => x.DownloadContentAsync())
                            .Throws(new RequestFailedException("File Get Error"));
                    }
                    break;
                case StorageAction.Store:
                    if (testResult == TestResult.Success)
                    {
                        _mockBlobClient.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()).Result)
                            .Returns(() => null);
                    }
                    else
                    {
                        _mockBlobClient.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()))
                            .Throws(new RequestFailedException("File Upload Error"));
                    }
                    break;
                case StorageAction.Remove:
                    if (testResult == TestResult.Success)
                    {
                        _mockBlobClient.Setup(x => x.DeleteAsync(It.IsAny<DeleteSnapshotsOption>(), null, It.IsAny<CancellationToken>()).Result)
                            .Returns(() => null);
                    }
                    else
                    {
                        _mockBlobClient.Setup(x => x.DeleteAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                            .Throws(new RequestFailedException("File Remove Error"));
                    }
                    break;
                default:
                    break;
            }

            _mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_mockBlobClient.Object);

            _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient(It.IsAny<string>())).Returns(
                _mockBlobContainerClient.Object);

            _mockVirusScannerService.Setup(x => x.ScanAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(antiVirusScanResult);

            if (fileInspectorShouldMatch)
            {
                //permits the extension the test is configured with (rtf)
                _mockFileFormatInspector.Setup(x => x.DetermineFileFormat(It.IsAny<Stream>()))
                    .Returns(
                        new RichTextFormat());
            }
            else
            {
                _mockFileFormatInspector.Setup(x => x.DetermineFileFormat(It.IsAny<Stream>()));
            }

            
            var userFileUploadOptions = new UserFileUploadOptions
            {
                MaxNumberDocuments = maxNumberOfDocuments,
                MaxFileSizeBytes = userMaxFileSizeBytes,
                AllowedFileTypes = new[]
                {
                    new AllowedFileType
                    {
                        FileUploadReasons = [FileUploadReason.SupportingDocument, FileUploadReason.AgentAuthorityForm],
                        Description = "test",
                        Extensions = new[] { _permittedExtension,"csv","doc" }
                    }
                }
            };

            var apiFileUploadOptions = new ApiFileUploadOptions()
            {
                MaxFileSizeBytes = apiMaxFileSizeBytes,
            };

            var persistentDiskStorageOptions = new AzureFileStorageOptions()
            {
                ConnectionString = "",
                Container = ""
            };
            
            var fileValidator = new FileValidator(
                _mockFileFormatInspector.Object,
                Options.Create(userFileUploadOptions),
                Options.Create(apiFileUploadOptions));

            return new AzureFileStorageService(
                _mockBlobServiceClient.Object,
                fileValidator,
                _mockVirusScannerService.Object,
                Options.Create(persistentDiskStorageOptions),
                new NullLogger<AzureFileStorageService>()
            );
        }
    }
}