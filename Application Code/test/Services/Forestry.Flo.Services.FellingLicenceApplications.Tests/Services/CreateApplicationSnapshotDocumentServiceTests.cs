using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class CreateApplicationSnapshotDocumentServiceTests
    {
        private readonly Mock<ILogger<CreateApplicationSnapshotDocumentService>> _logger;
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepositoryMock;
        private readonly Mock<HttpClient> _client;
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock;

        private readonly Mock<IOptions<PDFGeneratorAPIOptions>> _options;
        private readonly PDFGeneratorAPIOptions _pdfGeneratorApiOptions;

        public CreateApplicationSnapshotDocumentServiceTests()
        {
            _fellingLicenceApplicationInternalRepositoryMock = new Mock<IFellingLicenceApplicationInternalRepository>();
            _logger = new Mock<ILogger<CreateApplicationSnapshotDocumentService>>();
            _client = new Mock<HttpClient>();

            _pdfGeneratorApiOptions = new PDFGeneratorAPIOptions
            {
                BaseUrl = "http://localhost:9999/api/v1/generate-pdf"
            };

            _options = new Mock<IOptions<PDFGeneratorAPIOptions>>();
            _options.SetupGet(c => c.Value).Returns(_pdfGeneratorApiOptions);

            _unitOfWOrkMock = new Mock<IUnitOfWork>();
        }

        [Theory, AutoMoqData]
        public async Task shouldReturnSuccess_WhenApiRequestResponseSuccess(
            PDFGeneratorRequest pdfGeneratorRequest,
            FellingLicenceApplication fla,
            HttpResponseMessage apiResult)
        {
            // setup
            var sut = CreateSut();

            apiResult.StatusCode = HttpStatusCode.Accepted;

            _fellingLicenceApplicationInternalRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            _client
                .Setup(r => r.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResult);

            var result = await sut.CreateApplicationSnapshotAsync(fla.Id, pdfGeneratorRequest, CancellationToken.None);

            // assert

            Assert.True(result.IsSuccess);

            // verify

            _fellingLicenceApplicationInternalRepositoryMock.Verify(v => v
                    .GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

            _client.Verify(v=>v.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);

        }

        [Theory, AutoMoqData]
        public async Task shouldReturnFailure_WhenApiRequestResponseNotFound(
            PDFGeneratorRequest pdfGeneratorRequest,
            FellingLicenceApplication fla,
            HttpResponseMessage apiResult)
        {
            // setup
            var sut = CreateSut();

            apiResult.StatusCode = HttpStatusCode.NotFound;

            _fellingLicenceApplicationInternalRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            _client
                .Setup(r => r.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResult);

            var result = await sut.CreateApplicationSnapshotAsync(fla.Id, pdfGeneratorRequest, CancellationToken.None);
            
            // assert

            Assert.True(result.IsFailure);

            // verify

            _fellingLicenceApplicationInternalRepositoryMock.Verify(v => v
                .GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

            _client.Verify(v => v.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);

        }

        [Theory, AutoMoqData]
        public async Task shouldReturnFailure_WhenApiRequestResponseUnauthorized(
            PDFGeneratorRequest pdfGeneratorRequest,
            FellingLicenceApplication fla,
            HttpResponseMessage apiResult)
        {
            // setup
            var sut = CreateSut();

            apiResult.StatusCode = HttpStatusCode.Unauthorized;

            _fellingLicenceApplicationInternalRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            _client
                .Setup(r => r.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResult);

            var result = await sut.CreateApplicationSnapshotAsync(fla.Id, pdfGeneratorRequest, CancellationToken.None);

            // assert

            Assert.True(result.IsFailure);

            // verify

            _fellingLicenceApplicationInternalRepositoryMock.Verify(v => v
                .GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

            _client.Verify(v => v.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);

        }

        [Theory, AutoMoqData]
        public async Task shouldReturnFailure_WhenNoFlaExists(
            Guid applicationId,
            PDFGeneratorRequest pdfGeneratorRequest)
        {
            // setup
            var sut = CreateSut();

            _fellingLicenceApplicationInternalRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe.None);

            var result = await sut.CreateApplicationSnapshotAsync(applicationId, pdfGeneratorRequest, CancellationToken.None);

            // assert

            Assert.True(result.IsFailure);

            // verify

            _fellingLicenceApplicationInternalRepositoryMock.Verify(v => v
                .GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

            _client.Verify(v => v.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);

        }

        private CreateApplicationSnapshotDocumentService CreateSut(int maxNumberOfDocuments=2, int maxFileSizeBytes=1024)
        {
            _fellingLicenceApplicationInternalRepositoryMock.Reset();
            _logger.Reset();
            _client.Reset();

            _unitOfWOrkMock.Reset();
            
            return new CreateApplicationSnapshotDocumentService(
                _fellingLicenceApplicationInternalRepositoryMock.Object,
                _logger.Object,
                _client.Object,
                _options.Object
            );
        }
    }
}
