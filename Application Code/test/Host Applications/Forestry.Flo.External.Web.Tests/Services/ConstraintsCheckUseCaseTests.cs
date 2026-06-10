using AutoFixture;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using NodaTime;
using NodaTime.Testing;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services;

public class ConstraintsCheckUseCaseTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersMock = new();
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _retrieveFellingLicenceApplicationMock = new();
    private readonly Mock<IGetPropertyProfiles> _retrievePropertyProfilesMock = new();
    private readonly Mock<IGetCompartments> _retrieveCompartmentsMock = new();
    private readonly Mock<IAgentAuthorityService> _retrieveAgentAuthorityMock = new();
    private readonly Mock<IUpdateFellingLicenceApplicationForExternalUsers> _updateFellingLicenceApplicationMock = new();
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepositoryMock = new();
    private readonly Mock<IAuditService<ConstraintsCheckUseCase>> _auditMock = new();
    private readonly IClock _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private RequestContext _requestContext;

    private readonly Fixture _fixture = new();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task SetApplicationConstraintCheck_WhenUnableToGetUserAccess(
        ConstraintCheckModel model)
    {
        var sut = CreateSut();

        var user = GetExternalApplicant();

        _retrieveUserAccountsMock.Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>("error"));

        var result = await sut.SetApplicationConstraintCheckAsync(user, model, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(user.UserAccountId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SetApplicationConstraintCheck_WhenApplicationNotEditableByUser(
        ConstraintCheckModel model,
        UserAccessModel uam)
    {
        var sut = CreateSut();

        var user = GetExternalApplicant();

        _retrieveUserAccountsMock.Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _retrieveFellingLicenceApplicationMock.Setup(x =>
                x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var result = await sut.SetApplicationConstraintCheckAsync(user, model, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(user.UserAccountId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetIsEditable(model.ApplicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SetApplicationConstraintCheck_WhenUnableToLoadApplication(
        ConstraintCheckModel model,
        UserAccessModel uam)
    {
        var sut = CreateSut();

        var user = GetExternalApplicant();

        _retrieveUserAccountsMock.Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _retrieveFellingLicenceApplicationMock.Setup(x =>
                x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _retrieveFellingLicenceApplicationMock.Setup(x =>
                x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("error"));

        var result = await sut.SetApplicationConstraintCheckAsync(user, model, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(user.UserAccountId.Value, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetIsEditable(model.ApplicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.Verify(x =>
                x.GetApplicationByIdAsync(model.ApplicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SetApplicationConstraintCheck_WhenSavingChangesFails(
        ConstraintCheckModel model,
        UserAccessModel uam,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        var user = GetExternalApplicant();

        _retrieveUserAccountsMock.Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _retrieveFellingLicenceApplicationMock.Setup(x =>
                x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _retrieveFellingLicenceApplicationMock.Setup(x =>
                x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _unitOfWorkMock.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SetApplicationConstraintCheckAsync(user, model, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(user.UserAccountId.Value, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetIsEditable(model.ApplicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.Verify(x =>
            x.GetApplicationByIdAsync(model.ApplicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepositoryMock.Verify(x => x.Update(application), Times.Once);
        _fellingLicenceApplicationExternalRepositoryMock.VerifyGet(x => x.UnitOfWork, Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.VerifyNoOtherCalls();
        _fellingLicenceApplicationExternalRepositoryMock.VerifyNoOtherCalls();

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateFellingLicenceApplicationFailure
                && x.UserId == user.UserAccountId
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == model.ApplicationId
                && JsonSerializer.Serialize(x.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    application.WoodlandOwnerId,
                    Section = "Constraint Details",
                    Error = UserDbErrorReason.General.GetDescription()
                }, _serializerOptions)),
            CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SetApplicationConstraintCheck_WhenSuccessful(
        ConstraintCheckModel model,
        UserAccessModel uam,
        FellingLicenceApplication application)
    {
        model.ExternalLisReportRun = true;

        var sut = CreateSut();

        var user = GetExternalApplicant();

        _retrieveUserAccountsMock.Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _retrieveFellingLicenceApplicationMock.Setup(x =>
                x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _retrieveFellingLicenceApplicationMock.Setup(x =>
                x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _unitOfWorkMock.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SetApplicationConstraintCheckAsync(user, model, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(user.UserAccountId.Value, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetIsEditable(model.ApplicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.Verify(x =>
            x.GetApplicationByIdAsync(model.ApplicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepositoryMock.Verify(x => x.Update(application), Times.Once);
        _fellingLicenceApplicationExternalRepositoryMock.VerifyGet(x => x.UnitOfWork, Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.VerifyNoOtherCalls();
        _fellingLicenceApplicationExternalRepositoryMock.VerifyNoOtherCalls();

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateFellingLicenceApplication
                && x.UserId == user.UserAccountId
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == application.Id
                && JsonSerializer.Serialize(x.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    application.WoodlandOwnerId,
                    Section = "Constraint Details"
                }, _serializerOptions)),
            CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        Assert.Equal(model.NotRunningExternalLisReport, application.NotRunningExternalLisReport);
        Assert.Equal(model.StepComplete, application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus);

        Assert.Equal(UtcNow, application.ExternalLisAccessedTimestamp);
    }

    [Theory, AutoMoqData]
    public async Task RecordReceivedLisReport_WhenUnableToLoadApplication(Guid applicationId)
    {
        var sut = CreateSut();

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("error"));

        var result = await sut.RecordReceivedLisReportAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RecordReceivedLisReport_WhenSavingChangesFails(
        Guid applicationId,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _unitOfWorkMock.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.RecordReceivedLisReportAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepositoryMock.Verify(x => x.Update(application), Times.Once);
        _fellingLicenceApplicationExternalRepositoryMock.VerifyGet(x => x.UnitOfWork, Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.VerifyNoOtherCalls();
        _fellingLicenceApplicationExternalRepositoryMock.VerifyNoOtherCalls();

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateFellingLicenceApplicationFailure
                && x.UserId == null
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    application.WoodlandOwnerId,
                    Section = "Constraint Details",
                    Error = UserDbErrorReason.General.GetDescription()
                }, _serializerOptions)),
            CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RecordReceivedLisReport_WhenSuccessful(
        Guid applicationId,
        FellingLicenceApplication application)
    {
        application.NotRunningExternalLisReport = true;
        application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus = false;
        var existingAccessedTime = application.ExternalLisAccessedTimestamp!.Value;

        var sut = CreateSut();

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _unitOfWorkMock.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.RecordReceivedLisReportAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepositoryMock.Verify(x => x.Update(application), Times.Once);
        _fellingLicenceApplicationExternalRepositoryMock.VerifyGet(x => x.UnitOfWork, Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.VerifyNoOtherCalls();
        _fellingLicenceApplicationExternalRepositoryMock.VerifyNoOtherCalls();

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateFellingLicenceApplication
                && x.UserId == null
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    application.WoodlandOwnerId,
                    Section = "Constraint Details"
                }, _serializerOptions)),
            CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        Assert.False(application.NotRunningExternalLisReport);
        Assert.True(application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus);
        Assert.Equal(existingAccessedTime, application.ExternalLisAccessedTimestamp);
    }

    [Theory, AutoMoqData]
    public async Task RecordReceivedLisReport_WhenSuccessful_NoLisAccessedTimestampYet(
        Guid applicationId,
        FellingLicenceApplication application)
    {
        application.NotRunningExternalLisReport = true;
        application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus = false;
        application.ExternalLisAccessedTimestamp = null;

        var sut = CreateSut();

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _unitOfWorkMock.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.RecordReceivedLisReportAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepositoryMock.Verify(x => x.Update(application), Times.Once);
        _fellingLicenceApplicationExternalRepositoryMock.VerifyGet(x => x.UnitOfWork, Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.VerifyNoOtherCalls();
        _fellingLicenceApplicationExternalRepositoryMock.VerifyNoOtherCalls();

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateFellingLicenceApplication
                && x.UserId == null
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    application.WoodlandOwnerId,
                    Section = "Constraint Details"
                }, _serializerOptions)),
            CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        Assert.False(application.NotRunningExternalLisReport);
        Assert.True(application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus);
        Assert.Equal(_fixedTimeClock.GetCurrentInstant().ToDateTimeUtc(), application.ExternalLisAccessedTimestamp.Value);
    }

    private ExternalApplicant GetExternalApplicant(Guid? userId = null)
    {
        var userPrinciple = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId ?? Guid.NewGuid(), accountTypeExternal: AccountTypeExternal.FcUser, isFcUser: true);
        return new ExternalApplicant(userPrinciple);
    }

    private ConstraintsCheckUseCase CreateSut()
    {
        _fellingLicenceApplicationExternalRepositoryMock.Reset();
        _retrieveUserAccountsMock.Reset();
        _retrieveWoodlandOwnersMock.Reset();
        _retrieveFellingLicenceApplicationMock.Reset();
        _retrievePropertyProfilesMock.Reset();
        _retrieveCompartmentsMock.Reset();
        _retrieveAgentAuthorityMock.Reset();
        _auditMock.Reset();
        _unitOfWorkMock.Reset();

        _fellingLicenceApplicationExternalRepositoryMock
            .SetupGet(x => x.UnitOfWork)
            .Returns(_unitOfWorkMock.Object);

        var userPrinciple = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(), accountTypeExternal: AccountTypeExternal.FcUser);
        _requestContext = new("test", new RequestUserModel(userPrinciple));

        return new ConstraintsCheckUseCase(
            _fellingLicenceApplicationExternalRepositoryMock.Object,
            _retrieveUserAccountsMock.Object,
            _retrieveWoodlandOwnersMock.Object,
            _retrieveFellingLicenceApplicationMock.Object,
            _retrievePropertyProfilesMock.Object,
            _retrieveCompartmentsMock.Object,
            _retrieveAgentAuthorityMock.Object,
            _requestContext,
            _auditMock.Object,
            _fixedTimeClock,
            new NullLogger<ConstraintsCheckUseCase>());
    }
}