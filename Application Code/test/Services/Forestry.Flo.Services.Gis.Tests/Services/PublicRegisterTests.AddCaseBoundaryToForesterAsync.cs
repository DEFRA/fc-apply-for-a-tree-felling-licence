using AutoFixture.Xunit2;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class PublicRegisterTests
{
    [Theory, AutoData]
    public void ShowCaseOnConsultationRegisterAsync_EmptyCompartmentsThrow(AddToPublicRegisterModel model)
    {
        model.Compartments = [];

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var caughtException =
          Assert.ThrowsAsync<ArgumentException>(() => sut.AddCaseToConsultationRegisterAsync(model, CancellationToken.None));

    }

    [Theory, AutoData]
    public async Task ShowCaseOnConsultationRegisterAsync_DeleteServer_Failure_CheckResponse(AddToPublicRegisterModel model)
    {
        model.ExistingEsriId = null;
        model.Compartments = _compartments;

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

        var response = await sut.AddCaseToConsultationRegisterAsync(model, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Added Case Boundary, but failed to add compartments. Unable to rollback Boundary", response.Error);
    }

    [Theory, AutoData]
    public async Task ShowCaseOnConsultationRegisterAsync_DeleteServerSuccess(AddToPublicRegisterModel model)
    {
        model.ExistingEsriId = null;
        model.Compartments = _compartments;

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
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successDeleteCompartment).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.AddCaseToConsultationRegisterAsync(model, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Added Case Boundary, but failed to add compartments. Boundary has been rolled back", response.Error);
    }

    [Theory, AutoData]
    public async Task ShowCaseOnConsultationRegisterAsync_Success(AddToPublicRegisterModel model)
    {
        model.ExistingEsriId = null;
        model.Compartments = _compartments;

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

        var response = await sut.AddCaseToConsultationRegisterAsync(model, CancellationToken.None);

        Assert.True(response.IsSuccess);
    }

    [Theory, AutoData]
    public async Task ShowCaseOnConsultationRegisterAsync_WhenRepublishing_Success(AddToPublicRegisterModel model)
    {
        model.ExistingEsriId = 1;
        model.Compartments = _compartments;

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successQuery).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successQuery).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/deleteFeatures")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(_successDeleteCompartment);

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successUpdate).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successAddBoundary).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.AddCaseToConsultationRegisterAsync(model, CancellationToken.None);

        Assert.True(response.IsSuccess);
    }

    [Theory, AutoData]
    public async Task ShowCaseOnConsultationRegisterAsync_WhenRepublishing_NotFoundOnLayerAnyMore_Success(AddToPublicRegisterModel model)
    {
        model.ExistingEsriId = 2;
        model.Compartments = _compartments;

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successQuery).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successAddBoundary).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successAddBoundary).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.AddCaseToConsultationRegisterAsync(model, CancellationToken.None);

        Assert.True(response.IsSuccess);
    }
}
