using Castle.Components.DictionaryAdapter;
using Forestry.Flo.Services.Gis.Models.Esri.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using Forestry.Flo.Services.Gis.Tests.Services.Infrastructure;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForestryServicesTests
{
    private Mock<ILogger<ForestryServices>> _mockLogger;
    private Mock<HttpMessageHandler> _mockHttpHandler;
    private Mock<IHttpClientFactory> _mockHttpClientFactory;
    private EsriConfig _config;
    private readonly HttpResponseMessage _successTokenRMessage;
    private Point _nullIsland = new(0.0f, 0.0f);
    private readonly HttpResponseMessage _htmlMessage;
    private List<Polygon> _polygons;
    private Geometry<Polygon> _geometryPolygon;

    public ForestryServicesTests()
    {
        _mockLogger = new Mock<ILogger<ForestryServices>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _config = new EsriConfig() {
            Forestry = new ForestryConfig {
                ApiKey = "Key",

                FeaturesService = new FeaturesServiceSettings {
                    Path = "feat",
                    NeedsToken = true,
                    IsPublic = false,
                    GenerateService = new GenerateServiceSettings {
                        Path = "gen"
                    }
                },

                BaseUrl = "https://www.AGOL.com/",
                GenerateTokenService = new OAuthServiceSettingsClient {
                    ClientID = "test",
                    ClientSecret = "secrets",
                    Path = "GetToken"
                },


                GeometryService = new GeometryServiceSettings {
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
                },
                LayerServices =
                [
                    new FeatureLayerConfig()
                    {
                        Name = "SiteVisitCompartments", ServiceURI = "https://www.AGOL.com/SiteVisitCompartments",
                        Fields = new List<string> { }, NeedsToken = true
                    }

                ]
            },
            SpatialReference = 2270
        };

        _successTokenRMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
            "{\"token\":\"R0dXWiRiZmvxESEeAVTDqDfxxbaMEFRkc0pzj4_iTdmSgmEz1IVWQ8xV0UK3K3dY\",\"expires\":1666259794347}")
        };

        _successTokenRMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var localPolygon = new Polygon {
            Rings =
            [
                new EditableList<List<float>>
                {
                    new EditableList<float> { 4.2f, 4.1f }
                }
            ]
        };

        _polygons = [localPolygon];

        _geometryPolygon = new Geometry<Polygon>(localPolygon, "esriGeometryPolygon");
    }

    [Fact]
    public void Constructor_ThrowsForestryNotSet()
    {

        var caughtException = Assert.Throws<ArgumentNullException>(() => new ForestryServicesTestPipe(new EsriConfig(), _mockHttpClientFactory.Object, _mockLogger.Object));
        Assert.Equal("Value cannot be null. (Parameter 'Forestry not configured')", caughtException.Message);
    }

    private ForestryServicesTestPipe CreateSut(EsriConfig? config = null)
    {
        config ??= _config;

        return new ForestryServicesTestPipe(config, _mockHttpClientFactory.Object, _mockLogger.Object);
    }
}
