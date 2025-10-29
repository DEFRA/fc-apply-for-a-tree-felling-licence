using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Moq;
using Moq.Protected;
using System.Net;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForesterServicesTests
{
    [Fact]
    public async Task Publish_FLAToInternalAsync_ReturnsFailure_WhenNoCompartments()
    {
        _mockHttpHandler.Reset();

        var sut = CreateSUT();

        var result = await sut.Publish_FLAToInternalAsync(
            propertyName: "Test Property",
            applicationRef: "APP-REF",
            gridReference: "GR123",
            nearestTown: "TestTown",
            adminHubAreaName: "Hub",
            consultationPublicRegisterStartDate: DateTime.UtcNow,
            consultationPublicRegisterPeriod: 30,
            consultationPublicRegisterEndDate: DateTime.UtcNow.AddDays(30),
            decisionPublicRegisterStartDate: DateTime.UtcNow,
            decisionPublicRegisterPeriod: 10,
            decisionPublicRegisterEndDateTime: DateTime.UtcNow.AddDays(10),
            compartmentLabel: "A",
            confirmedTotalAreaHa: 1.23f,
            fsAreaName: "FS Area",
            currentApplicationStatus: "Approved",
            applicant: "Applicant",
            hasConditions: false,
            submittedDate: DateTime.UtcNow,
            decisionDate: DateTime.UtcNow,
            withdrawalDate: null,
            adminOfficer: "Admin",
            woodLandOfficer: "Woodland",
            approvingOfficer: "Approver",
            expiredDate: DateTime.UtcNow.AddYears(1),
            expiryCategory: "Standard",
            compartments: new List<InternalCompartmentDetails<Polygon>>(),
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("No compartments Set", result.Error);
    }

    [Fact]
    public async Task Publish_FLAToInternalAsync_ReturnsFailure_WhenLayerNotFound()
    {
        _mockHttpHandler.Reset();

        var sut = CreateSUT(new EsriConfig() { Forester = new ForesterConfig { GenerateTokenService = new OAuthServiceSettingsUser() { Password = "", Username = "", Path = "" } } });

        var compartments = new List<InternalCompartmentDetails<Polygon>>
        {
            new InternalCompartmentDetails<Polygon> { ShapeGeometry = new Polygon() }
        };

        var result = await sut.Publish_FLAToInternalAsync(
            propertyName: "Test Property",
            applicationRef: "APP-REF",
            gridReference: "GR123",
            nearestTown: "TestTown",
            adminHubAreaName: "Hub",
            consultationPublicRegisterStartDate: DateTime.UtcNow,
            consultationPublicRegisterPeriod: 30,
            consultationPublicRegisterEndDate: DateTime.UtcNow.AddDays(30),
            decisionPublicRegisterStartDate: DateTime.UtcNow,
            decisionPublicRegisterPeriod: 10,
            decisionPublicRegisterEndDateTime: DateTime.UtcNow.AddDays(10),
            compartmentLabel: "A",
            confirmedTotalAreaHa: 1.23f,
            fsAreaName: "FS Area",
            currentApplicationStatus: "Approved",
            applicant: "Applicant",
            hasConditions: false,
            submittedDate: DateTime.UtcNow,
            decisionDate: DateTime.UtcNow,
            withdrawalDate: null,
            adminOfficer: "Admin",
            woodLandOfficer: "Woodland",
            approvingOfficer: "Approver",
            expiredDate: DateTime.UtcNow.AddYears(1),
            expiryCategory: "Standard",
            compartments: compartments,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to find layer details", result.Error);
    }

    [Fact]
    public async Task Publish_FLAToInternalAsync_ReturnsFailure_WhenGeometryFails()
    {
        _mockHttpHandler.Reset();

        var sut = CreateSUT();

        var compartments = new List<InternalCompartmentDetails<Polygon>>
        {
            new InternalCompartmentDetails<Polygon> { ShapeGeometry = null! }
        };

        var result = await sut.Publish_FLAToInternalAsync(
            propertyName: "Test Property",
            applicationRef: "APP-REF",
            gridReference: "GR123",
            nearestTown: "TestTown",
            adminHubAreaName: "Hub",
            consultationPublicRegisterStartDate: DateTime.UtcNow,
            consultationPublicRegisterPeriod: 30,
            consultationPublicRegisterEndDate: DateTime.UtcNow.AddDays(30),
            decisionPublicRegisterStartDate: DateTime.UtcNow,
            decisionPublicRegisterPeriod: 10,
            decisionPublicRegisterEndDateTime: DateTime.UtcNow.AddDays(10),
            compartmentLabel: "A",
            confirmedTotalAreaHa: 1.23f,
            fsAreaName: "FS Area",
            currentApplicationStatus: "Approved",
            applicant: "Applicant",
            hasConditions: false,
            submittedDate: DateTime.UtcNow,
            decisionDate: DateTime.UtcNow,
            withdrawalDate: null,
            adminOfficer: "Admin",
            woodLandOfficer: "Woodland",
            approvingOfficer: "Approver",
            expiredDate: DateTime.UtcNow.AddYears(1),
            expiryCategory: "Standard",
            compartments: compartments,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("No valid rings found in input polygons.", result.Error);
    }

    [Fact]
    public async Task Publish_FLAToInternalAsync_ReturnsError_WhenUpdateServer_Failure()
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/ExternalFLA/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway }).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var compartments = new List<InternalCompartmentDetails<Polygon>>
        {
            new InternalCompartmentDetails<Polygon> { ShapeGeometry = new Polygon { Rings = [new()] } }
        };

        var result = await sut.Publish_FLAToInternalAsync(
            propertyName: "Test Property",
            applicationRef: "APP-REF",
            gridReference: "GR123",
            nearestTown: "TestTown",
            adminHubAreaName: "Hub",
            consultationPublicRegisterStartDate: DateTime.UtcNow,
            consultationPublicRegisterPeriod: 30,
            consultationPublicRegisterEndDate: DateTime.UtcNow.AddDays(30),
            decisionPublicRegisterStartDate: DateTime.UtcNow,
            decisionPublicRegisterPeriod: 10,
            decisionPublicRegisterEndDateTime: DateTime.UtcNow.AddDays(10),
            compartmentLabel: "A",
            confirmedTotalAreaHa: 1.23f,
            fsAreaName: "FS Area",
            currentApplicationStatus: "Approved",
            applicant: "Applicant",
            hasConditions: false,
            submittedDate: DateTime.UtcNow,
            decisionDate: DateTime.UtcNow,
            withdrawalDate: null,
            adminOfficer: "Admin",
            woodLandOfficer: "Woodland",
            approvingOfficer: "Approver",
            expiredDate: DateTime.UtcNow.AddYears(1),
            expiryCategory: "Standard",
            compartments: compartments,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockHttpHandler.VerifyAll();

        Assert.Equal("Unable to connect to the esri service", result.Error);
    }

    [Fact]
    public async Task Publish_FLAToInternalAsync_ReturnsSuccess_WhenAllValid()
    {
        _mockHttpHandler.Reset();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/GetToken")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successTokenRMessage).Verifiable();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(t => t!.RequestUri!.Equals("https://www.AGOL.com/ExternalFLA/addFeatures")),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(_successAddResponseMessage).Verifiable();

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpHandler.Object));

        var sut = CreateSUT();

        var compartments = new List<InternalCompartmentDetails<Polygon>>
        {
            new InternalCompartmentDetails<Polygon> { ShapeGeometry = new Polygon { Rings = [new()] } }
        };

        var result = await sut.Publish_FLAToInternalAsync(
            propertyName: "Test Property",
            applicationRef: "APP-REF",
            gridReference: "GR123",
            nearestTown: "TestTown",
            adminHubAreaName: "Hub",
            consultationPublicRegisterStartDate: DateTime.UtcNow,
            consultationPublicRegisterPeriod: 30,
            consultationPublicRegisterEndDate: DateTime.UtcNow.AddDays(30),
            decisionPublicRegisterStartDate: DateTime.UtcNow,
            decisionPublicRegisterPeriod: 10,
            decisionPublicRegisterEndDateTime: DateTime.UtcNow.AddDays(10),
            compartmentLabel: "A",
            confirmedTotalAreaHa: 1.23f,
            fsAreaName: "FS Area",
            currentApplicationStatus: "Approved",
            applicant: "Applicant",
            hasConditions: false,
            submittedDate: DateTime.UtcNow,
            decisionDate: DateTime.UtcNow,
            withdrawalDate: null,
            adminOfficer: "Admin",
            woodLandOfficer: "Woodland",
            approvingOfficer: "Approver",
            expiredDate: DateTime.UtcNow.AddYears(1),
            expiryCategory: "Standard",
            compartments: compartments,
            cancellationToken: CancellationToken.None);

        _mockHttpHandler.VerifyAll();

        Assert.True(result.IsSuccess);
    }
}