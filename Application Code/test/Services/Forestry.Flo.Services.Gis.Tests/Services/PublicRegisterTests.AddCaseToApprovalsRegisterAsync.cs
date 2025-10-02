using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public partial class PublicRegisterTests
{
    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]

    public async Task AddCaseToDecisionRegisterAsync_Check_EsriID(string fellingLicenceOutcome)
    {
        _mockHttpHandler.Reset();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var result = await sut.AddCaseToDecisionRegisterAsync(0, "case", fellingLicenceOutcome, DateTime.Now, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("Object ID not correctly set", result.Error);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_Check_CaseRef(string fellingLicenceOutcome)
    {
        _mockHttpHandler.Reset();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var result = await sut.AddCaseToDecisionRegisterAsync(3, "", fellingLicenceOutcome, DateTime.Now, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("No Case Reference given", result.Error);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_UpdateServer_Failure_check(string fellingLicenceOutcome)
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

        var response = await sut.AddCaseToDecisionRegisterAsync(3, "case", fellingLicenceOutcome, DateTime.Now, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Unable to connect to the esri service", response.Error);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_QueryServer_Failure_check(string fellingLicenceOutcome)
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

        var response = await sut.AddCaseToDecisionRegisterAsync(3, "case", fellingLicenceOutcome, DateTime.Now, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Updated the Boundary, Updating Compartments returned 'Unable to query the compartments layer'", response.Error);
    }


    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_UpdateCompartmentServer_Failure_check(string fellingLicenceOutcome)
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

        var response = await sut.AddCaseToDecisionRegisterAsync(3, "case", fellingLicenceOutcome, DateTime.Now, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Updated the Boundary, Updating Compartments returned 'Empty message from ESRI'", response.Error);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_UpdateCompartmentServerNullUpdates(string fellingLicenceOutcome)
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

        var response = await sut.AddCaseToDecisionRegisterAsync(3, "case", fellingLicenceOutcome, DateTime.Now, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Updated the Boundary, Updating Compartments returned 'No Results'", response.Error);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_Success(string fellingLicenceOutcome)
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

        var response = await sut.AddCaseToDecisionRegisterAsync(3, "case", fellingLicenceOutcome, DateTime.Now, CancellationToken.None);

        Assert.True(response.IsSuccess);
    }

    [Fact]
    public async Task AddCaseToDecisionRegisterAsync_FailureWithIncorrectOutcomeDecision()
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

        var response = await sut.AddCaseToDecisionRegisterAsync(3, "case", "badOutcome", DateTime.Now, CancellationToken.None);

        Assert.False(response.IsSuccess);
    }
}
