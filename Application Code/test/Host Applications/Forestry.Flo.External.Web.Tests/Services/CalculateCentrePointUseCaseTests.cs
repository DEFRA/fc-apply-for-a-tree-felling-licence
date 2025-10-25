using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.MassTransit.Messages;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.External.Web.Tests.Services;

public class CalculateCentrePointUseCaseTests
{
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceService;
    private readonly Mock<IAuditService<CalculateCentrePointUseCase>> _auditMock;
    private readonly Mock<ISubmitFellingLicenceService> _submitFellingLicenceService;
    private readonly Mock<IUpdateCentrePoint> _updateCentrePointMock;
    private readonly Mock<IOptions<InternalUserSiteOptions>> _optionsMock;
    private readonly Mock<IPropertyProfileRepository> _propertyProfileRepositoryMock;

    private readonly Fixture _fixtureInstance;

    public CalculateCentrePointUseCaseTests()
    {
        _auditMock = new Mock<IAuditService<CalculateCentrePointUseCase>>();
        _getFellingLicenceService = new Mock<IGetFellingLicenceApplicationForExternalUsers>();
        _submitFellingLicenceService = new Mock<ISubmitFellingLicenceService>();
        _updateCentrePointMock = new Mock<IUpdateCentrePoint>();
        _optionsMock = new Mock<IOptions<InternalUserSiteOptions>>();
        _fixtureInstance = new Fixture();
        _propertyProfileRepositoryMock = new Mock<IPropertyProfileRepository>();
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string BaseUrl = "testUrl";
    private const string LinkToApp = "linkToApp";
    private const string CentrePoint = "centrePoint";
    private const string OsGrid = "OsGridReference";
    private const string AreaCode = "017";
    private const string AdminRegion = "Test";

    private readonly ConfiguredFcArea _configuredFcArea = new ConfiguredFcArea(new AreaModel()
        { Code = "1", Name = "AreaName", Id = new Guid() }, AreaCode, AdminRegion);

[Theory, AutoMoqData]
    public async Task ReturnsSuccess_WhenCentrePointCorrectlySet(
        FellingLicenceApplication application, 
        AutoAssignWoRecord autoAssignWoRecord,
        PropertyProfile property)
    {
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = application.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var message = new CentrePointCalculationMessage(
            application.WoodlandOwnerId,
            application.CreatedById,
            application.Id, 
            userAccessModel.IsFcUser,
            userAccessModel.AgencyId);

        var testUrl = $"{BaseUrl}FellingLicenceApplication/ApplicationSummary/{message.ApplicationId}";

        var relevantPropertyProfileCompartments = PrepareCompartments(application, property);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _propertyProfileRepositoryMock.Setup(s =>
            s.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateCentrePointForApplicationAsync(
                    It.IsAny<Guid>(), 
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(CentrePoint);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateOSGridAsync(
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(OsGrid);

        _submitFellingLicenceService.Setup(s =>
                s.GetConfiguredFcAreaAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configuredFcArea);

        _updateCentrePointMock.Setup(s => s.UpdateCentrePointAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _submitFellingLicenceService.Setup(s => s.AutoAssignWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(autoAssignWoRecord);

        var result = await sut.CalculateCentrePointAsync(message, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            CancellationToken.None));

        _submitFellingLicenceService.Verify(v => v.CalculateCentrePointForApplicationAsync(
                application.Id,
                It.Is<List<string>>(x => x.SequenceEqual(relevantPropertyProfileCompartments.Select(y => y.GISData).ToList())),
                CancellationToken.None),
            Times.Once);

        _submitFellingLicenceService.Verify(v => v.CalculateOSGridAsync(CentrePoint, CancellationToken.None), Times.Once);
        _updateCentrePointMock.Verify(
            v => v.UpdateCentrePointAsync(
                application.Id,
                It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                            x.AgencyId == message.AgencyId &&
                                            x.UserAccountId == message.UserId
                                            && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                            && x.WoodlandOwnerIds.Count == 1
                ),
                AreaCode, 
                AdminRegion,
                CentrePoint, 
                OsGrid, 
                CancellationToken.None)
            , Times.Once);
      
        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.CalculateCentrePointForApplication
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    CentrePoint = CentrePoint,
                    OsGridReference = OsGrid,
                    ConfiguredFcArea = _configuredFcArea
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenApplicationCannotBeRetrieved(
        FellingLicenceApplication application)
    {
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = application.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };
        
        var message = new CentrePointCalculationMessage(
            application.WoodlandOwnerId,
            application.CreatedById,
            application.Id,
            userAccessModel.IsFcUser,
            userAccessModel.AgencyId);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("test"));

        var result = await sut.CalculateCentrePointAsync(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            It.IsAny<CancellationToken>()));

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.CalculateCentrePointForApplicationFailure
                && x.UserId == message.UserId
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = $"Unable to retrieve application with identifier {message.ApplicationId}"
                }, _options)), 
            CancellationToken.None), Times.Once);
        _submitFellingLicenceService.VerifyNoOtherCalls();
        _updateCentrePointMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenAreaCodeCannotBeRetrieved(
        FellingLicenceApplication application,
        PropertyProfile property)
    {
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = application.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };


        var message = new CentrePointCalculationMessage(
            application.WoodlandOwnerId,
            application.CreatedById,
            application.Id,
            userAccessModel.IsFcUser,
            userAccessModel.AgencyId);

        var testUrl = $"{BaseUrl}FellingLicenceApplication/ApplicationSummary/{message.ApplicationId}";

        var relevantPropertyProfileCompartments = PrepareCompartments(application, property);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _propertyProfileRepositoryMock.Setup(s =>
            s.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateCentrePointForApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(CentrePoint);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateOSGridAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(OsGrid);

        _submitFellingLicenceService.Setup(s =>
                s.GetConfiguredFcAreaAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConfiguredFcArea>($"Unable to convert Point \"{CentrePoint}\""));

        var result = await sut.CalculateCentrePointAsync(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            It.IsAny<CancellationToken>()));

        _submitFellingLicenceService.Verify(v => v.CalculateCentrePointForApplicationAsync(
                application.Id,
                It.Is<List<string>>(x => x.SequenceEqual(relevantPropertyProfileCompartments.Select(y => y.GISData).ToList())),
                CancellationToken.None),
            Times.Once);
        _submitFellingLicenceService.Verify(v => v.CalculateOSGridAsync(CentrePoint, CancellationToken.None), Times.Once);
        _submitFellingLicenceService.Verify(v => v.GetConfiguredFcAreaAsync(CentrePoint, CancellationToken.None), Times.Once);
        
        _submitFellingLicenceService.Verify(v => v.AutoAssignWoodlandOfficerAsync(application.Id, application.CreatedById, testUrl, CancellationToken.None), Times.Never);
        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.CalculateCentrePointForApplicationFailure
                && x.UserId == message.UserId
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = $"Unable to retrieve the configured FC Area for application having id {message.ApplicationId}"
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenSelectedPropertyCompartmentsNull(
        FellingLicenceApplication application,
        PropertyProfile property)
    {
        application.SubmittedFlaPropertyDetail = null;

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = application.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };


        var message = new CentrePointCalculationMessage(
            application.WoodlandOwnerId,
            application.CreatedById,
            application.Id,
            userAccessModel.IsFcUser,
            userAccessModel.AgencyId);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _propertyProfileRepositoryMock.Setup(s =>
            s.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        var result = await sut.CalculateCentrePointAsync(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            CancellationToken.None));
        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.CalculateCentrePointForApplicationFailure
                && x.UserId == message.UserId
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = $"Unable to retrieve selected property compartments for application with identifier {message.ApplicationId} as some GISData is missing."
                }, _options)),
            CancellationToken.None), Times.Once);
        _submitFellingLicenceService.VerifyNoOtherCalls();
        _updateCentrePointMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenCentrePointCalculationFails(
        FellingLicenceApplication application,
        PropertyProfile property)
    {
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = application.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var message = new CentrePointCalculationMessage(
            application.WoodlandOwnerId,
            application.CreatedById,
            application.Id,
            userAccessModel.IsFcUser,
            userAccessModel.AgencyId);

        var relevantPropertyProfileCompartments = PrepareCompartments(application, property);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _propertyProfileRepositoryMock.Setup(s =>
            s.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateCentrePointForApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>("test"));

        var result = await sut.CalculateCentrePointAsync(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            It.IsAny<CancellationToken>()));

        _submitFellingLicenceService.Verify(v => v.CalculateCentrePointForApplicationAsync(
                application.Id,
                It.Is<List<string>>(x => x.SequenceEqual(relevantPropertyProfileCompartments.Select(y => y.GISData).ToList())),
                CancellationToken.None),
            Times.Once);
        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.CalculateCentrePointForApplicationFailure
                && x.UserId == message.UserId
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = $"Unable to calculate centre point for application having id {message.ApplicationId}"
                }, _options)),
            CancellationToken.None), Times.Once);
        _submitFellingLicenceService.VerifyNoOtherCalls();
        _updateCentrePointMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenOsGridRefCalculationFails(
        FellingLicenceApplication application,
        PropertyProfile property)
    {
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = application.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var message = new CentrePointCalculationMessage(
            application.WoodlandOwnerId,
            application.CreatedById,
            application.Id,
            userAccessModel.IsFcUser,
            userAccessModel.AgencyId);

        var relevantPropertyProfileCompartments = PrepareCompartments(application, property);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _propertyProfileRepositoryMock.Setup(s =>
            s.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateCentrePointForApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(CentrePoint);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateOSGridAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>("test"));

        var result = await sut.CalculateCentrePointAsync(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            CancellationToken.None));

        _submitFellingLicenceService.Verify(v => v.CalculateCentrePointForApplicationAsync(
                application.Id,
                It.Is<List<string>>(x => x.SequenceEqual(relevantPropertyProfileCompartments.Select(y => y.GISData).ToList())),
                CancellationToken.None),
            Times.Once);
        _submitFellingLicenceService.Verify(v => v.CalculateOSGridAsync(CentrePoint, CancellationToken.None), Times.Once);
        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.CalculateCentrePointForApplicationFailure
                && x.UserId == message.UserId
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = $"Unable to calculate OS Grid Ref for application having id {message.ApplicationId}"
                }, _options)),
            CancellationToken.None), Times.Once);
        _submitFellingLicenceService.VerifyNoOtherCalls();
        _updateCentrePointMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenFlaUpdateFails(
        FellingLicenceApplication application,
        PropertyProfile property)
    {
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = application.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var message = new CentrePointCalculationMessage(
            application.WoodlandOwnerId,
            application.CreatedById,
            application.Id,
            userAccessModel.IsFcUser,
            userAccessModel.AgencyId);

        const string error = "testError";
        
        var relevantPropertyProfileCompartments = PrepareCompartments(application, property);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _propertyProfileRepositoryMock.Setup(s =>
            s.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateCentrePointForApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(CentrePoint);

        _submitFellingLicenceService.Setup(s =>
                s.CalculateOSGridAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(OsGrid);

        _submitFellingLicenceService.Setup(s =>
                s.GetConfiguredFcAreaAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configuredFcArea);

        _updateCentrePointMock.Setup(s => s.UpdateCentrePointAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConfiguredFcArea>(error));

        var result = await sut.CalculateCentrePointAsync(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            It.IsAny<CancellationToken>()));

        _submitFellingLicenceService.Verify(v => v.CalculateCentrePointForApplicationAsync(
                application.Id,
                It.Is<List<string>>(x => x.SequenceEqual(relevantPropertyProfileCompartments.Select(y => y.GISData).ToList())),
                CancellationToken.None),
            Times.Once);
        _submitFellingLicenceService.Verify(v => v.CalculateOSGridAsync(CentrePoint, CancellationToken.None), Times.Once);
        _submitFellingLicenceService.Verify(v => v.GetConfiguredFcAreaAsync(CentrePoint, CancellationToken.None), Times.Once);
        
        _updateCentrePointMock.Verify(v => v.UpdateCentrePointAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            AreaCode, 
            AdminRegion,
            CentrePoint, 
            OsGrid, 
            CancellationToken.None), Times.Once);
       
        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.CalculateCentrePointForApplicationFailure
                && x.SourceEntityId == message.ApplicationId
                && x.UserId == message.UserId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = $"Unable to update the Area Code, Admin Region, Centre Point, and OS grid reference for application of id {message.ApplicationId}, with error {error}"
                }, _options)),
            CancellationToken.None), Times.Once);
        _submitFellingLicenceService.VerifyNoOtherCalls();
    }

    private static IEnumerable<Compartment> PrepareCompartments(FellingLicenceApplication application, PropertyProfile property)
    {
        var selectedCompartmentIds = application.LinkedPropertyProfile?.ProposedFellingDetails?
                        .Select(d => d.PropertyProfileCompartmentId).ToList();
        foreach (var compartment in property.Compartments)
        {
            compartment.Id = selectedCompartmentIds!.First();
        }
        var relevantPropertyProfileCompartments = property.Compartments.Where(p => selectedCompartmentIds!.Contains(p.Id));
        return relevantPropertyProfileCompartments;
    }

    private CalculateCentrePointUseCase CreateSut()
    {
        _optionsMock.Setup(x => x.Value).Returns(new InternalUserSiteOptions{ BaseUrl = BaseUrl});
        _getFellingLicenceService.Reset();
        _updateCentrePointMock.Reset();
        _auditMock.Reset();
        _propertyProfileRepositoryMock.Reset();

        return new CalculateCentrePointUseCase(
            _auditMock.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())),
            _submitFellingLicenceService.Object,
            _getFellingLicenceService.Object,
            _updateCentrePointMock.Object,
            new NullLogger<CalculateCentrePointUseCase>(),
            _optionsMock.Object,
            _propertyProfileRepositoryMock.Object);
    }
}