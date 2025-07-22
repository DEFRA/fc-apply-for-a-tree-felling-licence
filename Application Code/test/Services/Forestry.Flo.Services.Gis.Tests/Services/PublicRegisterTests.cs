using Forestry.Flo.Services.Gis.Services;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http.Headers;
using Castle.Components.DictionaryAdapter;
using Forestry.Flo.Services.Gis.Models.MapObjects;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class PublicRegisterTests
{
    private Mock<ILogger<PublicRegister>> _mockLogger;
    private Mock<HttpMessageHandler> _mockHttpHandler;
    private Mock<IHttpClientFactory> _mockHttpClientFactory;


    private readonly EsriConfig _config;
    private readonly Polygon _polygon;
    private readonly HttpResponseMessage _htmlMessage;
    private readonly HttpResponseMessage _successTokenRMessage;
    private readonly HttpResponseMessage _successUpdate;
    private readonly HttpResponseMessage _successAddBoundary;
    private readonly HttpResponseMessage _successDeleteCompartment;
    private readonly HttpResponseMessage _successQuery;
    private readonly HttpResponseMessage _emptyMessage;

    private List<InternalCompartmentDetails<Polygon>> _compartments;

    public PublicRegisterTests()
    {
        _mockLogger = new Mock<ILogger<PublicRegister>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        _config = new EsriConfig {
            PublicRegister = new PublicRegistryConfig {
                BaseUrl = "https://www.forester_gis.com/geostore/",
                NeedsToken = true,
                GenerateTokenService = new OAuthServiceSettingsUser {
                    Username = "User",
                    Password = "Password",
                    Path = "tokens/"
                },
                Boundaries = new BaseEsriServiceConfig {
                    Path = "Boundaries"
                },
                Compartments = new BaseEsriServiceConfig {
                    Path = "Compartments"
                },
                Comments = new BaseEsriServiceConfig {
                    Path = "comments"
                },
                LookUps = new ESRILookUp {
                    Status = new Statuses {
                        Approved = "0",
                        Consultation = "1",
                        FinalProposal = "2",
                        InitialProposal = "3",
                        UploadedByGMS = "4"
                    }
                }
            }
        };

        _htmlMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("<html><title>queryPathIsWrong</title><body></body><html>")

        };
        _htmlMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

        _successTokenRMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                "{\"token\":\"R0dXWiRiZmvxESEeAVTDqDfxxbaMEFRkc0pzj4_iTdmSgmEz1IVWQ8xV0UK3K3dY\",\"expires\":1666259794347}")
        };
        _successTokenRMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _successAddBoundary = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(new CreateUpdateDeleteResponse<int> {
                AddResults = [new BaseCreateDeleteResult<int> { ObjectId = 1, WasSuccessful = true }]
            }))
        };

        _successAddBoundary.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _successUpdate = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                "{\"updateResults\": [{\"objectId\": 1,\"globalId\": \"{CBFA6974-BAA8-4533-B6D0-7B9729E1DF5D}\", \"success\": true }]}")
        };

        _successUpdate.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _emptyMessage = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{}")
        };
        _emptyMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _successQuery = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                "{\"objectIdFieldName\": \"objectid\", \"globalIdFieldName\": \"globalid_1\", \"features\": []}")
        };

        _successQuery.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");




        _successDeleteCompartment = new HttpResponseMessage {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(new CreateUpdateDeleteResponse<int> {
                DeleteResults = [new BaseCreateDeleteResult<int> { ObjectId = 1, WasSuccessful = true }]
            }))
        };

        _successDeleteCompartment.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

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

    public PublicRegister CreateSUT()
    {
        return new PublicRegister(_config, _mockHttpClientFactory.Object, _mockLogger.Object);
    }
}

