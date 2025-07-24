using System.Net;
using FluentAssertions;
using Forestry.Flo.Services.Gis.Models.Esri.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public partial class BaseServicesTests
{

    [Fact]
    public async Task GetUnionedPolygonAsync_ThrowsWhen_GeometryServiceConfigNotSet()
    {
        var access = CreateSut();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
                 access.GetUnionedPolygonAsync(new List<string>(), CancellationToken.None));

    }

    [Fact]
    public async Task GetUnionedPolygonAsync_ThrowsWhen_MergeServiceConfigNotSet()
    {

        var access = CreateSut(geometryService: new GeometryServiceSettings());
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            access.GetUnionedPolygonAsync(new List<string>(), CancellationToken.None));
    }

    [Fact]
    public async Task GetUnionedPolygonAsync_ThrowsWhen_SpatialReferenceNotSet()
    {
        var access = CreateSut(geometryService: _geometryServiceSettings);
        await Assert.ThrowsAsync<ArgumentException>(() => access.GetUnionedPolygonAsync(new List<string>(), CancellationToken.None));

    }

    [Fact]
    public async Task GetUnionedPolygonAsync_Success()
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/geom/union")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(new Geometry<Polygon> { Shape = _polygons[0] }))
                }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));
        var access = CreateSut(geometryService: _geometryServiceSettings, spatialReference: 27700);
        var response = await access.GetUnionedPolygonAsync(_polygons.Select(p => p.GetGeometrySimple()).ToList(), CancellationToken.None);


        response.IsSuccess.Should().BeTrue();
        _mockHttpHandler.VerifyAll();
    }
}
