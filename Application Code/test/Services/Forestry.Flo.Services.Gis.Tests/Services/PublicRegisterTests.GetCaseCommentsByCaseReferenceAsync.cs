using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public partial class PublicRegisterTests
{
    [Fact]
    public async Task GetCaseCommentsByCaseReferenceAsync_Fails_CheckResponse()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);
        _mockHttpHandler.Protected()

            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/comments/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway });


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.GetCaseCommentsByCaseReferenceAsync("caseNo", CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be("Unable to connect to the esri service");
    }

    [Fact]
    public async Task GetCaseCommentsByCaseReferenceAsync_QueryServerSuccess()
    {

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/tokens/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage);
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.forester_gis.com/geostore/comments/query")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successQuery);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var response = await sut.GetCaseCommentsByCaseReferenceAsync("caseNo", CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
    }
}
