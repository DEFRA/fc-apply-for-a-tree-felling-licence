using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public partial class ForesterServicesTests
{
    [Fact]
    public async Task GetPhytophthoraRamorumRiskZonesAsync_ThrowsWhen_LayerNotSet()
    {
        var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser() { Password = "", Username = "", Path = "" } } });

        var caughtException = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetPhytophthoraRamorumRiskZonesAsync(_nullIsland, CancellationToken.None));
        Assert.Equal("Value cannot be null. (Parameter '_config.LayerServices')", caughtException.Message);
    }

    [Fact]
    public async Task GetPhytophthoraRamorumRiskZonesAsync_ThrowsWhen_NotSet()
    {
        var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser() { Password = "", Username = "", Path = "" }, LayerServices = new List<FeatureLayerConfig>() } });

        var result = await sut.GetPhytophthoraRamorumRiskZonesAsync(_nullIsland, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("Unable to find layer details", result.Error);
    }

    [Fact]
    public async Task GetPhytophthoraRamorumRiskZonesAsync_NoResultsTreatedAsSuccess()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"objectIdFieldName\": \"OBJECTID\",\"uniqueIdField\": {\"name\": \"OBJECTID\",\"isSystemMaintained\": true},\"globalIdFieldName\": \"GlobalID\",\"serverGens\": {\"minServerGen\": 7547713,\"serverGen\": 8462309},\"geometryType\": \"esriGeometryPolygon\",\"spatialReference\": {\"wkid\": 27700,\"latestWkid\": 27700},\"fields\": [{\"name\": \"OBJECTID\",\"type\": \"esriFieldTypeOID\",\"alias\": \"OBJECTID\",\"sqlType\": \"sqlTypeOther\",\"domain\": null,\"defaultValue\": null},{\"name\": \"LAD21CD\",\"type\": \"esriFieldTypeString\",\"alias\": \"LAD21CD\",\"sqlType\": \"sqlTypeOther\",\"length\": 9,\"domain\": null,\"defaultValue\": null},{\"name\": \"LAD21NM\",\"type\": \"esriFieldTypeString\",\"alias\": \"LAD21NM\",\"sqlType\": \"sqlTypeOther\",\"length\": 36,\"domain\": null,\"defaultValue\": null}],\"features\": []\r\n}")
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Risk/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSUT(_config);

        var response = await classUnderTest.GetPhytophthoraRamorumRiskZonesAsync(_nullIsland, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Value);
        Assert.Empty(response.Value);
    }

    [Fact]
    public async Task GetPhytophthoraRamorumRiskZonesAsync_QueryServerSuccess()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"objectIdFieldName\": \"FID\",\"uniqueIdField\": {\"name\": \"FID\",\"isSystemMaintained\": true},\"globalIdFieldName\": \"\",\"geometryType\": \"esriGeometryPolygon\",\"spatialReference\": {\"wkid\": 27700,\"latestWkid\": 27700},\"fields\": [    {\"name\": \"FID\",\"type\": \"esriFieldTypeOID\",\"alias\": \"FID\",\"sqlType\": \"sqlTypeOther\",\"domain\": null,\"defaultValue\": null    },    {\"name\": \"AREA_NAME\",\"type\": \"esriFieldTypeString\",\"alias\": \"AREA_NAME\",\"sqlType\": \"sqlTypeOther\",\"length\": 70,\"domain\": null,\"defaultValue\": null    }],\"features\": [    {\"attributes\": {\"objectid\": 1,\"name\": \"Zone 1\"}}]}")
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Risk/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSUT();

        var response = await classUnderTest.GetPhytophthoraRamorumRiskZonesAsync(_nullIsland, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Value);
        Assert.Single(response.Value);
        Assert.Equal("Zone 1", response.Value[0].ZoneName);
    }
}
