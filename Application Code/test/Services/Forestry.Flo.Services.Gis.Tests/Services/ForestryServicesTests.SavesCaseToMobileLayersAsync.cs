using Castle.Components.DictionaryAdapter;
using FluentAssertions;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Internal;
using Moq.Protected;
using Moq;
using System.Net;

namespace Forestry.Flo.Services.Gis.Tests.Access;

public partial class ForestryAccessTests
{
    //FLOV2-1317 Site visit mobile apps disabled for now
    //[Fact]
    //public async Task SavesCaseToMobileLayersAsync_ReturnsFailureWhenLayerNotFound()
    //{
    //    var sut = CreateSUT(new EsriConfig() { AGOL = new AGOLConfig { GenerateTokenService = new OAuthServiceSettingsClient { ClientID = "", ClientSecret = "", Path = "" }, LayerServices = new()} });


    //    var result = await sut.SavesCaseToMobileLayersAsync("Test", new(){ new InternalFullCompartmentDetails()} , CancellationToken.None);

    //    result.IsFailure.Should().BeTrue();
    //    result.Error.Should().Be("Unable to find layer details");
    //}

    //[Fact]
    //public async Task SavesCaseToMobileLayersAsync_NoCompartmentsGiven()
    //{
    //    var SUT = CreateSUT();

    //    var result = await SUT.SavesCaseToMobileLayersAsync("Test", new(), CancellationToken.None);

    //    result.IsSuccess.Should().BeTrue();
    //}

    //[Fact]
    //public async Task SavesCaseToMobileLayersAsync_UpdateServer_Failure_check()
    //{
    //    _mockHttpHandler.Reset();
    //    _mockHttpHandler.Protected()
    //        .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
    //            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

    //    _mockHttpHandler.Protected()
    //        .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/SiteVisitCompartments/addFeatures")),
    //            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway }).Verifiable();


    //    _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

    //    var sut = CreateSUT();


    //    var response = await sut.SavesCaseToMobileLayersAsync("Test", new() { new (){ CompartmentLabel = "Test"}}, CancellationToken.None);

    //    _mockHttpHandler.VerifyAll();

    //    response.IsFailure.Should().BeTrue();
    //    response.Error.Should().Be("Unable to connect to the esri service");
    //}
}