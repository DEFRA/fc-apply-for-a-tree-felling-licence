using Forestry.Flo.Services.Gis.Models.Esri.Common;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;

namespace Forestry.Flo.Services.Gis.Tests.Services
{
    public partial class ForestryServicesTests
    {

        [Fact]
        public async Task CalculateCentrePointAsync_MergeFails()
        {
            _mockHttpHandler.Reset();

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/geom/union")),
                    ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                }).Verifiable();

            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

            var sut = CreateSut();
            var response = await sut.CalculateCentrePointAsync(_polygons.Select(p => p.GetGeometrySimple()).ToList(), CancellationToken.None);

            _mockHttpHandler.VerifyAll();
            Assert.True(response.IsFailure);
            Assert.Equal("Unable to connect to the esri service", response.Error);
        }


        [Fact]
        public async Task CalculateCentrePointAsync_Success()
        {
            _mockHttpHandler.Reset();


            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/geom/union")),
                    ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(new Geometry<Polygon> { Shape = _polygons[0] }))
                }).Verifiable();


            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

            var access = CreateSut();
            var response = await access.CalculateCentrePointAsync(_polygons.Select(p => p.GetGeometrySimple()).ToList(), CancellationToken.None);

            _mockHttpHandler.VerifyAll();
            Assert.True(response.IsSuccess);
            Assert.Equal(4.2f, response.Value.X);
            Assert.Equal(4.1f, response.Value.Y);
        }
    }
}
