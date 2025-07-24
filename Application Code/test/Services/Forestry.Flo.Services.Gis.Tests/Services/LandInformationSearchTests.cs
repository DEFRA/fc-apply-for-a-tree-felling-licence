using System.Net;
using System.Net.Http.Headers;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class LandInformationSearchTests
{
    private readonly Mock<ILogger<LandInformationSearch>> _mockLogger;
    private Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

    private readonly LandInformationSearchOptions _landInformationSearchOptions;
    private readonly HttpResponseMessage _htmlMessage;
    private readonly HttpResponseMessage _successTokenRMessage;

    private readonly Polygon _polygon;

    private List<InternalCompartmentDetails<Polygon>> _compartments;

    public LandInformationSearchTests()
    {
        _mockLogger = new Mock<ILogger<LandInformationSearch>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        _landInformationSearchOptions = new LandInformationSearchOptions
        {
            BaseUrl = "https://example.com",
            FeaturePath = "/FeatureServer/0",
            TokenUrl = "https://example.com",
            TokenPath = "/token",
            ClientId = "clientId",
            ClientSecret = "clientSecret"
        };

        _htmlMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("<html><title>queryPathIsWrong</title><body></body><html>")

        };
        _htmlMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

        _successTokenRMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                "{\"token\":\"R0dXWiRiZmvxESEeAVTDqDfxxbaMEFRkc0pzj4_iTdmSgmEz1IVWQ8xV0UK3K3dY\",\"expires\":1666259794347}")
        };
        _successTokenRMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _polygon = JsonConvert.DeserializeObject<Polygon>(
                "{\"spatialReference\":{\"wkid\":27700},\"rings\":[[[490070.0654219554,138363.69600009505],[586170.0654202382,111359.38428214227],[482533.8850376785,103378.99647435723],[486609.63967905333,134042.3124613746],[490070.0654219554,138363.69600009505]]]}")
            !;

        _compartments =
        [
            new InternalCompartmentDetails<Polygon>
            {
                ShapeGeometry = _polygon,
                CompartmentLabel = "The Big Trees",
                CompartmentNumber = "435A",
                SubCompartmentNo = "85"
            },

            new InternalCompartmentDetails<Polygon>
            {
                ShapeGeometry = _polygon,
                CompartmentLabel = "The small Trees",
                CompartmentNumber = "435T",
                SubCompartmentNo = "80"
            }
        ];
    }
    public LandInformationSearch CreateSUT()
    {
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        return new LandInformationSearch(
            new OptionsWrapper<LandInformationSearchOptions>(_landInformationSearchOptions),
            _mockHttpClientFactory.Object,
            _mockLogger.Object);
    }
}
