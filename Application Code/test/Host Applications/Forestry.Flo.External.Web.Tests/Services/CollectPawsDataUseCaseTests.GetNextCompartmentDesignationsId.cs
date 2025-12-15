using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Tests.Common;
using LinqKit;

namespace Forestry.Flo.External.Web.Tests.Services;

public partial class CollectPawsDataUseCaseTests
{
    [Theory, AutoMoqData]
    public async Task GetNextCompartmentDesignationsIdWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        Guid currentId,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
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
    public async Task GetNextCompartmentDesignationsIdWhenCannotRetrieveApplication(
        Guid applicationId,
        Guid userId,
        Guid currentId,
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

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
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
    public async Task GetNextCompartmentDesignationsIdWhenNoCompartmentDesignationsOnApplication(
        Guid applicationId,
        Guid userId,
        Guid currentId,
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

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
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
    public async Task GetNextCompartmentDesignationsIdWhenCannotRetrieveUserAccessForRetrievingPropertyProfile(
        Guid applicationId,
        Guid userId,
        Guid currentId,
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

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
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
    public async Task GetNextCompartmentDesignationsIdWhenCannotRetrievePropertyProfile(
        Guid applicationId,
        Guid userId,
        Guid currentId,
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

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
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
    public async Task GetNextCompartmentDesignationsIdWhenNoDesignationStepStatusesAndIsEiaApplication(
        Guid applicationId,
        Guid userId,
        Guid currentId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile)
    {
        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        application.LinkedPropertyProfile.ProposedFellingDetails[0].OperationType =
            FellingOperationType.FellingIndividualTrees;
        application.LinkedPropertyProfile.ProposedFellingDetails[0].IsRestocking = false;
        application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails = [];

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.NextCompartmentDesignationsId);
        Assert.True(result.Value.RequiresEia);

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

    [Theory, AutoMoqData]
    public async Task GetNextCompartmentDesignationsIdWhenNoDesignationStepStatusesAndIsNotEiaApplication(
        Guid applicationId,
        Guid userId,
        Guid currentId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile)
    {
        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        application.LinkedPropertyProfile.ProposedFellingDetails.ForEach(x =>
        {
            x.OperationType = FellingOperationType.Thinning;
            application.LinkedPropertyProfile.ProposedFellingDetails[0].IsRestocking = false;
            application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails = [];
        });

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.NextCompartmentDesignationsId);
        Assert.False(result.Value.RequiresEia);

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

    [Theory, AutoMoqData]
    public async Task GetNextCompartmentDesignationsIdWhenCurrentIdIsFinalDesignationEntry(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile)
    {
        var sut = CreateSut();

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
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt2.Id)
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
            .Single(x => x.PropertyProfileCompartmentId == cpt3.Id);

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        application.LinkedPropertyProfile.ProposedFellingDetails[0].OperationType =
            FellingOperationType.FellingIndividualTrees;
        application.LinkedPropertyProfile.ProposedFellingDetails[0].IsRestocking = false;
        application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails = [];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
            user,
            applicationId,
            askedFor.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.NextCompartmentDesignationsId);
        Assert.True(result.Value.RequiresEia);

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

    [Theory, AutoMoqData]
    public async Task GetNextCompartmentDesignationsIdWhenCurrentIdIsUnknownReturnsNull(
        Guid applicationId,
        Guid userId,
        Guid currentId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile)
    {
        var sut = CreateSut();

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
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt2.Id)
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

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        application.LinkedPropertyProfile.ProposedFellingDetails[0].OperationType =
            FellingOperationType.FellingIndividualTrees;
        application.LinkedPropertyProfile.ProposedFellingDetails[0].IsRestocking = false;
        application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails = [];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
            user,
            applicationId,
            currentId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.NextCompartmentDesignationsId);
        Assert.True(result.Value.RequiresEia);

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

    [Theory, AutoMoqData]
    public async Task GetNextCompartmentDesignationsIdWhenCurrentIdIsNotTheLastReturnsTheNext(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile)
    {
        var sut = CreateSut();

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
                .Create(),
            _fixture.Build<CompartmentDesignationStatus>()
                .With(x => x.CompartmentId, cpt2.Id)
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

        var current = application.LinkedPropertyProfile.ProposedCompartmentDesignations
            .Single(x => x.PropertyProfileCompartmentId == cpt1.Id);
        var expected = application.LinkedPropertyProfile.ProposedCompartmentDesignations
            .Single(x => x.PropertyProfileCompartmentId == cpt2.Id);

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        application.LinkedPropertyProfile.ProposedFellingDetails[0].OperationType =
            FellingOperationType.FellingIndividualTrees;
        application.LinkedPropertyProfile.ProposedFellingDetails[0].IsRestocking = false;
        application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails = [];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetNextCompartmentDesignationsIdAsync(
            user,
            applicationId,
            current.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected.Id, result.Value.NextCompartmentDesignationsId);
        Assert.True(result.Value.RequiresEia);

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
}