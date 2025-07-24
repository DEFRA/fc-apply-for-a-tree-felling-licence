using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using FluentAssertions;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForestryServicesTests
{
    [Fact]
    public async Task GetFeaturesFromStringAsync_ThrowsWhen_FileEmpty()
    {

        var sut = CreateSut(new EsriConfig() { Forestry = new ForestryConfig { GenerateTokenService = new OAuthServiceSettingsClient { ClientID = "", Path = "", ClientSecret = "" } } });
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();
        var caughtException = await Assert.ThrowsAsync<ArgumentException>(() => sut.GetFeaturesFromStringAsync("", "", false, 0, false, 0, "", "", new CancellationToken()));
        caughtException.Message.Should().Be("Required input No valid text set was empty. (Parameter 'No valid text set')");
    }

    [Fact]
    public async Task GetFeaturesFromStringAsync_ThrowsWhen_GenerateNotSet()
    {
        var sut = CreateSut(new EsriConfig() { Forestry = new ForestryConfig { GenerateTokenService = new OAuthServiceSettingsClient { ClientID = "", Path = "", ClientSecret = "" } }, SpatialReference = 1 });

        var caughtException = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetFeaturesFromStringAsync("", "", false, 0, false, 0, "{}", "", new CancellationToken()));
        caughtException.Message.Should().Be("Value cannot be null. (Parameter '_config.FeaturesService')");
    }

    [Fact]
    public async Task GetFeaturesFromStringAsync_ThrowsWhen_SpatialNotSet()
    {
        var access = CreateSut(
                new EsriConfig {
                    Forestry = new ForestryConfig {
                        FeaturesService = new FeaturesServiceSettings { GenerateService = new GenerateServiceSettings() },
                        GenerateTokenService = new OAuthServiceSettingsClient { ClientID = "", Path = "", ClientSecret = "" }
                    }
                });

        var caughtException = await Assert.ThrowsAsync<ArgumentException>(() => access.GetFeaturesFromStringAsync("", "", false, 0, false, 0, "{}", "", new CancellationToken()));
        caughtException.Message.Should().Be("Required input SpatialReference cannot be zero. (Parameter 'SpatialReference')");
    }


    [Fact]
    public async Task GetFeaturesFromStringAsync_ReturnsSuccess()
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/feat/gen")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{'result':'pass'}"),
                }).Verifiable();


        var httpClient = new HttpClient(_mockHttpHandler.Object);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var sut = CreateSut();

        var resx = await sut.GetFeaturesFromStringAsync("", "", false, 0, false, 0, "{}", "", new CancellationToken());

        resx.IsSuccess.Should().BeTrue();
        resx.Value.Should().Be("{'result':'pass'}");
        _mockHttpHandler.VerifyAll();
    }
}
