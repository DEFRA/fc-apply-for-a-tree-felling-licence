using Moq.Protected;
using Moq;
using System.Net;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class LandInformationSearchTests
{
    [Fact]
    public async Task GetCompartmentIdsAsync_TokenFails()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway });

        _mockHttpHandler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/query")),
                ItExpr.IsAny<CancellationToken>());

        var sut = CreateSUT();

        var response = await sut.GetCompartmentIdsAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task GetCompartmentIdsAsync_QueryReturnsBadGateway()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.GetCompartmentIdsAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task GetCompartmentIdsAsync_QueryReturnsHTML()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_htmlMessage).Verifiable();

        var sut = CreateSUT();

        var response = await sut.GetCompartmentIdsAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsFailure);
    }

    [Fact]
    public async Task GetCompartmentIdsAsync_QueryReturnsEmptyResult()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"objectIdFieldName\":\"OBJECTID\",\"uniqueIdField\":{\"name\":\"OBJECTID\",\"isSystemMaintained\":true},\"globalIdFieldName\":\"\",\"geometryType\":\"esriGeometryPolygon\",\"spatialReference\":{\"wkid\":27700,\"latestWkid\":27700},\"fields\":[{\"name\":\"OBJECTID\",\"type\":\"esriFieldTypeOID\",\"alias\":\"OBJECTID\",\"sqlType\":\"sqlTypeOther\",\"domain\":null,\"defaultValue\":null}],\"features\":[]}")
                }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.GetCompartmentIdsAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsSuccess);
        Assert.Empty(response.Value);
    }


    [Fact]
    public async Task GetCompartmentIdsAsync_Success()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"objectIdFieldName\":\"OBJECTID\",\"uniqueIdField\":{\"name\":\"OBJECTID\",\"isSystemMaintained\":true},\"globalIdFieldName\":\"\",\"geometryType\":\"esriGeometryPolygon\",\"spatialReference\":{\"wkid\":27700,\"latestWkid\":27700},\"fields\":[{\"name\":\"OBJECTID\",\"type\":\"esriFieldTypeOID\",\"alias\":\"OBJECTID\",\"sqlType\":\"sqlTypeOther\",\"domain\":null,\"defaultValue\":null}],\"features\":[{\"attributes\":{\"OBJECTID\":1047}},{\"attributes\":{\"OBJECTID\":1048}}]}")
                }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.GetCompartmentIdsAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        Assert.True(response.IsSuccess);

        Assert.Equal(2, response.Value.Count);
        Assert.Equal(1047, response.Value[0]);
        Assert.Equal(1048, response.Value[1]);
    }
}

