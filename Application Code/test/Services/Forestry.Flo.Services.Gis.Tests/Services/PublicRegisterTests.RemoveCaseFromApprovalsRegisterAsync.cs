using FluentAssertions;
using Forestry.Flo.Services.Gis.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class PublicRegisterTests
{
    [Fact]
    public async Task RemoveCaseFromApprovalsRegisterAsync_GuardCheck_EsriID()
    {
        _mockHttpHandler.Reset();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var result = await sut.RemoveCaseFromDecisionRegisterAsync(0, "case", CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Object ID not correctly set");
    }

    [Fact]
    public async Task RemoveCaseFromApprovalsRegisterAsync_Check_CaseRef()
    {
        _mockHttpHandler.Reset();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var result = await sut.RemoveCaseFromDecisionRegisterAsync(3, "", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("No Case Reference given");
    }


    [Fact]
    public async Task RemoveCaseFromApprovalsRegisterAsync_UpdateServer_Fails_checkReturn()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.RemoveCaseFromDecisionRegisterAsync(3, "case", CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be("Unable to connect to the esri service");
    }

    [Fact]
    public async Task RemoveCaseFromApprovalsRegisterAsync_QueryServerFails_checkReturn()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successUpdate).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.RemoveCaseFromDecisionRegisterAsync(3, "case", CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be("Updated the Boundary, Updating Compartments returned 'Unable to query the compartments layer'");
    }

    [Fact]
    public async Task RemoveCaseFromApprovalsRegisterAsync_UpdateCompartmentserverEmptyMessage()
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successUpdate).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successQuery).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_emptyMessage).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.RemoveCaseFromDecisionRegisterAsync(3, "case", CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be("Updated the Boundary, Updating Compartments returned 'Empty message from ESRI'");
    }

    [Fact]
    public async Task RemoveCaseFromApprovalsRegisterAsync_UpdateCompartmentserverNullUpdates()
    {
        var message = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"addResults\": [] }")
        };

        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successUpdate).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successQuery).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(message).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.RemoveCaseFromDecisionRegisterAsync(3, "case", CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be("Updated the Boundary, Updating Compartments returned 'No Results'");
    }

    [Fact]
    public async Task RemoveCaseFromApprovalsRegisterAsync_UpdateCompartmentsServer_Fails_checkReturn()
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successUpdate).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successQuery).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.RemoveCaseFromDecisionRegisterAsync(3, "case", CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be("Updated the Boundary, Updating Compartments returned 'Unable to connect to the esri service'");
    }


    [Fact]
    public async Task RemoveCaseFromApprovalsRegisterAsync_Success()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Boundaries/updateFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successUpdate).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successQuery).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/Compartments/updateFeatures")),
                             ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successUpdate).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.RemoveCaseFromDecisionRegisterAsync(3, "case", CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
    }
}
