using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ApproverReviewServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();
    private readonly Mock<IClock> _clockMock = new();
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly Fixture _fixture = new();

    [Theory, AutoData]
    public async Task GetWhenNoApproverReviewExistsYet(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.None);

        var result = await sut.GetApproverReviewAsync(applicationId, CancellationToken.None);

        Assert.True(result.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetWhenApproverReviewExists(Guid applicationId, ApproverReview approverReview)
    {
        var sut = CreateSut();
        
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.From(approverReview));
        
        var result = await sut.GetApproverReviewAsync(applicationId, CancellationToken.None);
     
        Assert.True(result.HasValue);
        Assert.Equal(approverReview.FellingLicenceApplicationId, result.Value.ApplicationId);
        Assert.Equal(approverReview.RequestedStatus, result.Value.RequestedStatus);
        Assert.Equal(approverReview.CheckedApplication, result.Value.CheckedApplication);
        Assert.Equal(approverReview.CheckedDocumentation, result.Value.CheckedDocumentation);
        Assert.Equal(approverReview.CheckedCaseNotes, result.Value.CheckedCaseNotes);
        Assert.Equal(approverReview.CheckedWOReview, result.Value.CheckedWOReview);
        Assert.Equal(approverReview.InformedApplicant, result.Value.InformedApplicant);
        Assert.Equal(approverReview.ApprovedLicenceDuration, result.Value.ApprovedLicenceDuration);
        Assert.Equal(approverReview.DurationChangeReason, result.Value.DurationChangeReason);
        Assert.Equal(approverReview.PublicRegisterPublish, result.Value.PublicRegisterPublish);
        Assert.Equal(approverReview.PublicRegisterExemptionReason, result.Value.PublicRegisterExemptionReason);

        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task AddApproverReviewWhenApplicationHasNoStatusHistory(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task AddApproverReviewWhenApplicationIsNotInCorrectState(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId)
    {
        var sut = CreateSut();

        var statusHistory = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ statusHistory ]);

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task AddApproverReviewWhenApplicationIsNotAssignedToAnyone(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId)
    {
        var sut = CreateSut();

        var statusHistory = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.SentForApproval)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([statusHistory]);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task AddApproverReviewWhenApplicationIsAssignedToAnotherFieldManager(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId)
    {
        var sut = CreateSut();

        var statusHistory = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.SentForApproval)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([statusHistory]);

        var assignee = _fixture
            .Build<AssigneeHistory>()
            .With(x => x.Role, AssignedUserRole.FieldManager)
            .Without(x => x.TimestampUnassigned)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ assignee ]);

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task AddApproverReviewWhenApplicationIsUnassignedFromPerformingUser(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId)
    {
        var sut = CreateSut();

        var statusHistory = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.SentForApproval)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([statusHistory]);

        var assignee = _fixture
            .Build<AssigneeHistory>()
            .With(x => x.Role, AssignedUserRole.FieldManager)
            .With(x => x.AssignedUserId, userId)
            .Without(x => x.FellingLicenceApplication)
            .Create();  // this will have a set TimestampUnassigned

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([assignee]);

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddApproverReviewWhenSaveThrows(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId)
    {
        var sut = CreateSut();

        var statusHistory = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.SentForApproval)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([statusHistory]);

        var assignee = _fixture
            .Build<AssigneeHistory>()
            .With(x => x.Role, AssignedUserRole.FieldManager)
            .With(x => x.AssignedUserId, userId)
            .Without(x => x.TimestampUnassigned)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([assignee]);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateApproverReviewAsync(It.IsAny<ApproverReview>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test Exception"));

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateApproverReviewAsync(It.Is<ApproverReview>(ar =>
            ar.FellingLicenceApplicationId == model.ApplicationId
            && ar.RequestedStatus == model.RequestedStatus
            && ar.CheckedApplication == model.CheckedApplication
            && ar.CheckedDocumentation == model.CheckedDocumentation
            && ar.CheckedCaseNotes == model.CheckedCaseNotes
            && ar.CheckedWOReview == model.CheckedWOReview
            && ar.InformedApplicant == model.InformedApplicant
            && ar.ApprovedLicenceDuration == model.ApprovedLicenceDuration
            && ar.DurationChangeReason == model.DurationChangeReason
            && ar.PublicRegisterPublish == model.PublicRegisterPublish
            && ar.PublicRegisterExemptionReason == model.PublicRegisterExemptionReason
            && ar.LastUpdatedById == userId
            && ar.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddApproverReviewWhenSaveFails(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId)
    {
        var sut = CreateSut();

        var statusHistory = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.SentForApproval)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([statusHistory]);

        var assignee = _fixture
            .Build<AssigneeHistory>()
            .With(x => x.Role, AssignedUserRole.FieldManager)
            .With(x => x.AssignedUserId, userId)
            .Without(x => x.TimestampUnassigned)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([assignee]);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateApproverReviewAsync(It.IsAny<ApproverReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateApproverReviewAsync(It.Is<ApproverReview>(ar =>
            ar.FellingLicenceApplicationId == model.ApplicationId
            && ar.RequestedStatus == model.RequestedStatus
            && ar.CheckedApplication == model.CheckedApplication
            && ar.CheckedDocumentation == model.CheckedDocumentation
            && ar.CheckedCaseNotes == model.CheckedCaseNotes
            && ar.CheckedWOReview == model.CheckedWOReview
            && ar.InformedApplicant == model.InformedApplicant
            && ar.ApprovedLicenceDuration == model.ApprovedLicenceDuration
            && ar.DurationChangeReason == model.DurationChangeReason
            && ar.PublicRegisterPublish == model.PublicRegisterPublish
            && ar.PublicRegisterExemptionReason == model.PublicRegisterExemptionReason
            && ar.LastUpdatedById == userId
            && ar.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddApproverReviewWhenSuccess(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId)
    {
        var sut = CreateSut();

        var statusHistory = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.SentForApproval)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([statusHistory]);

        var assignee = _fixture
            .Build<AssigneeHistory>()
            .With(x => x.Role, AssignedUserRole.FieldManager)
            .With(x => x.AssignedUserId, userId)
            .Without(x => x.TimestampUnassigned)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([assignee]);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateApproverReviewAsync(It.IsAny<ApproverReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateApproverReviewAsync(It.Is<ApproverReview>(ar =>
            ar.FellingLicenceApplicationId == model.ApplicationId
            && ar.RequestedStatus == model.RequestedStatus
            && ar.CheckedApplication == model.CheckedApplication
            && ar.CheckedDocumentation == model.CheckedDocumentation
            && ar.CheckedCaseNotes == model.CheckedCaseNotes
            && ar.CheckedWOReview == model.CheckedWOReview
            && ar.InformedApplicant == model.InformedApplicant
            && ar.ApprovedLicenceDuration == model.ApprovedLicenceDuration
            && ar.DurationChangeReason == model.DurationChangeReason
            && ar.PublicRegisterPublish == model.PublicRegisterPublish
            && ar.PublicRegisterExemptionReason == model.PublicRegisterExemptionReason
            && ar.LastUpdatedById == userId
            && ar.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateApproverReviewWhenSuccess(
        Guid applicationId,
        ApproverReviewModel model,
        ApproverReview existingReview,
        Guid userId)
    {
        var sut = CreateSut();

        var statusHistory = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.SentForApproval)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([statusHistory]);

        var assignee = _fixture
            .Build<AssigneeHistory>()
            .With(x => x.Role, AssignedUserRole.FieldManager)
            .With(x => x.AssignedUserId, userId)
            .Without(x => x.TimestampUnassigned)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([assignee]);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.From(existingReview));

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateApproverReviewAsync(It.IsAny<ApproverReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SaveApproverReviewAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateApproverReviewAsync(existingReview, It.IsAny<CancellationToken>()), Times.Once);
        
        Assert.Equal(model.ApplicationId, existingReview.FellingLicenceApplicationId);
        Assert.Equal(model.RequestedStatus, existingReview.RequestedStatus);
        Assert.Equal(model.CheckedApplication, existingReview.CheckedApplication);
        Assert.Equal(model.CheckedDocumentation, existingReview.CheckedDocumentation);
        Assert.Equal(model.CheckedCaseNotes, existingReview.CheckedCaseNotes);
        Assert.Equal(model.CheckedWOReview, existingReview.CheckedWOReview);
        Assert.Equal(model.InformedApplicant, existingReview.InformedApplicant);
        Assert.Equal(model.ApprovedLicenceDuration, existingReview.ApprovedLicenceDuration);
        Assert.Equal(model.DurationChangeReason, existingReview.DurationChangeReason);
        Assert.Equal(model.PublicRegisterPublish, existingReview.PublicRegisterPublish);
        Assert.Equal(model.PublicRegisterExemptionReason, existingReview.PublicRegisterExemptionReason);
        Assert.Equal(userId, existingReview.LastUpdatedById);
        Assert.Equal(_now, existingReview.LastUpdatedDate);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task DeleteApproverReviewWhenNoneExistsForId(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.None);

        var result = await sut.DeleteApproverReviewAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task DeleteApproverReviewWhenDeleteFails(Guid applicationId, ApproverReview existingEntity)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.DeleteApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.DeleteApproverReviewAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.DeleteApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task DeleteApproverReviewWhenDeleteThrows(Guid applicationId, ApproverReview existingEntity)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.DeleteApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.DeleteApproverReviewAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.DeleteApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task DeleteApproverReviewSuccess(Guid applicationId, ApproverReview existingEntity)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReview>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.DeleteApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.DeleteApproverReviewAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fellingLicenceApplicationRepository.Verify(x => x.GetApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.DeleteApproverReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    private ApproverReviewService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();
        _clockMock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_now));

        return new ApproverReviewService(
            _fellingLicenceApplicationRepository.Object,
            _clockMock.Object,
            new NullLogger<ApproverReviewService>());
    }
}