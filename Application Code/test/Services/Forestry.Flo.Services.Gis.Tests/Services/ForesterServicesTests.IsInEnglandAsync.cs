
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Moq.Protected;
using Moq;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForesterServicesTests
{
    [Fact]
    public async Task IsInEnglandAsync_ThrowsWhen_LayerNotSet()
    {
        var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser { Password = "", Path = "", Username = "" } } });

        var caughtException = await Assert.ThrowsAsync<ArgumentNullException>(() => sut.IsInEnglandAsync(_nullIsland, CancellationToken.None));

        caughtException.Message.Should().Be("Value cannot be null. (Parameter '_config.LayerServices')");
    }

    [Fact]
    public async Task IsInEnglandAsync_NoResults_NoFeatures()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"displayFieldName\": \"ctry23cd\", \"fieldAliases\": {  \"ctry23cd\": \"CTRY23CD\" }, \"fields\": [  {   \"name\": \"ctry23cd\",   \"type\": \"esriFieldTypeString\",   \"alias\": \"CTRY23CD\",   \"length\": 9  } ], \"features\": [ ]}")
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Boundary/query")),
                 ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSUT();

        var response = await classUnderTest.IsInEnglandAsync(_nullIsland, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.False(response.Value);
    }

    [Fact]
    public async Task IsInEnglandAsync_NoResults_MoreThanOneFeature()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"displayFieldName\": \"ctry23cd\", \"fieldAliases\": {  \"ctry23cd\": \"CTRY23CD\" }, \"fields\": [  {   \"name\": \"ctry23cd\",   \"type\": \"esriFieldTypeString\",   \"alias\": \"CTRY23CD\",   \"length\": 9  } ], \"features\": [{   \"attributes\": {  \"ctry23cd\": \"E92000001\" }},{   \"attributes\": {  \"ctry23cd\": \"E92000002\" }}  ]}")
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Boundary/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSUT();

        var response = await classUnderTest.IsInEnglandAsync(_nullIsland, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.False(response.Value);
    }

    [Fact]
    public async Task IsInEnglandAsync_NoResults_OneFeatureWrongCode()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(string.Concat("{ \"displayFieldName\": \"ctry23cd\", \"fieldAliases\": {  \"ctry23cd\": \"CTRY23CD\" }, \"fields\": [  {   \"name\": \"ctry23cd\",   \"type\": \"esriFieldTypeString\",   \"alias\": \"CTRY23CD\",   \"length\": 9  } ], \"features\": [{   \"attributes\": {  \"ctry23cd\": \"", _config.Forester.CountryCode.AsSpan(0, _config.Forester.CountryCode.Length - 1), "\" }}  ]}"))
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Boundary/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSUT();

        var response = await classUnderTest.IsInEnglandAsync(_nullIsland, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.False(response.Value);
    }

    [Fact]
    public async Task IsInEnglandAsync_NoResults_Success()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"displayFieldName\": \"ctry23cd\", \"fieldAliases\": {  \"ctry23cd\": \"CTRY23CD\" }, \"fields\": [  {   \"name\": \"ctry23cd\",   \"type\": \"esriFieldTypeString\",   \"alias\": \"CTRY23CD\",   \"length\": 9  } ], \"features\": [{   \"attributes\": {  \"ctry23cd\": \"" + _config.Forester.CountryCode + "\" }}  ]}")
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Boundary/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSUT();

        var response = await classUnderTest.IsInEnglandAsync(_nullIsland, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.True(response.Value);
    }
}