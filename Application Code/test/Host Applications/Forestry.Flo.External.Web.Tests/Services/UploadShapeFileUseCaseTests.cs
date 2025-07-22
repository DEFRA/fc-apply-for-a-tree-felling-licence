using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Forestry.Flo.External.Web.Tests.Services
{
    public class UploadShapeFileUseCaseTests
    {
        private Mock<ISupportedConfig> _config;
        private Mock<IForestryServices> _access;
        private Mock<ILogger<UploadShapeFileUseCase>> _logger;


        public UploadShapeFileUseCaseTests()
        {
            _logger = new Mock<ILogger<UploadShapeFileUseCase>>();
            _access = new Mock<IForestryServices>();
            _config = new Mock<ISupportedConfig>();
        }

        [Fact]
        public void GetSupportedFileTypes_Success()
        {
            var expected = new string[] { "Val 1", "Val 2" };
            _config.Setup(m => m.GetSupportedFileTypes()).Returns(expected);
            var useCase = new UploadShapeFileUseCase(_config.Object, _access.Object, _logger.Object);

            var actual = useCase.GetSupportedFileTypes();
            Assert.Equal(expected, actual.Value);
        }

        [Fact]
        public void GetSupportedFileTypes_Success_WhenArrayEmpty()
        {
            var expected = new string[0];
            _config.Setup(m => m.GetSupportedFileTypes()).Returns(expected);
            var useCase = new UploadShapeFileUseCase(_config.Object, _access.Object, _logger.Object);

            var actual = useCase.GetSupportedFileTypes();
            Assert.True(actual.HasValue);
            Assert.Equal(expected, actual.Value);
        }

        [Fact]
        public void GetSupportedFileTypes_handlesError()
        {
            _config.Setup(m => m.GetSupportedFileTypes()).Throws(new InvalidOperationException());

            var useCase = new UploadShapeFileUseCase(_config.Object, _access.Object, _logger.Object);
            var actual = useCase.GetSupportedFileTypes();

            Assert.False(actual.HasValue);
        }

        [Fact]
        public async Task GetShapesFromFile_ThrowHandled()
        {
            byte[] filebytes = Encoding.UTF8.GetBytes("dummy file");
            IFormFile file = new FormFile(new MemoryStream(filebytes), 0, filebytes.Length, "Data", "file.zip");

            _access.Setup(m => m.GetFeaturesFromFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                        It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<byte[]>(), It.Is<string>(r => r.Equals("shapefile")), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            var useCase = new UploadShapeFileUseCase(_config.Object, _access.Object, _logger.Object);

            var resx = await useCase.GetShapesFromFileAsync("file", ".zip", false, 0, false, 0, file, new CancellationToken());

            Assert.True(resx.IsFailure);
        }


        [Fact]
        public async Task GetShapesFromFile_Success()
        {
            string expected = "Result";
            byte[] filebytes = Encoding.UTF8.GetBytes("dummy file");
            IFormFile file = new FormFile(new MemoryStream(filebytes), 0, filebytes.Length, "Data", "file.zip");

            _access.Setup(m => m.GetFeaturesFromFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<byte[]>(), It.Is<string>(r => r.Equals("shapefile")), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("Result"));

            var useCase = new UploadShapeFileUseCase(_config.Object, _access.Object, _logger.Object);

            var resx = await useCase.GetShapesFromFileAsync("file", ".zip", false, 0, false, 0, file, new CancellationToken());

            Assert.True(resx.IsSuccess);
            Assert.Equal(expected, resx.Value);
        }

        [Fact]
        public async Task GetFeaturesFromStringAsync_ThrowHandled()
        {
            byte[] filebytes = Encoding.UTF8.GetBytes("dummy file");
            IFormFile file = new FormFile(new MemoryStream(filebytes), 0, filebytes.Length, "Data", "file.zip");

            _access.Setup(m => m.GetFeaturesFromStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                        It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(), It.Is<string>(r => r.Equals("geojson")), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            var useCase = new UploadShapeFileUseCase(_config.Object, _access.Object, _logger.Object);

            var resx = await useCase.GetShapesFromStringAsync("file", "geojson", false, 0, false, 0, "value", new CancellationToken());

            _access.VerifyAll();
            Assert.True(resx.IsFailure);
        }


        [Fact]
        public async Task GetFeaturesFromStringAsync_Success()
        {
            string expected = "Result";

            _access.Setup(m => m.GetFeaturesFromStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(), It.Is<string>(r => r.Equals("geojson")), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("Result"));

            var useCase = new UploadShapeFileUseCase(_config.Object, _access.Object, _logger.Object);

            var resx = await useCase.GetShapesFromStringAsync("file", "geojson", false, 0, false, 0, "value", new CancellationToken());

            _access.VerifyAll();
            Assert.True(resx.IsSuccess);
            Assert.Equal(expected, resx.Value);
        }
    }
}
