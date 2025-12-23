using System.Security.Claims;
using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services;

public class CheckForPawsRequirementUseCaseTests
{
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceApplicationService = new();
    private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesService = new();
    private readonly Mock<IUpdateApplicationFromForesterLayers> _updateApplicationFromForesterLayers = new();
    private readonly Mock<IAuditService<CheckForPawsRequirementUseCase>> _auditService = new();
    private RequestContext _requestContext;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoData]
    public async Task WhenCannotRetrieveApplication(PawsRequirementCheckMessage message, string error)
    {
        var sut = CreateSut();

        _getFellingLicenceApplicationService.Setup(x => x.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

        var result = await sut.CheckAndUpdateApplicationForPaws(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceApplicationService.Verify(x => x.GetApplicationByIdAsync(
            It.Is<Guid>(id => id == message.ApplicationId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getFellingLicenceApplicationService.VerifyNoOtherCalls();

        _getPropertyProfilesService.VerifyNoOtherCalls();

        _updateApplicationFromForesterLayers.VerifyNoOtherCalls();

        var errorMessage = $"Failed to retrieve application with id {message.ApplicationId} to check compartments for PAWS: {error}";
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.PawsRequirementCheckFailed
                && x.UserId == message.UserId
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = errorMessage
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrievePropertyProfile(
        PawsRequirementCheckMessage message, 
        FellingLicenceApplication application,
        string error)
    {
        var sut = CreateSut();

        _getFellingLicenceApplicationService.Setup(x => x.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _getPropertyProfilesService.Setup(x => x.GetPropertyByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile>(error));

        var result = await sut.CheckAndUpdateApplicationForPaws(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceApplicationService.Verify(x => x.GetApplicationByIdAsync(
            It.Is<Guid>(id => id == message.ApplicationId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getFellingLicenceApplicationService.VerifyNoOtherCalls();

        _getPropertyProfilesService.Verify(x => x.GetPropertyByIdAsync(
            It.Is<Guid>(id => id == application.LinkedPropertyProfile.PropertyProfileId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _updateApplicationFromForesterLayers.VerifyNoOtherCalls();

        var errorMessage = $"Failed to retrieve property with id {application.LinkedPropertyProfile?.PropertyProfileId} on application with id {message.ApplicationId} to check compartments for PAWS: {error}";
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.PawsRequirementCheckFailed
                && x.UserId == message.UserId
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = errorMessage
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCompartmentsOnPropertyProfileDoNotMatchApplication(
        PawsRequirementCheckMessage message,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile)
    {
        var sut = CreateSut();

        _getFellingLicenceApplicationService.Setup(x => x.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _getPropertyProfilesService.Setup(x => x.GetPropertyByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        var result = await sut.CheckAndUpdateApplicationForPaws(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceApplicationService.Verify(x => x.GetApplicationByIdAsync(
            It.Is<Guid>(id => id == message.ApplicationId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getFellingLicenceApplicationService.VerifyNoOtherCalls();

        _getPropertyProfilesService.Verify(x => x.GetPropertyByIdAsync(
            It.Is<Guid>(id => id == application.LinkedPropertyProfile.PropertyProfileId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _updateApplicationFromForesterLayers.VerifyNoOtherCalls();

        var errorMessage = $"Failed to retrieve all selected compartments or GIS data is missing for application with id {message.ApplicationId} to check compartments for PAWS.";
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.PawsRequirementCheckFailed
                && x.UserId == message.UserId
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = errorMessage
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCompartmentsOnPropertyProfileDoNotHaveGisData(
        PawsRequirementCheckMessage message,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        ProposedFellingDetail felling,
        ProposedRestockingDetail restocking)
    {

        felling.PropertyProfileCompartmentId = propertyProfile.Compartments.First().Id;
        restocking.PropertyProfileCompartmentId = propertyProfile.Compartments.Last().Id;
        felling.ProposedRestockingDetails = [ restocking ];
        propertyProfile.Compartments.First().GISData = null;
        application.LinkedPropertyProfile.ProposedFellingDetails = [ felling ];

        var sut = CreateSut();

        _getFellingLicenceApplicationService.Setup(x => x.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _getPropertyProfilesService.Setup(x => x.GetPropertyByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        var result = await sut.CheckAndUpdateApplicationForPaws(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceApplicationService.Verify(x => x.GetApplicationByIdAsync(
            It.Is<Guid>(id => id == message.ApplicationId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getFellingLicenceApplicationService.VerifyNoOtherCalls();

        _getPropertyProfilesService.Verify(x => x.GetPropertyByIdAsync(
            It.Is<Guid>(id => id == application.LinkedPropertyProfile.PropertyProfileId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _updateApplicationFromForesterLayers.VerifyNoOtherCalls();

        var errorMessage = $"Failed to retrieve all selected compartments or GIS data is missing for application with id {message.ApplicationId} to check compartments for PAWS.";
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.PawsRequirementCheckFailed
                && x.UserId == message.UserId
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = errorMessage
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdateFails(
        PawsRequirementCheckMessage message,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        ProposedFellingDetail felling,
        ProposedRestockingDetail restocking,
        string error)
    {

        felling.PropertyProfileCompartmentId = propertyProfile.Compartments.First().Id;
        restocking.PropertyProfileCompartmentId = propertyProfile.Compartments.Last().Id;
        felling.ProposedRestockingDetails = [restocking];
        application.LinkedPropertyProfile.ProposedFellingDetails = [felling];

        var sut = CreateSut();

        _getFellingLicenceApplicationService.Setup(x => x.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _getPropertyProfilesService.Setup(x => x.GetPropertyByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _updateApplicationFromForesterLayers.Setup(x => x.UpdateForPawsLayersAsync(
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<Guid, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.CheckAndUpdateApplicationForPaws(message, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getFellingLicenceApplicationService.Verify(x => x.GetApplicationByIdAsync(
            It.Is<Guid>(id => id == message.ApplicationId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getFellingLicenceApplicationService.VerifyNoOtherCalls();

        _getPropertyProfilesService.Verify(x => x.GetPropertyByIdAsync(
            It.Is<Guid>(id => id == application.LinkedPropertyProfile.PropertyProfileId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _updateApplicationFromForesterLayers.Verify(x => x.UpdateForPawsLayersAsync(
            It.Is<Guid>(id => id == message.ApplicationId),
            It.Is<Dictionary<Guid, string>>(d => 
                d[felling.PropertyProfileCompartmentId] == propertyProfile.Compartments.First().GISData
                && d[restocking.PropertyProfileCompartmentId] == propertyProfile.Compartments.Last().GISData),
            It.IsAny<CancellationToken>()), Times.Once);
        _updateApplicationFromForesterLayers.VerifyNoOtherCalls();

        var errorMessage = $"Failed to update application with id {message.ApplicationId} after checking compartments for PAWS: {error}";
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.PawsRequirementCheckFailed
                && x.UserId == message.UserId
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = errorMessage
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdateSucceeds(
        PawsRequirementCheckMessage message,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        ProposedFellingDetail felling,
        ProposedRestockingDetail restocking)
    {

        felling.PropertyProfileCompartmentId = propertyProfile.Compartments.First().Id;
        restocking.PropertyProfileCompartmentId = propertyProfile.Compartments.Last().Id;
        felling.ProposedRestockingDetails = [restocking];
        application.LinkedPropertyProfile.ProposedFellingDetails = [felling];

        var sut = CreateSut();

        _getFellingLicenceApplicationService.Setup(x => x.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _getPropertyProfilesService.Setup(x => x.GetPropertyByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _updateApplicationFromForesterLayers.Setup(x => x.UpdateForPawsLayersAsync(
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<Guid, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.CheckAndUpdateApplicationForPaws(message, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _getFellingLicenceApplicationService.Verify(x => x.GetApplicationByIdAsync(
            It.Is<Guid>(id => id == message.ApplicationId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getFellingLicenceApplicationService.VerifyNoOtherCalls();

        _getPropertyProfilesService.Verify(x => x.GetPropertyByIdAsync(
            It.Is<Guid>(id => id == application.LinkedPropertyProfile.PropertyProfileId),
            It.Is<UserAccessModel>(u =>
                u.IsFcUser == message.IsFcUser &&
                u.AgencyId == message.AgencyId &&
                u.UserAccountId == message.UserId &&
                u.WoodlandOwnerIds.SequenceEqual(new List<Guid> { message.WoodlandOwnerId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _updateApplicationFromForesterLayers.Verify(x => x.UpdateForPawsLayersAsync(
            It.Is<Guid>(id => id == message.ApplicationId),
            It.Is<Dictionary<Guid, string>>(d =>
                d[felling.PropertyProfileCompartmentId] == propertyProfile.Compartments.First().GISData
                && d[restocking.PropertyProfileCompartmentId] == propertyProfile.Compartments.Last().GISData),
            It.IsAny<CancellationToken>()), Times.Once);
        _updateApplicationFromForesterLayers.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.PawsRequirementCheckCompleted
                && x.UserId == message.UserId
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == message.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    private CheckForPawsRequirementUseCase CreateSut()
    {
        _getFellingLicenceApplicationService.Reset();
        _getPropertyProfilesService.Reset();
        _updateApplicationFromForesterLayers.Reset();
        _auditService.Reset();

        _requestContext = new RequestContext("test", new RequestUserModel(new ClaimsPrincipal()));

        return new CheckForPawsRequirementUseCase(
            _requestContext,
            _getFellingLicenceApplicationService.Object,
            _getPropertyProfilesService.Object,
            _updateApplicationFromForesterLayers.Object,
            _auditService.Object,
            new NullLogger<CheckForPawsRequirementUseCase>());
    }
}