using System.Net;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Newtonsoft.Json;
using Moq;
using Forestry.Flo.Services.Gis.Models.Internal;
using Moq.Protected;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForesterServicesTests
{
    public InternalCompartmentDetails<BaseShape> ShapeDetails = new()
    {
        CompartmentLabel = "A",
        CompartmentNumber = "1",
        SubCompartmentNo = "1"
    };

    private string shape =
        "{\"spatialReference\":{\"wkid\":27700},\"rings\": [[[359125.0454550035, 173149.57548723381],[359170.4299405478, 173168.08758002162],[359177.5959119495, 173171.073401439],[359176.998747666, 173171.073401439],[359179.98456908343, 173164.50459432075],[359184.76188335125, 173129.27190159555],[359180.5817333669, 173117.328615926],[359175.80441909906, 173128.0775730286],[359165.05546199647, 173126.88324446164],[359161.4724762956, 173128.67473731207],[359139.3773978069, 173118.52294449296],[359125.04545500345, 173149.57548723381]]]}";

    [Fact]
    public async Task GenerateCompartmentImageAsync_ReturnsNoExtent()
    {
        ShapeDetails.ShapeGeometry = new Polygon();

        var sut = CreateSUT();
        var result =
            await sut.GenerateImage_SingleCompartmentAsync(ShapeDetails, CancellationToken.None, 100,
                MapGeneration.Other, "");

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to calculate Extend", result.Error);
    }

    [Fact]
    public async Task GenerateCompartmentImageAsync_PostQueryWithConversionAsyncFails()
    {

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/Export")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_mockHttpHandler.Object));


        ShapeDetails.ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(shape);

        var sut = CreateSUT();
        var result =
            await sut.GenerateImage_SingleCompartmentAsync(ShapeDetails, CancellationToken.None, 100,
                MapGeneration.Other, "");

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to connect to the esri service", result.Error);
        _mockHttpHandler.VerifyAll();
    }


    [Fact]
    public async Task GenerateCompartmentImageAsync_GetImageFails()
    {

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/Export")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK, 
                Content = new StringContent(
                    "{\"jobId\": \"1\", \"jobStatus\" : \"esriJobSubmitted\" }")
            }).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/jobs/1")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"jobId\": \"1\", \"jobStatus\" : \"Succeeded\", \"results\":{ \"Output_File\": { \"paramUrl\": \"ready\"}} \r\n}")
            }).Verifiable();


        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/jobs/1/ready")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"paramName\" :\"1\", \"dataType\" : \"file\",  \"value\": { \"url\" :\"https://paul.com/image.png\" }  \r\n}")
            }).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://paul.com/image.png")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_mockHttpHandler.Object));


        ShapeDetails.ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(shape);

        var sut = CreateSUT();
        var result =
            await sut.GenerateImage_SingleCompartmentAsync(ShapeDetails, CancellationToken.None, 100,
                MapGeneration.Other, "");

        Assert.True(result.IsFailure);
        _mockHttpHandler.VerifyAll();
    }

    [Fact]
    public async Task GenerateCompartmentImageAsync_CheckJobStatusFails()
    {

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/Export")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"jobId\": \"1\", \"jobStatus\" : \"esriJobSubmitted\" }")
            }).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/jobs/1")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_mockHttpHandler.Object));

        ShapeDetails.ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(shape);

        var sut = CreateSUT();
        var result =
            await sut.GenerateImage_SingleCompartmentAsync(ShapeDetails, CancellationToken.None, 100,
                MapGeneration.Other, "");

        Assert.True(result.IsFailure);
        _mockHttpHandler.VerifyAll();
    }

    [Fact]
    public async Task GenerateCompartmentImageAsync_CheckStatusFails()
    {

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/Export")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"jobId\": \"1\", \"jobStatus\" : \"esriJobSubmitted\" }")
            }).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/jobs/1")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "{\"jobId\": \"1\", \"jobStatus\" : \"Succeeded\", \"results\":{ \"Output_File\": { \"paramUrl\": \"ready\"}} \r\n}")
            }).Verifiable();


        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/Utils/jobs/1/ready")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_mockHttpHandler.Object));


        ShapeDetails.ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(shape);

        var sut = CreateSUT();
        var result =
            await sut.GenerateImage_SingleCompartmentAsync(ShapeDetails, CancellationToken.None, 100,
                MapGeneration.Other, "");

        Assert.True(result.IsFailure);
        _mockHttpHandler.VerifyAll();
    }
}
