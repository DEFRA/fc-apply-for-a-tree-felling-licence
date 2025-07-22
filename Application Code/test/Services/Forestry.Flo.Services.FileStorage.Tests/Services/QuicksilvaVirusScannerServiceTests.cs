using System.Net;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using IHttpMessageHandler = Moq.Contrib.HttpClient.IHttpMessageHandler;

namespace Forestry.Flo.Services.FileStorage.Tests.Services
{
    public class QuicksilvaVirusScannerServiceTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private QuicksilvaVirusScannerService? _sut;

        public QuicksilvaVirusScannerServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        }

        [Theory]
        public async Task ReturnsVirusFreeWhenReceived200StatusCode()
        {
            //arrange
            _sut = CreateSut();
            
            //act
            var result = await _sut.ScanAsync("test", new byte[10L], It.IsAny<CancellationToken>());
            
            //assert
            Assert.That(result, Is.EqualTo(AntiVirusScanResult.VirusFree));
        }

        [Theory]
        public async Task ReturnsVirusFoundWhenReceived406StatusCode()
        {
            //arrange
            _sut = CreateSut(HttpStatusCode.NotAcceptable);
            
            //act
            var result = await _sut.ScanAsync("test", new byte[10L], It.IsAny<CancellationToken>());
            
            //assert
            Assert.That(result, Is.EqualTo(AntiVirusScanResult.VirusFound));
        }

        [Theory]
        public async Task ReturnsUndeterminedWhenUnexpectedStatusCode()
        {
            //arrange
            _sut = CreateSut(HttpStatusCode.ExpectationFailed);
            
            //act
            var result = await _sut.ScanAsync("test", new byte[10L], It.IsAny<CancellationToken>());
            
            //assert
            Assert.That(result, Is.EqualTo(AntiVirusScanResult.Undetermined));
        }

        [Theory]
        public async Task ReturnsUndeterminedWhenExceptionOccurs()
        {
            //arrange
            _sut = CreateSut(httpCallThrowsException:true);
            
            //act
            var result = await _sut.ScanAsync("test", new byte[10L], It.IsAny<CancellationToken>());
            
            //assert
            Assert.That(result, Is.EqualTo(AntiVirusScanResult.Undetermined));
        }

        [Theory]
        public async Task ReturnsAvNotConfiguredResultWhenServiceIsDisabled()
        {
            //arrange
            _sut = CreateSut(isEnabled: false);
            
            //act
            var result = await _sut.ScanAsync("test", new byte[10L], It.IsAny<CancellationToken>());
            
            //assert
            Assert.That(result, Is.EqualTo(AntiVirusScanResult.DisabledInConfiguration));
        }

        private QuicksilvaVirusScannerService CreateSut(
            HttpStatusCode statusCode = HttpStatusCode.OK,
            bool httpCallThrowsException = false,
            bool isEnabled = true)

        {
            _mockHttpClientFactory.Reset();
            _mockHttpMessageHandler.Reset();

            if (!httpCallThrowsException)
            {
                _mockHttpMessageHandler.Protected().As<IHttpMessageHandler>()
                    .Setup(x => x.SendAsync(
                        It.Is<HttpRequestMessage>(r =>
                            r.Method == HttpMethod.Post),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new HttpResponseMessage(statusCode));
            }
            else
            {
                _mockHttpMessageHandler.Protected().As<IHttpMessageHandler>()
                    .Setup(x => x.SendAsync(
                        It.Is<HttpRequestMessage>(r =>
                            r.Method == HttpMethod.Post),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("test exception"));
            }

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return new QuicksilvaVirusScannerService(
                _mockHttpClientFactory.Object,
                Options.Create(new QuicksilvaVirusScannerServiceOptions
                {
                    AvEndpoint = "http://test.com",
                    IsEnabled = isEnabled
                }),
                new NullLogger<QuicksilvaVirusScannerService>()
            );
        }
    }
}
