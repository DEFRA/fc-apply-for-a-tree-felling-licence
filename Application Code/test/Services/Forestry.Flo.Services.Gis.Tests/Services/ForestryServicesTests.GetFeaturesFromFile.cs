using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Moq;
using Moq.Protected;
using System.Net;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public partial class ForestryServicesTests
{
    [Fact]
    public async Task GetFeaturesFromFile_ThrowsWhen_FileEmpty()
    {

        var sut = CreateSut(new EsriConfig() { Forestry = new ForestryConfig { GenerateTokenService = new OAuthServiceSettingsClient { ClientID = "", Path = "", ClientSecret = "" } } });

        var caughtException = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetFeaturesFromFileAsync("", "", false, 0, false, 0, [], "", new CancellationToken()));
        Assert.Equal("Required input No valid file contents set was empty. (Parameter 'No valid file contents set')", caughtException.Message);
    }

    [Fact]
    public async Task GetFeaturesFromFile_ThrowsWhen_GenerateNotSet()
    {
        var sut = CreateSut(new EsriConfig() { Forestry = new ForestryConfig { GenerateTokenService = new OAuthServiceSettingsClient { ClientID = "", Path = "", ClientSecret = "" } }, SpatialReference = 1 });

        var caughtException = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetFeaturesFromFileAsync("", "", false, 0, false, 0, new byte[1], "", new CancellationToken()));
        Assert.Equal("Value cannot be null. (Parameter '_config.FeaturesService')", caughtException.Message);
    }

    [Fact]
    public async Task GetFeaturesFromFile_ThrowsWhen_SpatialNotSet()
    {
        var sut = CreateSut(
                new EsriConfig {
                    Forestry = new ForestryConfig {
                        FeaturesService = new FeaturesServiceSettings { GenerateService = new GenerateServiceSettings() },
                        GenerateTokenService = new OAuthServiceSettingsClient { ClientID = "", Path = "", ClientSecret = "" }
                    }
                });

        var caughtException = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetFeaturesFromFileAsync("", "", false, 0, false, 0, new byte[1], "", new CancellationToken()));
        Assert.Equal("Required input SpatialReference cannot be zero. (Parameter 'SpatialReference')", caughtException.Message);
    }

    [Fact]
    public async Task GetFeaturesFromFile_ReturnsSuccess()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/feat/gen?f=json&filetype=&publishParameters={\"name\":\".\",\"targetSR\":{\"wkid\":2270},\"maxRecordCount\":100,\"enforceInputFileSizeLimit\":false,\"enforceOutputJsonSizeLimit\":false}")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{'result':'pass'}"),
                }).Verifiable();


        var httpClient = new HttpClient(_mockHttpHandler.Object);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var access = CreateSut();

        var resx = await access.GetFeaturesFromFileAsync("", "", false, 0, false, 0, new byte[1], "", new CancellationToken());
        _mockHttpHandler.VerifyAll();
    }
}
