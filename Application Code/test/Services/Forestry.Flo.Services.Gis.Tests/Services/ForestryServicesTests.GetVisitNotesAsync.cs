using FluentAssertions;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Moq.Protected;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Mobile;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Tests.Access
{
    public partial class ForestryAccessTests
    {
        //FLOV2-1317 Site visit mobile apps disabled for now
        //[Fact]
        //public async Task GetVisitNotesAsync_ReturnsFailureWhenLayerNotFound()
        //{
        //    var sut = CreateSUT(new EsriConfig()
        //    {
        //        AGOL = new AGOLConfig
        //        {
        //            GenerateTokenService = new OAuthServiceSettingsClient
        //                { ClientID = "", ClientSecret = "", Path = "" },
        //            LayerServices = new()
        //        }
        //    });


        //    var result = await sut.GetVisitNotesAsync("Test", CancellationToken.None);

        //    result.IsFailure.Should().BeTrue();
        //    result.Error.Should().Be("Unable to find layer details");
        //}

        //[Fact]
        //public async Task GetVisitNotesAsync_PostFailure_check()
        //{
        //    _mockHttpHandler.Reset();
        //    _mockHttpHandler.Protected()
        //        .Setup<Task<HttpResponseMessage>>("SendAsync",
        //            ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
        //            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        //    _mockHttpHandler.Protected()
        //        .Setup<Task<HttpResponseMessage>>("SendAsync",
        //            ItExpr.Is<HttpRequestMessage>(t =>
        //                t!.RequestUri!.Equals("https://www.AGOL.com/SiteVisitCompartments/query")),
        //            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
        //            { StatusCode = HttpStatusCode.BadGateway }).Verifiable();


        //    _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
        //        .Returns(new HttpClient(_mockHttpHandler.Object));

        //    var sut = CreateSUT();


        //    var result = await sut.GetVisitNotesAsync("Test", CancellationToken.None);

        //    _mockHttpHandler.VerifyAll();

        //    result.IsFailure.Should().BeTrue();
        //    result.Error.Should().Be("Unable to connect to the esri service");
        //}


        //[Fact]
        //public async Task GetVisitNotesAsync_NoResultsFound_check()
        //{
        //    _mockHttpHandler.Reset();
        //    _mockHttpHandler.Protected()
        //        .Setup<Task<HttpResponseMessage>>("SendAsync",
        //            ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
        //            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        //    _mockHttpHandler.Protected()
        //        .Setup<Task<HttpResponseMessage>>("SendAsync",
        //            ItExpr.Is<HttpRequestMessage>(t =>
        //                t!.RequestUri!.Equals("https://www.AGOL.com/SiteVisitCompartments/query")),
        //            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
        //            { StatusCode = HttpStatusCode.BadGateway }).Verifiable();


        //    _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
        //        .Returns(new HttpClient(_mockHttpHandler.Object));

        //    var sut = CreateSUT();


        //    var result = await sut.GetVisitNotesAsync("Test", CancellationToken.None);

        //    _mockHttpHandler.VerifyAll();

        //    result.IsFailure.Should().BeTrue();
        //    result.Error.Should().Be("Unable to connect to the esri service");
        //}
    }
}
