using System.Net;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public partial class LandInformationSearchTests
{

    [Fact]
    public async Task ClearLayerAsync_TokenFails()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway });

        _mockHttpHandler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/query")),
                ItExpr.IsAny<CancellationToken>());

        _mockHttpHandler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/deleteFeatures")),
                ItExpr.IsAny<CancellationToken>());
        var sut = CreateSUT();

        var response = await sut.ClearLayerAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        response.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ClearLayerAsync_QueryReturnsBadGateway()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/token")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, }).Verifiable();

        _mockHttpHandler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/deleteFeatures")),
                ItExpr.IsAny<CancellationToken>());

        var sut = CreateSUT();

        var response = await sut.ClearLayerAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        response.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ClearLayerAsync_QueryEmptyList()
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

        _mockHttpHandler.Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/deleteFeatures")),
                ItExpr.IsAny<CancellationToken>());

        var sut = CreateSUT();

        var response = await sut.ClearLayerAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ClearLayerAsync_DeleteReturnsFalse()
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

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/deleteFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":false}")
                }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.ClearLayerAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        response.IsFailure.Should().BeTrue();

    }

    [Fact]
    public async Task ClearLayerAsync_DeleteReturnsTrue()
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

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://example.com/FeatureServer/0/deleteFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":true}")
                }).Verifiable();

        var sut = CreateSUT();

        var response = await sut.ClearLayerAsync("", CancellationToken.None);

        _mockHttpHandler.VerifyAll();
        response.IsSuccess.Should().BeTrue();

    }
}

