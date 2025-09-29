using Forestry.Flo.Services.Gis.Models.Internal;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class PublicRegisterTests
{
    [Fact]
    public void ShowCaseOnConsultationRegisterAsync_EmptyCompartmentsThrow()
    {

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var caughtException =
          Assert.ThrowsAsync<ArgumentException>(() => sut.AddCaseToConsultationRegisterAsync("C-NP-AB-Test-18-10-22", "Pauls Garden", "Type", "Grid", "Swindon", "Swindon", "Wiltshire", DateTime.Now, 90, 0, 0, 0, 0, new List<InternalCompartmentDetails<Polygon>>(), CancellationToken.None));

    }

    [Fact]
    public async Task ShowCaseOnConsultationRegisterAsync_DeleteServer_Failure_CheckResponse()
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successAddBoundary).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway }).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/deleteFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_emptyMessage).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.AddCaseToConsultationRegisterAsync("C-NP-AB-Test-18-10-22", "Pauls Garden", "Type", "Grid", "Swindon", "Swindon", "Wiltshire", DateTime.Now, 90, 0, 0, 0, 0, _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Added Case Boundary, but failed to add compartments. Unable to rollback Boundary", response.Error);
    }

    [Fact]
    public async Task ShowCaseOnConsultationRegisterAsync_DeleteServerSuccess()
    {
        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"deleteResults\": [{\"objectId\": 801,\"globalId\": \"{10D14F7F-D2C6-4B99-875C-CEB39B248F78}\",\"success\": true}]}")
        };

        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successAddBoundary).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway }).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/deleteFeatures")),
                ItExpr.IsAny<CancellationToken>()).Throws(new Exception("This is an error message")).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.AddCaseToConsultationRegisterAsync("C-NP-AB-Test-18-10-22", "Pauls Garden", "Type", "Grid", "Swindon", "Swindon", "Wiltshire", DateTime.Now, 90, 0, 0, 0, 0, _compartments, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Added Case Boundary, but failed to add compartments. Unable to rollback Boundary", response.Error);
    }

    [Fact]
    public async Task ShowCaseOnConsultationRegisterAsync_Success()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successAddBoundary).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successAddBoundary).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.AddCaseToConsultationRegisterAsync("C-NP-AB-Test-18-10-22", "Pauls Garden", "Type", "Grid", "Swindon", "Swindon", "Wiltshire", DateTime.Now, 90, 0, 0, 0, 0, _compartments, CancellationToken.None);

        Assert.True(response.IsSuccess);
    }
}
