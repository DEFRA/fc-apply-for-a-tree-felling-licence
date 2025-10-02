using System.Net;
using System.Net.Http.Headers;
using Castle.Components.DictionaryAdapter;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Common;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public partial class BaseServicesTests
{
    private readonly Mock<ILogger<Infrastructure.BaseServicesTests>> _mockLogger = new();
    private readonly Mock<HttpMessageHandler> _mockHttpHandler = new();
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();

    private readonly GeometryServiceSettings _geometryServiceSettings = new()
    {
        Path = "https://www.AGOL.com/geom/",
        IsPublic = true,
        NeedsToken = false,
        IntersectService = new BaseEsriServiceConfig {
            Path = "intersect"
        },
        UnionService = new BaseEsriServiceConfig {
            Path = "union"
        },
        ProjectService = new ProjectServiceSettings {
            Path = "project",
            OutSR = 1
        },
    };


    private readonly List<Polygon> _polygons =
    [
        new()
        {
                Rings =
                [
                    new EditableList<List<float>>
                    {
                        new EditableList<float> { 4.2f, 4.1f }
                    }
                ]
        }
    ];

    public Infrastructure.BaseServicesTests CreateSut(bool setToken = false, int addDays = 1, GetTokenParameters? tokenIn = null, List<FeatureLayerConfig>? layerSettings = null, GeometryServiceSettings? geometryService = null, int? spatialReference = null)
    {

        var tokens = tokenIn ?? new GetTokenParameters("user", "admin", false);

        var sut = new Infrastructure.BaseServicesTests(_mockHttpClientFactory.Object, "client", _mockLogger.Object, tokens, "http://test.org/path/", layerSettings, geometryService, spatialReference);

        if (setToken) {
            sut.Token = new EsriTokenResponse { Expiry = DateTime.Now.AddDays(addDays), TokenString = "Token" };
        }
        return sut;


    }

    [Fact]
    public async Task GetToken_ReturnsFailOnBadServer()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.BadGateway,
                });


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var response = await classUnderTest.GetTokenAsync(CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal("Unable to connect to the esri service", response.Error);
    }

    /// <summary>
    /// NOTE: Esri's api has a UI loaded over the top. Paths matter..... 
    /// For example */getoken returns the UI (aka HTML), */gettoken/ is the query
    /// </summary>
    [Theory]
    [InlineData("<html><title>queryPathIsWrong</title><body></body><html>")]
    public async Task GetToken_ReturnsHtml(string html)
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(html)
                });


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var response = await classUnderTest.GetTokenAsync(CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal("Unexpected character encountered while parsing value: <. Path '', line 0, position 0.", response.Error);
    }

    [Fact]
    public async Task GetToken_ReturnsBadJsonHtml()
    {
        var obj = JsonConvert.SerializeObject(new { message = "" });
        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(obj)

        };

        returnMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var response = await classUnderTest.GetTokenAsync(CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal($"Unable to deserialize content as ESRI token response: {obj}", response.Error);
    }


    [Theory]
    [InlineData("{\"error\":{\"code\":501,\"message\":\"No Simple Server Code\",\"details\":\"some details\"}}")]
    public async Task GetToken_ReturnsEsriError(string error)
    {
        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(error)

        };

        returnMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var response = await classUnderTest.GetTokenAsync(CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal("No Simple Server Code", response.Error);
    }

    [Fact]
    public async Task GetToken_HandlesError()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception("This is an error"));


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var response = await classUnderTest.GetTokenAsync(CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal("This is an error", response.Error);
    }

    [Fact]
    public async Task GetToken_TokenCodeNotSet()
    {
        var obj = JsonConvert.SerializeObject(new EsriTokenResponse {
            Expiry = DateTime.Now,
        });

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(obj)

        };

        returnMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var response = await classUnderTest.GetTokenAsync(CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal($"Unable to deserialize content as ESRI token response: {obj}", response.Error);
    }

    [Fact]
    public async Task GetToken_Success()
    {
        var obj = JsonConvert.SerializeObject(new EsriTokenResponse {
            Expiry = DateTime.Now,
            TokenString = "Token"
        });

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(obj)

        };

        returnMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var response = await classUnderTest.GetTokenAsync(CancellationToken.None);

        Assert.True(response.IsSuccess);
    }

    [Fact]
    public async Task GetToken_CalledIfExpired()
    {
        var obj = JsonConvert.SerializeObject(new EsriTokenResponse {
            Expiry = DateTime.Now.AddDays(1),
            TokenString = "Token"
        });

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(obj)

        };

        returnMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut(true, -1);

        var response = await classUnderTest.GetTokenAsync(CancellationToken.None);

        Assert.True(response.IsSuccess);
        _mockHttpHandler.VerifyAll();
    }

    [Fact]
    public async Task GetTokenString_ServiceThrows()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var result = await classUnderTest.GetTokenString();

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to log into server", result.Error);
        _mockHttpHandler.Verify();
    }

    [Fact]
    public async Task GetTokenString_ServiceNotCalledIfTokenSet()
    {
        var classUnderTest = CreateSut(true);

        await classUnderTest.GetTokenAsync(default);

        var result = await classUnderTest.GetTokenString();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);
        _mockHttpHandler.Protected().Verify("SendAsync", Times.AtMostOnce(), ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTokenString()
    {
        var obj = JsonConvert.SerializeObject(new EsriTokenResponse {
            Expiry = DateTime.Now,
            TokenString = "Token"
        });

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(obj)

        };

        returnMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var result = await classUnderTest.GetTokenString();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);
        _mockHttpHandler.Verify();
    }

    [Fact]
    public async Task GetTokenString_SetTokenFails()
    {
        var obj = JsonConvert.SerializeObject(new EsriTokenResponse {
            Expiry = DateTime.Now,
            TokenString = "Token"
        });

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(obj)

        };

        returnMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).Throws(new NotImplementedException());


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var classUnderTest = CreateSut();

        var result = await classUnderTest.GetTokenString();

        Assert.False(result.IsSuccess);
        Assert.Equal("Unable to log into server", result.Error);
        _mockHttpHandler.Verify();
    }

    [Fact]
    public async Task PostQueryWithConversionAsync_HandlesError()
    {
        var SUT = CreateSut();

        var result = await SUT.PostQueryWithConversionAsync<string>(new BaseParameter(), "path", false);

        Assert.True(result.IsFailure);
    }


    [Fact]
    public async Task PostQueryWithConversionAsync_TokenFails()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.BadRequest,
                }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var SUT = CreateSut(setToken: true, addDays: -1);

        var result = await SUT.PostQueryWithConversionAsync<string>(new BaseParameter(), "https://www.AGOL.com/", true);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to log into server", result.Error);
        _mockHttpHandler.VerifyAll();
    }

    [Fact]
    public async Task PostQueryWithConversionAsync_Check_Fail_handles()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.BadRequest,
                }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var SUT = CreateSut();

        var result = await SUT.PostQueryWithConversionAsync<string>(new BaseParameter(), "https://www.AGOL.com/", false);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to connect to the esri service", result.Error);
        _mockHttpHandler.VerifyAll();
    }


    [Fact]
    public async Task PostQueryAsync_HandlesError()
    {
        var SUT = CreateSut();

        var result = await SUT.PostQueryAsync(new BaseParameter(), "path", false, false);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task PostQueryAsync_HandlesErrorCode()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.BadRequest,
                }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var SUT = CreateSut();

        var result = await SUT.PostQueryAsync(new BaseParameter(), "https://www.AGOL.com/", false, false);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to connect to the esri service", result.Error);
        _mockHttpHandler.VerifyAll();
    }

    [Theory]
    [InlineData("<html><title>queryPathIsWrong</title><body></body><html>")]
    public async Task PostQueryAsync_ReturnsHtml(string html)
    {
        var message = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(html)
        };
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(message);


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var SUT = CreateSut();

        var result = await SUT.PostQueryAsync(new BaseParameter(), "https://www.AGOL.com/", true, false);

        Assert.True(result.IsSuccess);
        Assert.Equal(html, result.Value);
        _mockHttpHandler.VerifyAll();
    }

    [Theory]
    [InlineData("<html><title></title><body></body><html>")]
    public async Task PostQueryAsync_RejectsHtml(string html)
    {
        var message = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(html)
        };
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(message);


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var SUT = CreateSut();

        var result = await SUT.PostQueryAsync(new BaseParameter(), "https://www.AGOL.com/", false, false);

        Assert.True(result.IsSuccess);
        Assert.Equal(html, result.Value);
        _mockHttpHandler.VerifyAll();
    }

    [Fact]
    public async Task PostQueryAsync_ReturnsEsriError()
    {

        var returnMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"error\": {\"code\": 400,\"message\": \"Unable to complete operation.\",\"details\": [ \"Unable to perform query operation.\"]}}")
        };
        returnMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");


        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(returnMessage);

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));
        var SUT = CreateSut();

        var result = await SUT.PostQueryAsync(new BaseParameter(), "https://www.AGOL.com/", false, false);

        Assert.True(result.IsFailure);
        Assert.Equal("ESRI Error-> Unable to complete operation.: Unable to perform query operation.)", result.Error);
        _mockHttpHandler.VerifyAll();
    }

    [Fact]
    public async Task GPostQueryAsync_QueryServerThrows()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/")),
                ItExpr.IsAny<CancellationToken>()).Throws(new Exception("This is an error message"));

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));
        var SUT = CreateSut();

        var result = await SUT.PostQueryWithConversionAsync<string>(new BaseParameter(), "https://www.AGOL.com/", false);

        Assert.True(result.IsFailure);
        Assert.Equal("This is an error message", result.Error);
        _mockHttpHandler.VerifyAll();
    }


    [Fact]
    public async Task PostQueryWithConversionAsync_Success()
    {
        _mockHttpHandler.Reset();
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(new LabelPointResponse {
                        Points =
                        [new Point { X = 1.2f, Y = 1.2f }]
                    }))
                }).Verifiable();


        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));
        var SUT = CreateSut();

        var result = await SUT.PostQueryWithConversionAsync<LabelPointResponse>(new BaseParameter(), "https://www.AGOL.com/", false);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        _mockHttpHandler.VerifyAll();
    }


    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("{\"error\":{\"code\":501,\"message\":\"No Simple Server Code\",\"details\":\"some details\"}}")]
    [InlineData(
        "{\"error\":{\"code\":520,\"message\":\"Message\",\"details\":[\"Details 1\",\"Details 2\"]}}")]
    [InlineData("{\"error\":{\"code\":400,\"extendedCode\":-2147467261,\"message\":\"Unable to complete operation.\",\"details\":[\"Error executing tool. Export Web Map Task : ERROR 001305: Missing or invalid 'exportOptions' property in WebMap; cannot derive a size for the output image.\\nFailed to execute (Export Web Map).\\nFailed to execute (Export Web Map Task).\"]}}")]
    public void CheckForErrorsReturnsErrors(string message)
    {
        var SUT = CreateSut();

        var result = SUT.CheckForEsriErrors(message);
        Assert.True(result.HasValue);
    }

    [Fact]
    public async Task GetEsriGeneratedImageAsync_EnsureThreeHits()
    {

        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://paul.com/image.png")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));


        var classUnderTest = CreateSut();

        var result = await classUnderTest.GetEsriGeneratedImageAsync("https://paul.com/image.png", 1, CancellationToken.None);

        Assert.True(result.IsFailure);
        _mockHttpHandler.Verify();
        _mockHttpHandler.Protected().Verify("SendAsync", Times.Exactly(3), ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://paul.com/image.png")),
            ItExpr.IsAny<CancellationToken>());

    }




    [Theory]
    [InlineData("Stuff")]
    [InlineData("<html><html>")]
    public void CheckForErrorsReturnsNoErrors(string message)
    {
        var SUT = CreateSut();

        var result = SUT.CheckForEsriErrors(message);
        Assert.False(result.HasValue);
    }
}
