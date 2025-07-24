using System.Net;
using AutoFixture.Xunit2;
using FluentAssertions;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;


namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class BaseServicesTests

{
    [Fact]
    public async Task ConvertPointToLatLongAsync_ReturnsSuccess()
    {
        // Arrange
        var point = new Point(1.0f, 1.0f);
        var cancellationToken = CancellationToken.None;


        var projectionResponse = new ProjectionResponse<LatLongObj> {
            Geometries = [new LatLongObj { Latitude = 1.0f, Longitude = 1.0f }]
        };
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/geom/project")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(new ProjectionResponse<LatLongObj> {
                        Geometries = [new LatLongObj { Latitude = 1f, Longitude = 1f }]
                    }))
                }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSut(geometryService:  new GeometryServiceSettings {
            Path = "https://www.AGOL.com/geom/",
            IsPublic = true,
            NeedsToken = false,
            IntersectService = new BaseEsriServiceConfig {
                Path = "intersect"
            },
            UnionService = new BaseEsriServiceConfig {
                Path = "union"
            },
            ProjectService = new ProjectServiceSettings {
                Path = "project",
                OutSR = 1
            },
        },spatialReference: 27700);

        // Act
        var result = await sut.ConvertPointToLatLongAsync(point, cancellationToken);

        _mockHttpHandler.VerifyAll();
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1.0f, result.Value.Latitude);
        Assert.Equal(1.0f, result.Value.Longitude);
    }

    [Fact]
    public async Task ConvertPointToLatLongAsync_ThrowsWhen_GeometeryConfigNotSet()
    {
        var access = CreateSut( spatialReference: 27700);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            access.ConvertPointToLatLongAsync(new(), CancellationToken.None));

    }

    [Fact]
    public async Task ConvertPointToLatLongAsync_ThrowsWhen_ProjectConfigNotSet()
    {
        var access = CreateSut(geometryService: _geometryServiceSettings);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            access.ConvertPointToLatLongAsync(new(), CancellationToken.None));

    }

    [Fact]
    public async Task ConvertPointToLatLongAsync_ThrowsWhen_SpatialReferenceNotSet()
    {
        var access = CreateSut(geometryService: _geometryServiceSettings);
        await Assert.ThrowsAsync<ArgumentException>(() => access.ConvertPointToLatLongAsync(new(), CancellationToken.None));

    }

    [Theory, AutoData]
    public async Task ConvertPointToLatLongAsync_EmptyArrayIsFailure(Point point)
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/geom/project")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(new ProjectionResponse<LatLongObj>() { Geometries = [] }))
                }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var access = CreateSut(geometryService: _geometryServiceSettings,spatialReference:27700);
        var response = await access.ConvertPointToLatLongAsync(point, CancellationToken.None);


        response.IsFailure.Should().BeTrue();
        _mockHttpHandler.VerifyAll();
    }

    [Theory, AutoData]
    public async Task ConvertPointToLatLongAsync_Success(Point point)
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/geom/project")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(new ProjectionResponse<LatLongObj>() { Geometries = new() { new LatLongObj { Longitude = 1, Latitude = 2 } } }))
                }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var access = CreateSut(geometryService: _geometryServiceSettings , spatialReference: 27700);
        var response = await access.ConvertPointToLatLongAsync(point, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        _mockHttpHandler.VerifyAll();
    }
}