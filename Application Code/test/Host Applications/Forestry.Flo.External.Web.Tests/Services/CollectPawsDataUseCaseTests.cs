using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using System.Text.Json;

namespace Forestry.Flo.External.Web.Tests.Services;

public partial class CollectPawsDataUseCaseTests
{
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersMock = new();
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _retrieveFellingLicenceApplicationMock = new();
    private readonly Mock<IGetPropertyProfiles> _retrievePropertyProfilesMock = new();
    private readonly Mock<IGetCompartments> _retrieveCompartmentsMock = new();
    private readonly Mock<IAgentAuthorityService> _retrieveAgentAuthorityMock = new();
    private readonly Mock<IUpdateFellingLicenceApplicationForExternalUsers> _updateFellingLicenceApplicationMock = new();
    private readonly Mock<IAuditService<CollectPawsDataUseCase>> _auditMock = new();

    private RequestContext _requestContext;

    private readonly Fixture _fixture = new();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        Guid? currentId,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenCannotRetrieveApplication(
        Guid applicationId,
        Guid userId,
        Guid? currentId,
        UserAccessModel userAccessModel,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenNoCompartmentDesignationsOnApplication(
        Guid applicationId,
        Guid userId,
        Guid? currentId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        application.LinkedPropertyProfile.ProposedCompartmentDesignations.Clear();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenCannotRetrieveUserAccessForRetrievingPropertyProfile(
        Guid applicationId,
        Guid userId,
        Guid? currentId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        string error)
    {
        var sut = CreateSut();

        var uamQueue = new Queue<Result<UserAccessModel>>();
        uamQueue.Enqueue(Result.Success(userAccessModel));
        uamQueue.Enqueue(Result.Failure<UserAccessModel>(error));

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => uamQueue.Dequeue());

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenCannotRetrievePropertyProfile(
        Guid applicationId,
        Guid userId,
        Guid? currentId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        string error)
    {
        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    // assume other failure scenarios for retrieving ApplicationSummary using the usecasebase methods are covered in other test classes

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenNoCompartmentDesignationsOnApplicationHaveMatchingStepStatuses(
        Guid applicationId,
        Guid userId,
        Guid? currentId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency)
    {
        var sut = CreateSut();

        // by default none of the CompartmentDesignationsStepStatuses will have compartment ids matching the CompartmentDesignations
        // on the linked property profile

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(4));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenNoCurrentIdReturnsFirstOneByName(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency)
    {
        var cpt1 = BuildCompartment("1a", propertyProfile.Id);
        var cpt2 = BuildCompartment("1b", propertyProfile.Id);
        var cpt3 = BuildCompartment("2a", propertyProfile.Id);

        propertyProfile.Compartments.Clear();
        propertyProfile.Compartments.AddRange([cpt3, cpt2, cpt1]);

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.AddRange([
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt2.Id)
                .Without(x => x.Status)
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt3.Id)
                .Without(x => x.Status)
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt1.Id)
                .Without(x => x.Status)
                .Create()
        ]);

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt3.Id)
                .With(x => x.CrossesPawsZones, ["ARW", "IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create(),
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt2.Id)
                .With(x => x.CrossesPawsZones, ["IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create(),
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt1.Id)
                .With(x => x.CrossesPawsZones, ["ARW", "IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create()
        ];

        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.NotNull(result.Value.ApplicationSummary);
        Assert.Equal(FellingLicenceStatus.Draft, result.Value.FellingLicenceStatus);
        Assert.True(result.Value.StepRequiredForApplication);
        Assert.Null(result.Value.StepComplete);

        var designation =
            application.LinkedPropertyProfile.ProposedCompartmentDesignations.Single(x =>
                x.Id == result.Value.CompartmentDesignation.Id);

        Assert.Equal(cpt1.CompartmentNumber, result.Value.CompartmentDesignation.PropertyProfileCompartmentName);
        Assert.Equal(cpt1.Id, result.Value.CompartmentDesignation.PropertyProfileCompartmentId);
        Assert.Equal(designation.Id, result.Value.CompartmentDesignation.Id);
        Assert.Equal(designation.CrossesPawsZones, result.Value.CompartmentDesignation.CrossesPawsZones);
        Assert.Equal(designation.ProportionBeforeFelling, result.Value.CompartmentDesignation.ProportionBeforeFelling);
        Assert.Equal(designation.ProportionAfterFelling, result.Value.CompartmentDesignation.ProportionAfterFelling);
        Assert.Equal(designation.IsRestoringCompartment, result.Value.CompartmentDesignation.IsRestoringCompartment);
        Assert.Equal(designation.RestorationDetails, result.Value.CompartmentDesignation.RestorationDetails);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(4));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenCurrentIdNotFoundReturnsFirstOneByName(
        Guid applicationId,
        Guid userId,
        Guid currentId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency)
    {
        var cpt1 = BuildCompartment("1a", propertyProfile.Id);
        var cpt2 = BuildCompartment("1b", propertyProfile.Id);
        var cpt3 = BuildCompartment("2a", propertyProfile.Id);

        propertyProfile.Compartments.Clear();
        propertyProfile.Compartments.AddRange([cpt3, cpt2, cpt1]);

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.AddRange([
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt2.Id)
                .Without(x => x.Status)
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt3.Id)
                .Without(x => x.Status)
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt1.Id)
                .Without(x => x.Status)
                .Create()
        ]);

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt3.Id)
                .With(x => x.CrossesPawsZones, ["ARW", "IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create(),
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt2.Id)
                .With(x => x.CrossesPawsZones, ["ARW"])
                .Without(x => x.LinkedPropertyProfile)
                .Create(),
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt1.Id)
                .With(x => x.CrossesPawsZones, ["ARW", "IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create()
        ];

        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.NotNull(result.Value.ApplicationSummary);
        Assert.Equal(FellingLicenceStatus.Draft, result.Value.FellingLicenceStatus);
        Assert.True(result.Value.StepRequiredForApplication);
        Assert.Null(result.Value.StepComplete);

        var designation =
            application.LinkedPropertyProfile.ProposedCompartmentDesignations.Single(x =>
                x.Id == result.Value.CompartmentDesignation.Id);

        Assert.Equal(cpt1.CompartmentNumber, result.Value.CompartmentDesignation.PropertyProfileCompartmentName);
        Assert.Equal(cpt1.Id, result.Value.CompartmentDesignation.PropertyProfileCompartmentId);
        Assert.Equal(designation.Id, result.Value.CompartmentDesignation.Id);
        Assert.Equal(designation.CrossesPawsZones, result.Value.CompartmentDesignation.CrossesPawsZones);
        Assert.Equal(designation.ProportionBeforeFelling, result.Value.CompartmentDesignation.ProportionBeforeFelling);
        Assert.Equal(designation.ProportionAfterFelling, result.Value.CompartmentDesignation.ProportionAfterFelling);
        Assert.Equal(designation.IsRestoringCompartment, result.Value.CompartmentDesignation.IsRestoringCompartment);
        Assert.Equal(designation.RestorationDetails, result.Value.CompartmentDesignation.RestorationDetails);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(4));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenCurrentIdFoundReturnsSpecificOne(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency)
    {
        var cpt1 = BuildCompartment("1a", propertyProfile.Id);
        var cpt2 = BuildCompartment("1b", propertyProfile.Id);
        var cpt3 = BuildCompartment("2a", propertyProfile.Id);

        propertyProfile.Compartments.Clear();
        propertyProfile.Compartments.AddRange([cpt3, cpt2, cpt1]);

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.AddRange([
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt2.Id)
                .Without(x => x.Status)
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt3.Id)
                .Without(x => x.Status)
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt1.Id)
                .Without(x => x.Status)
                .Create()
        ]);

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt3.Id)
                .With(x => x.CrossesPawsZones, ["ARW", "IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create(),
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt2.Id)
                .With(x => x.CrossesPawsZones, ["IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create(),
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt1.Id)
                .With(x => x.CrossesPawsZones, ["ARW", "IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create()
        ];

        var expected = application.LinkedPropertyProfile.ProposedCompartmentDesignations
            .Single(x => x.PropertyProfileCompartmentId == cpt2.Id);

        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            expected.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.NotNull(result.Value.ApplicationSummary);
        Assert.Equal(FellingLicenceStatus.Draft, result.Value.FellingLicenceStatus);
        Assert.True(result.Value.StepRequiredForApplication);
        Assert.Null(result.Value.StepComplete);

        Assert.Equal(cpt2.CompartmentNumber, result.Value.CompartmentDesignation.PropertyProfileCompartmentName);
        Assert.Equal(cpt2.Id, result.Value.CompartmentDesignation.PropertyProfileCompartmentId);
        Assert.Equal(expected.Id, result.Value.CompartmentDesignation.Id);
        Assert.Equal(expected.CrossesPawsZones, result.Value.CompartmentDesignation.CrossesPawsZones);
        Assert.Equal(expected.ProportionBeforeFelling, result.Value.CompartmentDesignation.ProportionBeforeFelling);
        Assert.Equal(expected.ProportionAfterFelling, result.Value.CompartmentDesignation.ProportionAfterFelling);
        Assert.Equal(expected.IsRestoringCompartment, result.Value.CompartmentDesignation.IsRestoringCompartment);
        Assert.Equal(expected.RestorationDetails, result.Value.CompartmentDesignation.RestorationDetails);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(4));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPawsDesignationsViewModelWhenCurrentIdFoundButHasNoStepStatusReturnsFirstOne(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency)
    {
        var cpt1 = BuildCompartment("1a", propertyProfile.Id);
        var cpt2 = BuildCompartment("1b", propertyProfile.Id);
        var cpt3 = BuildCompartment("2a", propertyProfile.Id);

        propertyProfile.Compartments.Clear();
        propertyProfile.Compartments.AddRange([cpt3, cpt2, cpt1]);

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.AddRange([
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt3.Id)
                .Without(x => x.Status)
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt1.Id)
                .Without(x => x.Status)
                .Create()
        ]);

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt3.Id)
                .With(x => x.CrossesPawsZones, ["ARW", "IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create(),
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt2.Id)
                .With(x => x.CrossesPawsZones, ["ARW"])
                .Without(x => x.LinkedPropertyProfile)
                .Create(),
            _fixture.Build<ProposedCompartmentDesignations>()
                .With(x => x.PropertyProfileCompartmentId, cpt1.Id)
                .With(x => x.CrossesPawsZones, ["ARW", "IAWPP"])
                .Without(x => x.LinkedPropertyProfile)
                .Create()
        ];

        var askedFor = application.LinkedPropertyProfile.ProposedCompartmentDesignations
            .Single(x => x.PropertyProfileCompartmentId == cpt2.Id);
        var expected = application.LinkedPropertyProfile.ProposedCompartmentDesignations
            .Single(x => x.PropertyProfileCompartmentId == cpt1.Id);

        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetPawsDesignationsViewModelAsync(
            user,
            applicationId,
            askedFor.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.NotNull(result.Value.ApplicationSummary);
        Assert.Equal(FellingLicenceStatus.Draft, result.Value.FellingLicenceStatus);
        Assert.True(result.Value.StepRequiredForApplication);
        Assert.Null(result.Value.StepComplete);

        Assert.Equal(cpt1.CompartmentNumber, result.Value.CompartmentDesignation.PropertyProfileCompartmentName);
        Assert.Equal(cpt1.Id, result.Value.CompartmentDesignation.PropertyProfileCompartmentId);
        Assert.Equal(expected.Id, result.Value.CompartmentDesignation.Id);
        Assert.Equal(expected.CrossesPawsZones, result.Value.CompartmentDesignation.CrossesPawsZones);
        Assert.Equal(expected.ProportionBeforeFelling, result.Value.CompartmentDesignation.ProportionBeforeFelling);
        Assert.Equal(expected.ProportionAfterFelling, result.Value.CompartmentDesignation.ProportionAfterFelling);
        Assert.Equal(expected.IsRestoringCompartment, result.Value.CompartmentDesignation.IsRestoringCompartment);
        Assert.Equal(expected.RestorationDetails, result.Value.CompartmentDesignation.RestorationDetails);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(4));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }


    private ExternalApplicant GetExternalApplicant(Guid? userId = null)
    {
        var userPrinciple = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId ?? Guid.NewGuid(), accountTypeExternal: AccountTypeExternal.FcUser, isFcUser: true);
        return new ExternalApplicant(userPrinciple);
    }

    private Compartment BuildCompartment(string name, Guid propertyProfileId, Guid? id = null)
    {
        id ??= Guid.NewGuid();

        var result = new Compartment(name, null, null, null, propertyProfileId);
        typeof(Compartment)
            .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .SetValue(result, id);

        return result;
    }

    private CollectPawsDataUseCase CreateSut()
    {
        _retrieveUserAccountsMock.Reset();
        _retrieveWoodlandOwnersMock.Reset();
        _retrieveFellingLicenceApplicationMock.Reset();
        _retrievePropertyProfilesMock.Reset();
        _retrieveCompartmentsMock.Reset();
        _retrieveAgentAuthorityMock.Reset();
        _updateFellingLicenceApplicationMock.Reset();
        _auditMock.Reset();

        var userPrinciple = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(), accountTypeExternal: AccountTypeExternal.FcUser);
        _requestContext = new("test", new RequestUserModel(userPrinciple));

        return new CollectPawsDataUseCase(
            _retrieveUserAccountsMock.Object,
            _retrieveWoodlandOwnersMock.Object,
            _retrieveFellingLicenceApplicationMock.Object,
            _retrievePropertyProfilesMock.Object,
            _retrieveCompartmentsMock.Object,
            _retrieveAgentAuthorityMock.Object,
            _updateFellingLicenceApplicationMock.Object,
            _auditMock.Object,
            _requestContext,
            new NullLogger<CollectPawsDataUseCase>());
    }
}