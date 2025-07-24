using System.Net;
using System.Net.Http.Headers;
using Castle.Components.DictionaryAdapter;
using Forestry.Flo.Services.Gis.Services;
using Forestry.Flo.Services.Gis.Models.Esri.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Tests.Services.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForesterServicesTests
{
    private readonly Mock<ILogger<ForesterServices>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly EsriConfig _config;
    private readonly HttpResponseMessage _successTokenRMessage;
    private readonly Point _nullIsland = new(0.0f, 0.0f);
    private List<Polygon> _polygons;
    private Geometry<Polygon> _geometryPolygon;

    public ForesterServicesTests()
    {
        _mockLogger = new Mock<ILogger<ForesterServices>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _config = new EsriConfig() {
            Forester = new ForesterConfig {
                BaseUrl = "https://www.AGOL.com/",
                CountryCode = "E92000001",
                GenerateTokenService = new OAuthServiceSettingsUser {
                    Password = "password",
                    Username = "username",
                    Path = "GetToken",
                },
                NeedsToken = true,
                UtilitiesService = new UtilitiesServiceSettings {
                    NeedsToken = true,
                    ExportService = new ExportServiceSettings {
                        BaseMap = "base",
                        BaseMapID = "baseID",
                        DefaultFormat = "PNG8",
                        Path = "Export",
                        TextOverrides = new TextOverrideDetails {
                            RestockingTitle = "Restocking",
                            Copyright = "© 2021",
                            FellingTitle = "Felling",
                        }
                    },
                    Path = "Utils",
                    IsPublic = false,

                    JobStatusService = new JobStatusServiceSettings {
                        Path = "jobs/{0}",
                        Status = new StatusSettings {
                            FailedStates = ["Failed"],
                            SuccessStates = ["Succeeded"],
                            PendingStates = ["Pending"]
                        }
                    }
                },
                LayerServices =
                [
                    new FeatureLayerConfig()
                        {
                            Name = "Country_Boundaries_Generalised", ServiceURI = "https://www.AGOL.com/Boundary",
                            Fields = [], NeedsToken = true
                        },

                        new FeatureLayerConfig()
                        {
                            Name = "Woodland_Officers", ServiceURI = "https://www.AGOL.com/WoodlandOfficer",
                            Fields = [], NeedsToken = true
                        },

                        new FeatureLayerConfig()
                        {
                            Name = "LocalAuthority_Areas", ServiceURI = "https://www.AGOL.com/LA",
                            Fields = [], NeedsToken = true
                        },

                        new FeatureLayerConfig()
                        {
                            Name = "SiteVisitCompartments", ServiceURI = "https://www.AGOL.com/SiteVisitCompartments",
                            Fields = [], NeedsToken = true
                        },
                        new FeatureLayerConfig()
                        {
                            Name = "Phytophthora_Ramorum_Risk_Zones", ServiceURI = "https://www.AGOL.com/Risk",
                            Fields = [], NeedsToken = true
                        }

                ]
            },
            SpatialReference = 22770
        };


        _successTokenRMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"token\":\"T0\",\"expires\":1666259794347, \"expires_in\": 1}")
        };
        _successTokenRMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var localPolygon = new Polygon {
            Rings = new List<List<List<float>>>
            {
                    new EditableList<List<float>>
                    {
                        new EditableList<float> { 4.2f, 4.1f }
                    }
                }
        };

        _polygons = [localPolygon];

        _geometryPolygon = new Geometry<Polygon>(localPolygon, "esriGeometryPolygon");
    }

    [Fact]
    public void Constructor_ThrowsAGOLNotSet()
    {

        var caughtException = Assert.Throws<ArgumentNullException>(() => new IForesterAccessTestPipe(new EsriConfig(), _mockHttpClientFactory.Object, _mockLogger.Object));
        Assert.Equal("Value cannot be null. (Parameter 'Forester Settings not configured')", caughtException.Message);
    }

    public IForesterAccessTestPipe CreateSUT(EsriConfig? config = null)
    {
        config ??= _config;

        return new IForesterAccessTestPipe(config, _mockHttpClientFactory.Object, _mockLogger.Object);
    }
}
