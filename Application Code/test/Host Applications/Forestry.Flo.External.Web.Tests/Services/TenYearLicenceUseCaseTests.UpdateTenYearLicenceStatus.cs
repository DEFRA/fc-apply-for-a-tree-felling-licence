using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services;

public partial class TenYearLicenceUseCaseTests
{
    [Theory, AutoMoqData]
    public async Task UpdateTenYearLicenceStatusWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        bool isForTenYearLicence,
        string? woodlandManagementPlanReference,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            applicationId,
            user,
            isForTenYearLicence,
            woodlandManagementPlanReference,
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
    public async Task UpdateTenYearLicenceStatusForTenYearLicenceWhenUpdateFails(
        Guid applicationId,
        Guid userId,
        string? woodlandManagementPlanReference,
        UserAccessModel userAccessModel,
        string error)
    {
        const bool isForTenYearLicence = true;

        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateTenYearLicenceStatusAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            applicationId,
            user,
            isForTenYearLicence,
            woodlandManagementPlanReference,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.Verify(x => x.UpdateTenYearLicenceStatusAsync(applicationId, userAccessModel, isForTenYearLicence, woodlandManagementPlanReference, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.TenYearLicenceStepUpdatedFailure
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == user.UserAccountId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    isForTenYearLicence,
                    error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateTenYearLicenceStatusForNotTenYearLicenceWhenUpdateFails(
        Guid applicationId,
        Guid userId,
        string? woodlandManagementPlanReference,
        UserAccessModel userAccessModel,
        string error)
    {
        const bool isForTenYearLicence = false;

        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateTenYearLicenceStatusAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            applicationId,
            user,
            isForTenYearLicence,
            woodlandManagementPlanReference,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.Verify(x => x.UpdateTenYearLicenceStatusAsync(applicationId, userAccessModel, isForTenYearLicence, woodlandManagementPlanReference, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.TenYearLicenceStepCompletedFailure
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == user.UserAccountId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    isForTenYearLicence,
                    error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateTenYearLicenceStatusForTenYearLicenceWhenUpdateSucceeds(
        Guid applicationId,
        Guid userId,
        string? woodlandManagementPlanReference,
        UserAccessModel userAccessModel)
    {
        const bool isForTenYearLicence = true;

        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateTenYearLicenceStatusAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var user = GetExternalApplicant(userId);

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            applicationId,
            user,
            isForTenYearLicence,
            woodlandManagementPlanReference,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.Verify(x => x.UpdateTenYearLicenceStatusAsync(applicationId, userAccessModel, isForTenYearLicence, woodlandManagementPlanReference, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.TenYearLicenceStepUpdated
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == user.UserAccountId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    isForTenYearLicence
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateTenYearLicenceStatusForNotTenYearLicenceWhenUpdateSucceeds(
        Guid applicationId,
        Guid userId,
        string? woodlandManagementPlanReference,
        UserAccessModel userAccessModel)
    {
        const bool isForTenYearLicence = false;

        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateTenYearLicenceStatusAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var user = GetExternalApplicant(userId);

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            applicationId,
            user,
            isForTenYearLicence,
            woodlandManagementPlanReference,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.Verify(x => x.UpdateTenYearLicenceStatusAsync(applicationId, userAccessModel, isForTenYearLicence, woodlandManagementPlanReference, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.TenYearLicenceStepCompleted
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == user.UserAccountId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    isForTenYearLicence
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }
}