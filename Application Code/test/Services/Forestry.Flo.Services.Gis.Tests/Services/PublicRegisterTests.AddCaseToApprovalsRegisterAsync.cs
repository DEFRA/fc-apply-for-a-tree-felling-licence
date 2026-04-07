using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
using Moq;
using Moq.Protected;
using System.Net;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public partial class PublicRegisterTests
{
    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_EmptyCompartments_Throws(string fellingLicenceOutcome)
    {
        var model = Fixture.Build<AddToDecisionPublicRegisterModel>()
            .With(x => x.FellingLicenceOutcome, fellingLicenceOutcome)
            .With(x => x.Compartments, [])
            .Create();

        _mockHttpHandler.Reset();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var result = await sut.AddCaseToDecisionRegisterAsync(model, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("No compartments Set", result.Error);
    }

    [Theory, AutoData]
    public async Task AddCaseToDecisionRegisterAsync_UnknownOutcomeValue_ReturnsFailure(AddToDecisionPublicRegisterModel model)
    {
        _mockHttpHandler.Reset();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var result = await sut.AddCaseToDecisionRegisterAsync(model, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal($"Felling application outcome of {model.FellingLicenceOutcome} on application having reference {model.CaseReference} is invalid for sending to the Decision Public Register", result.Error);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_FailsToAddCompartments_FailsToRollbackCase(string fellingLicenceOutcome)
    {
        var model = Fixture.Build<AddToDecisionPublicRegisterModel>()
            .Without(x => x.ExistingEsriId)
            .With(x => x.FellingLicenceOutcome, fellingLicenceOutcome)
            .Create();

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

        var response = await sut.AddCaseToDecisionRegisterAsync(model, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Added Case Boundary, but failed to add compartments. Unable to rollback Boundary", response.Error);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_FailsToAddCompartments_RollsBackCase(string fellingLicenceOutcome)
    {
        var model = Fixture.Build<AddToDecisionPublicRegisterModel>()
            .Without(x => x.ExistingEsriId)
            .With(x => x.FellingLicenceOutcome, fellingLicenceOutcome)
            .Create();

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

        var response = await sut.AddCaseToDecisionRegisterAsync(model, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsFailure);
        Assert.Equal("Added Case Boundary, but failed to add compartments. Boundary has been rolled back", response.Error);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_Success(string fellingLicenceOutcome)
    {
        var model = Fixture.Build<AddToDecisionPublicRegisterModel>()
            .Without(x => x.ExistingEsriId)
            .With(x => x.FellingLicenceOutcome, fellingLicenceOutcome)
            .With(x => x.Compartments, _compartments)
            .Create();

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

        var response = await sut.AddCaseToDecisionRegisterAsync(model, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsSuccess);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_UpdatingExistingCase_Success(string fellingLicenceOutcome)
    {
        var model = Fixture.Build<AddToDecisionPublicRegisterModel>()
            .With(x => x.ExistingEsriId, 1)
            .With(x => x.FellingLicenceOutcome, fellingLicenceOutcome)
            .With(x => x.Compartments, _compartments)
            .Create();

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

        var response = await sut.AddCaseToDecisionRegisterAsync(model, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsSuccess);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Refused")]
    [InlineData("ReferredToLocalAuthority")]
    public async Task AddCaseToDecisionRegisterAsync_UpdatingExistingCaseButIsNotFoundOnLayerAnyMore_Success(string fellingLicenceOutcome)
    {
        var model = Fixture.Build<AddToDecisionPublicRegisterModel>()
            .With(x => x.ExistingEsriId, 2)
            .With(x => x.FellingLicenceOutcome, fellingLicenceOutcome)
            .With(x => x.Compartments, _compartments)
            .Create();

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

        var response = await sut.AddCaseToDecisionRegisterAsync(model, CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(response.IsSuccess);
    }
}
