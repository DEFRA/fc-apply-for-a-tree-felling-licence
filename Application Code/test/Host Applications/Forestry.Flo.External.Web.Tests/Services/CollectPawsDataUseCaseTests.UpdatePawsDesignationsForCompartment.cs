using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services;

public partial class CollectPawsDataUseCaseTests
{
    [Theory, AutoMoqData]
    public async Task UpdatePawsDesignationsForCompartmentWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        PawsCompartmentDesignationsModel model,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.UpdatePawsDesignationsForCompartmentAsync(
            user,
            applicationId,
            model,
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


        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.PawsDesignationsUpdateFailure
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == user.UserAccountId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    compartmentId = model.PropertyProfileCompartmentId,
                    error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdatePawsDesignationsForCompartmentWhenUpdateFails(
        Guid applicationId,
        Guid userId,
        PawsCompartmentDesignationsModel model,
        UserAccessModel uam,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateApplicationPawsDesignationsDataAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<PawsCompartmentDesignationsModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.UpdatePawsDesignationsForCompartmentAsync(
            user,
            applicationId,
            model,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.Verify(x => x.UpdateApplicationPawsDesignationsDataAsync(applicationId, uam, model, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();


        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.PawsDesignationsUpdateFailure
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == user.UserAccountId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    compartmentId = model.PropertyProfileCompartmentId,
                    error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdatePawsDesignationsForCompartmentWhenUpdateSucceeds(
        Guid applicationId,
        Guid userId,
        PawsCompartmentDesignationsModel model,
        UserAccessModel uam)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateApplicationPawsDesignationsDataAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<PawsCompartmentDesignationsModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var user = GetExternalApplicant(userId);

        var result = await sut.UpdatePawsDesignationsForCompartmentAsync(
            user,
            applicationId,
            model,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.Verify(x => x.UpdateApplicationPawsDesignationsDataAsync(applicationId, uam, model, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();


        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.PawsDesignationsUpdate
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == user.UserAccountId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    compartmentId = model.PropertyProfileCompartmentId
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }
}