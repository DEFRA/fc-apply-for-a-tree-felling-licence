using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Tests.Services
{
    public partial class ForesterServicesTests
    {

        [Fact]
        public async Task GetAncientWoodlandsAsync_ThrowsWhen_LayerNotSet()
        {
            var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser() { Password = "", Username = "", Path = "" } } });

            var caughtException = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAncientWoodlandAsync(_nullIsland, CancellationToken.None));
            Assert.Equal("Value cannot be null. (Parameter '_config.LayerServices')", caughtException.Message);
        }

        [Fact]
        public async Task GetAncientWoodlandsAsync_ThrowsWhen_LANotSet()
        {
            var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser() { Password = "", Username = "", Path = "" }, LayerServices = new List<FeatureLayerConfig>() } });

            var result = await sut.GetAncientWoodlandAsync(_nullIsland, CancellationToken.None);
            Assert.True(result.IsFailure);
            Assert.Equal("Unable to find layer details", result.Error);
        }


        [Theory, AutoData]
        public async Task GetAncientWoodlandAsync_QueryServerSuccess(BaseQueryResponse<AncientWoodland> queryResponse)
        {
            var serialisedResponse = JsonConvert.SerializeObject(queryResponse);
            var returnMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(serialisedResponse)
            };
            returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            _mockHttpHandler.Reset();

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                    ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/AW/query")),
                    ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

            var classUnderTest = CreateSUT();

            var response = await classUnderTest.GetAncientWoodlandAsync(_nullIsland, CancellationToken.None);

            Assert.True(response.IsSuccess);
            Assert.NotEmpty(response.Value);

            var expectedResults = queryResponse.Results.Select(x => x.Record).ToList();
            Assert.Equivalent(expectedResults, response.Value);
        }

        [Theory, AutoData]
        public async Task GetAncientWoodlandAsync_QueryServerSuccess_ZeroResults(BaseQueryResponse<AncientWoodland> queryResponse)
        {
            queryResponse.Results = [];
            var serialisedResponse = JsonConvert.SerializeObject(queryResponse);
            var returnMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(serialisedResponse)
            };
            returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            _mockHttpHandler.Reset();

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                    ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/AW/query")),
                    ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

            var classUnderTest = CreateSUT();

            var response = await classUnderTest.GetAncientWoodlandAsync(_nullIsland, CancellationToken.None);

            Assert.True(response.IsSuccess);
            Assert.Empty(response.Value);
        }
    }
}
