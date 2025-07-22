using FluentAssertions;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForesterServicesTests
{
    [Fact]
    public async Task GetWoodlandOfficerAsync_ThrowsWhen_LayerNotSet()
    {
        var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser{ Password = "", Path = "", Username = ""} } });

        var caughtException = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetWoodlandOfficerAsync(_nullIsland, CancellationToken.None));

        caughtException.Message.Should().Be("Value cannot be null. (Parameter '_config.LayerServices')");
    }

    [Fact]
    public async Task GetWoodlandOfficerAsync_ThrowsWhen_LANotSet()
    {
        var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser { Password = "", Path = "", Username = "" }, LayerServices = new List<FeatureLayerConfig>() } });

        var result = await sut.GetWoodlandOfficerAsync(_nullIsland, CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Should()
            .Be("Unable to find layer details");
    }

    [Fact]
    public async Task GetWoodlandOfficerAsync_NoResultsTreatedAsFailure()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"objectIdFieldName\": \"OBJECTID\",\"uniqueIdField\": {\"name\": \"OBJECTID\",\"isSystemMaintained\": true},\"globalIdFieldName\": \"\",\"geometryType\": \"esriGeometryPolygon\",\"spatialReference\": {\"wkid\": 27700,\"latestWkid\": 27700},\"fields\": [{\"name\": \"OBJECTID\",\"type\": \"esriFieldTypeOID\",\"alias\": \"OBJECTID\",\"sqlType\": \"sqlTypeOther\",\"domain\": null,\"defaultValue\": null},{\"name\": \"featname\",\"type\": \"esriFieldTypeString\",\"alias\": \"featname\",\"sqlType\": \"sqlTypeOther\",\"length\": 38,\"domain\": null,\"defaultValue\": null},{\"name\": \"cons\",\"type\": \"esriFieldTypeString\",\"alias\": \"cons\",\"sqlType\": \"sqlTypeOther\",\"length\": 40,\"domain\": null,\"defaultValue\": null},{\"name\": \"num\",\"type\": \"esriFieldTypeInteger\",\"alias\": \"num\",\"sqlType\": \"sqlTypeOther\",\"domain\": null,\"defaultValue\": null},{\"name\": \"fieldmanager\",\"type\": \"esriFieldTypeString\",\"alias\": \"Field Manager\",\"sqlType\": \"sqlTypeOther\",\"length\": 255,\"domain\": null,\"defaultValue\": null}],\"features\": []}")
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
        ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);
        _mockHttpHandler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/WoodlandOfficer/query")),
        ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSUT();

        var response = await classUnderTest.GetWoodlandOfficerAsync(_nullIsland, CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal("No Results found", response.Error);
    }

    [Fact]
    public async Task GetWoodlandOfficerAsync_QueryServerSuccess()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"objectIdFieldName\": \"OBJECTID\",\"uniqueIdField\": {\"name\": \"OBJECTID\",\"isSystemMaintained\": true},\"globalIdFieldName\": \"\",\"geometryType\": \"esriGeometryPolygon\",\"spatialReference\": {\"wkid\": 27700,\"latestWkid\": 27700},\"fields\": [{\"name\": \"OBJECTID\",\"type\": \"esriFieldTypeOID\",\"alias\": \"OBJECTID\",\"sqlType\": \"sqlTypeOther\",\"domain\": null,\"defaultValue\": null},{\"name\": \"featname\",\"type\": \"esriFieldTypeString\",\"alias\": \"featname\",\"sqlType\": \"sqlTypeOther\",\"length\": 38,\"domain\": null,\"defaultValue\": null},{\"name\": \"cons\",\"type\": \"esriFieldTypeString\",\"alias\": \"cons\",\"sqlType\": \"sqlTypeOther\",\"length\": 40,\"domain\": null,\"defaultValue\": null},{\"name\": \"num\",\"type\": \"esriFieldTypeInteger\",\"alias\": \"num\",\"sqlType\": \"sqlTypeOther\",\"domain\": null,\"defaultValue\": null},{\"name\": \"fieldmanager\",\"type\": \"esriFieldTypeString\",\"alias\": \"Field Manager\",\"sqlType\": \"sqlTypeOther\",\"length\": 255,\"domain\": null,\"defaultValue\": null}],\"features\": [{\"attributes\": {\"OBJECTID\": 2,\"featname\": \"H Simpson\",\"cons\": \"North West and West Milands\",\"num\": 2,\"fieldmanager\": \"Mr Burns\"}}]}")
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/WoodlandOfficer/query")),
                 ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSUT();

        var response = await classUnderTest.GetWoodlandOfficerAsync(_nullIsland, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal("Mr Burns", response.Value.OfficerName);
    }
}
