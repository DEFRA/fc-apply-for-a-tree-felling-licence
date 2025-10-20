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
using System.Text.Json;

namespace Forestry.Flo.External.Web.Tests.Services;

public partial class TenYearLicenceUseCaseTests
{
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersMock = new();
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _retrieveFellingLicenceApplicationMock = new();
    private readonly Mock<IGetPropertyProfiles> _retrievePropertyProfilesMock = new();
    private readonly Mock<IGetCompartments> _retrieveCompartmentsMock = new();
    private readonly Mock<IAgentAuthorityService> _retrieveAgentAuthorityMock = new();
    private readonly Mock<IUpdateFellingLicenceApplicationForExternalUsers> _updateFellingLicenceApplicationMock = new();
    private readonly Mock<IAuditService<TenYearLicenceUseCase>> _auditMock = new();
    
    private RequestContext _requestContext;

    private readonly Fixture _fixture = new();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task GetTenYearLicenceApplicationViewModelWhenCannotRetrieveUserAccess(
        Guid applicationId, 
        Guid userId, 
        bool returnToApplicationSummary, 
        bool fromDataImport,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetTenYearLicenceApplicationViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
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
    public async Task GetTenYearLicenceApplicationViewModelWhenCannotRetrieveApplication(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
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

        var result = await sut.GetTenYearLicenceApplicationViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
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
    public async Task GetTenYearLicenceApplicationViewModelWhenCannotRetrieveUserAccessForApplicationSummary(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
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

        var result = await sut.GetTenYearLicenceApplicationViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
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
    public async Task GetTenYearLicenceApplicationViewModelWhenCannotRetrievePropertyProfileForApplicationSummary(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
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

        var result = await sut.GetTenYearLicenceApplicationViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
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
    public async Task GetTenYearLicenceApplicationViewModelWhenCannotRetrieveSubmittedPropertyForApplicationSummary(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        string error)
    {
        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Submitted)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetExistingSubmittedFlaPropertyDetailAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Maybe<SubmittedFlaPropertyDetail>>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetTenYearLicenceApplicationViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetExistingSubmittedFlaPropertyDetailAsync(application.Id, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetTenYearLicenceApplicationViewModelWhenCannotRetrieveWoodlandOwnerForApplicationSummary(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
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
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwnerModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetTenYearLicenceApplicationViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task GetTenYearLicenceApplicationViewModelWhenSuccessForDraftApplication(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency)
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
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetTenYearLicenceApplicationViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var actual = result.Value;
        Assert.NotNull(actual);

        Assert.Equal(applicationId, actual.ApplicationId);
        Assert.Equal(returnToApplicationSummary, actual.ReturnToApplicationSummary);
        Assert.Equal(fromDataImport, actual.FromDataImport);
        Assert.Equal(FellingLicenceStatus.Draft, actual.FellingLicenceStatus);
        Assert.Equal(application.ApplicationReference, actual.ApplicationReference);
        Assert.Equal(application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus, actual.StepComplete);
        Assert.True(actual.StepRequiredForApplication);
        Assert.Equal(application.IsForTenYearLicence, actual.IsForTenYearLicence);
        Assert.Equal(application.WoodlandManagementPlanReference, actual.WoodlandManagementPlanReference);

        Assert.Equal(application.Id, actual.ApplicationSummary.Id);
        Assert.Equal(application.ApplicationReference, actual.ApplicationSummary.ApplicationReference);
        Assert.Equal(FellingLicenceStatus.Draft, actual.ApplicationSummary.Status);
        Assert.Equal(application.LinkedPropertyProfile.PropertyProfileId, actual.ApplicationSummary.PropertyProfileId);
        Assert.Equal(propertyProfile.Name, actual.ApplicationSummary.PropertyName);
        Assert.Equal(propertyProfile.NameOfWood, actual.ApplicationSummary.NameOfWood);
        Assert.Equal(application.WoodlandOwnerId, actual.ApplicationSummary.WoodlandOwnerId);
        Assert.Equal(woodlandOwner.GetContactNameForDisplay, actual.ApplicationSummary.WoodlandOwnerName);
        Assert.Equal(agency.OrganisationName ?? agency.ContactName, actual.ApplicationSummary.AgencyName);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetTenYearLicenceApplicationViewModelWhenSuccessForSubmittedApplication(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail submittedProperty,
        WoodlandOwnerModel woodlandOwner)
    {
        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Submitted)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetExistingSubmittedFlaPropertyDetailAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe<SubmittedFlaPropertyDetail>.From(submittedProperty)));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var user = GetExternalApplicant(userId);

        var result = await sut.GetTenYearLicenceApplicationViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var actual = result.Value;
        Assert.NotNull(actual);

        Assert.Equal(applicationId, actual.ApplicationId);
        Assert.Equal(returnToApplicationSummary, actual.ReturnToApplicationSummary);
        Assert.Equal(fromDataImport, actual.FromDataImport);
        Assert.Equal(FellingLicenceStatus.Submitted, actual.FellingLicenceStatus);
        Assert.Equal(application.ApplicationReference, actual.ApplicationReference);
        Assert.Equal(application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus, actual.StepComplete);
        Assert.True(actual.StepRequiredForApplication);
        Assert.Equal(application.IsForTenYearLicence, actual.IsForTenYearLicence);
        Assert.Equal(application.WoodlandManagementPlanReference, actual.WoodlandManagementPlanReference);

        Assert.Equal(application.Id, actual.ApplicationSummary.Id);
        Assert.Equal(application.ApplicationReference, actual.ApplicationSummary.ApplicationReference);
        Assert.Equal(FellingLicenceStatus.Submitted, actual.ApplicationSummary.Status);
        Assert.Equal(application.LinkedPropertyProfile.PropertyProfileId, actual.ApplicationSummary.PropertyProfileId);
        Assert.Equal(submittedProperty.Name, actual.ApplicationSummary.PropertyName);
        Assert.Equal(submittedProperty.NameOfWood, actual.ApplicationSummary.NameOfWood);
        Assert.Equal(application.WoodlandOwnerId, actual.ApplicationSummary.WoodlandOwnerId);
        Assert.Equal(woodlandOwner.GetContactNameForDisplay, actual.ApplicationSummary.WoodlandOwnerName);
        Assert.Null(actual.ApplicationSummary.AgencyName);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetExistingSubmittedFlaPropertyDetailAsync(application.Id, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

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

    private TenYearLicenceUseCase CreateSut()
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

        return new TenYearLicenceUseCase(
            _retrieveUserAccountsMock.Object,
            _retrieveWoodlandOwnersMock.Object,
            _retrieveFellingLicenceApplicationMock.Object,
            _retrievePropertyProfilesMock.Object,
            _retrieveCompartmentsMock.Object,
            _retrieveAgentAuthorityMock.Object,
            _updateFellingLicenceApplicationMock.Object,
            _auditMock.Object,
            _requestContext,
            new NullLogger<TenYearLicenceUseCase>());
    }

}