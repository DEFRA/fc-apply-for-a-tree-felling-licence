using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;

namespace Forestry.Flo.Services.Gis.Tests.Services
{
    public partial class ForesterServicesTests
    {

        [Fact]
        public async Task GetAncientWoodlandsRevisedAsync_ThrowsWhen_LayerNotSet()
        {
            var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser() { Password = "", Username = "", Path = "" } } });

            var caughtException = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAncientWoodlandsRevisedAsync(_nullIsland, CancellationToken.None));
            Assert.Equal("Value cannot be null. (Parameter '_config.LayerServices')", caughtException.Message);
        }

        [Fact]
        public async Task GetAncientWoodlandsRevisedAsync_ThrowsWhen_LANotSet()
        {
            var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser() { Password = "", Username = "", Path = "" }, LayerServices = new List<FeatureLayerConfig>() } });

            var result = await sut.GetAncientWoodlandsRevisedAsync(_nullIsland, CancellationToken.None);
            Assert.True(result.IsFailure);
            Assert.Equal("Unable to find layer details", result.Error);
        }


        [Fact]
        public async Task GetAncientWoodlandsRevisedAsync_QueryServerSuccess()
        {

            var returnMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"objectIdFieldName\": \"FID\",\"uniqueIdField\": {\"admin_hub\": \"FID\",\"isSystemMaintained\": true},\"globalIdFieldName\": \"\",\"geometryType\": \"esriGeometryPolygon\",\"spatialReference\": {\"wkid\": 27700,\"latestWkid\": 27700},\"fields\": [    {\"admin_hub\": \"FID\",\"type\": \"esriFieldTypeOID\",\"alias\": \"FID\",\"sqlType\": \"sqlTypeOther\",\"domain\": null,\"defaultValue\": null    },    {\"name\": \"AREA_NAME\",\"type\": \"esriFieldTypeString\",\"alias\": \"AREA_NAME\",\"sqlType\": \"sqlTypeOther\",\"length\": 70,\"domain\": null,\"defaultValue\": null    }],\"features\": [    {\"attributes\": {\"objectid\": 1,\"area_code\": \"cs\"}}]}")
            };
            returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            _mockHttpHandler.Reset();

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                    ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/AWR/query")),
                    ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

            var classUnderTest = CreateSUT();

            var response = await classUnderTest.GetAncientWoodlandsRevisedAsync(_nullIsland, CancellationToken.None);

            Assert.True(response.IsSuccess);
            Assert.NotEmpty(response.Value);
        }
    }
}
