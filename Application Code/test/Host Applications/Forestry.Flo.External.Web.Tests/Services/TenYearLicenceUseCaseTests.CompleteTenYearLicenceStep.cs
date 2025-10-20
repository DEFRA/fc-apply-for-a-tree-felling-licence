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
    public async Task CompleteTenYearLicenceStepWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.CompleteTenYearLicenceStepAsync(
            applicationId,
            user,
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
    public async Task CompleteTenYearLicenceStepWhenUpdateFails(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccess,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.CompleteTenYearLicenceStepAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.CompleteTenYearLicenceStepAsync(
            applicationId,
            user,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.Verify(x => x.CompleteTenYearLicenceStepAsync(applicationId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
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
                    isForTenYearLicence = true,
                    error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task CompleteTenYearLicenceStepWhenUpdateSucceeds(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccess)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.CompleteTenYearLicenceStepAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var user = GetExternalApplicant(userId);

        var result = await sut.CompleteTenYearLicenceStepAsync(
            applicationId,
            user,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.Verify(x => x.CompleteTenYearLicenceStepAsync(applicationId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
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
                    isForTenYearLicence = true
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }
}