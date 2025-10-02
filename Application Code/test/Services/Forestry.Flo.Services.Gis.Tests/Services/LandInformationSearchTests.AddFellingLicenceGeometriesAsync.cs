using System.Net;
using Moq;
using Moq.Protected;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class LandInformationSearchTests
{
    [Fact]
    public async Task AddFellingLicenceGeometriesAsync_ThrowsArgumentException_WhenPolygonsEmpty()
    {
        var sut = CreateSUT();
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.AddFellingLicenceGeometriesAsync(Guid.NewGuid(), [], CancellationToken.None));

        Assert.Contains("Required input polygons was empty", ex.Message);
        Assert.Equal("polygons", ex.ParamName);
    }

    [Fact]
    public async Task AddCaseToDecisionRegisterAsync_TokenFails()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway });

        _mockHttpHandler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/addFeatures")),
                ItExpr.IsAny<CancellationToken>());

        var sut = CreateSUT();

        var response = await sut.AddFellingLicenceGeometriesAsync(Guid.NewGuid(), _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task AddCaseToDecisionRegisterAsync_AddReturnsBadGateway()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.AddFellingLicenceGeometriesAsync(Guid.NewGuid(), _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task AddCaseToDecisionRegisterAsync_AddReturnsHTML()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_htmlMessage).Verifiable();

        var sut = CreateSUT();

        var response = await sut.AddFellingLicenceGeometriesAsync(Guid.NewGuid(), _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task AddCaseToDecisionRegisterAsync_AddReturnsEmptyResult()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.AddFellingLicenceGeometriesAsync(Guid.NewGuid(), _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task AddCaseToDecisionRegisterAsync_AddReturnsEmptyAddResult()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"addResults\":[]}")
            }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.AddFellingLicenceGeometriesAsync(Guid.NewGuid(), _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task AddCaseToDecisionRegisterAsync_AddReturnsFailedResult()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"addResults\":[{\"objectId\":1048,\"uniqueId\":1048,\"globalId\":null,\"success\":false}]}")
            }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.AddFellingLicenceGeometriesAsync(Guid.NewGuid(), _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task AddCaseToDecisionRegisterAsync_Success()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"addResults\":[{\"objectId\":1048,\"uniqueId\":1048,\"globalId\":null,\"success\":true}]}")
            }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.AddFellingLicenceGeometriesAsync(Guid.NewGuid(), _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsSuccess);
    }
}
