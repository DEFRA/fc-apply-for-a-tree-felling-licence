using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForestryServicesTests
{
    [Theory]
    [InlineData(51.557540, -1.7366200, "SU183843")]
    public async Task GetOSGridReference_Success(float lat, float lon, string expectedResult)
    {
        _mockHttpHandler.Reset();


        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/geom/project")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new ProjectionResponse<LatLongObj>() { Geometries = [new LatLongObj { Longitude = lon, Latitude = lat }] }))
            }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var access = CreateSut();

        var response = await access.GetOSGridReferenceAsync(_polygons[0].GetCenterPoint()!, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(expectedResult, response.Value);
    }

    [Theory]
    [InlineData(51.557540, -1.7366200)]
    public async Task GetOSGridReference_Failure(float lat, float lon)
    {
        _mockHttpHandler.Reset();


        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/geom/project")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var access = CreateSut();

        var response = await access.GetOSGridReferenceAsync(_polygons[0].GetCenterPoint()!, CancellationToken.None);

        Assert.True(response.IsFailure);
    }
}
