using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common; // Added for UserDbErrorReason
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewServiceAmendmentReviewTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    [Theory, AutoData]
    public async Task CreateFellingAndRestockingAmendmentReviewAsync_Succeeds_WhenValid(
        Guid applicationId,
        Guid woodlandOfficerUserId,
        DateTime responseDeadline,
        string amendmentsReason)
    {
        var sut = CreateSut();

        var woReview = new WoodlandOfficerReview
        {
            Id = Guid.NewGuid(),
            FellingLicenceApplicationId = applicationId
        };

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>
            {
                new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview }
            });

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>
            {
                new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = woodlandOfficerUserId }
            });

        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woReview));

        UnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        FellingLicenceApplicationRepository
            .Setup(x => x.AddFellingAndRestockingAmendmentReviewAsync(It.IsAny<FellingAndRestockingAmendmentReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.CreateFellingAndRestockingAmendmentReviewAsync(
            applicationId,
            woodlandOfficerUserId,
            responseDeadline,
            amendmentsReason,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(woReview.Id, result.Value.WoodlandOfficerReviewId);
        Assert.Equal(responseDeadline, result.Value.ResponseDeadline);
        Assert.Equal(amendmentsReason, result.Value.AmendmentsReason);
        Assert.Equal(woodlandOfficerUserId, result.Value.AmendingWoodlandOfficerId);
        Assert.InRange(result.Value.AmendmentsSentDate, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddFellingAndRestockingAmendmentReviewAsync(It.Is<FellingAndRestockingAmendmentReview>(r =>
                r.WoodlandOfficerReviewId == woReview.Id &&
                r.ResponseDeadline == responseDeadline &&
                r.AmendmentsReason == amendmentsReason &&
                r.AmendingWoodlandOfficerId == woodlandOfficerUserId),
            It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task CreateFellingAndRestockingAmendmentReviewAsync_ReturnsFailure_WhenNotInCorrectState(
        Guid applicationId,
        Guid woodlandOfficerUserId,
        DateTime responseDeadline,
        string amendmentsReason)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>());

        var result = await sut.CreateFellingAndRestockingAmendmentReviewAsync(
            applicationId,
            woodlandOfficerUserId,
            responseDeadline,
            amendmentsReason,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task CreateFellingAndRestockingAmendmentReviewAsync_ReturnsFailure_WhenUserNotAssignedWo(
        Guid applicationId,
        Guid woodlandOfficerUserId,
        DateTime responseDeadline,
        string amendmentsReason)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>
            {
                new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview }
            });

        // No WO assigned
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>());

        var result = await sut.CreateFellingAndRestockingAmendmentReviewAsync(
            applicationId,
            woodlandOfficerUserId,
            responseDeadline,
            amendmentsReason,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task CreateFellingAndRestockingAmendmentReviewAsync_ReturnsFailure_WhenRepositoryThrows(
        Guid applicationId,
        Guid woodlandOfficerUserId,
        DateTime responseDeadline,
        string amendmentsReason)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>
            {
                new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview }
            });

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>
            {
                new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = woodlandOfficerUserId }
            });

        // Throw when trying to get the WO review
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await sut.CreateFellingAndRestockingAmendmentReviewAsync(
            applicationId,
            woodlandOfficerUserId,
            responseDeadline,
            amendmentsReason,
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoData]
    public async Task CompleteFellingAndRestockingAmendmentReviewAsync_Succeeds_WhenRepositoryUpdates(
        Guid amendmentReviewId)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.SetAmendmentReviewCompletedAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.CompleteFellingAndRestockingAmendmentReviewAsync(amendmentReviewId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        FellingLicenceApplicationRepository.Verify(x => x.SetAmendmentReviewCompletedAsync(amendmentReviewId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task CompleteFellingAndRestockingAmendmentReviewAsync_ReturnsFailure_WhenRepositoryFails(
        Guid amendmentReviewId)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.SetAmendmentReviewCompletedAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.CompleteFellingAndRestockingAmendmentReviewAsync(amendmentReviewId, CancellationToken.None);

        Assert.True(result.IsFailure);
        FellingLicenceApplicationRepository.Verify(x => x.SetAmendmentReviewCompletedAsync(amendmentReviewId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // New tests for UpdateFellingAndRestockingAmendmentReviewAsync

    [Theory, AutoData]
    public async Task UpdateAmendmentReview_ReturnsFailure_WhenDisagreedWithoutReason(
        Guid applicationId,
        Guid applicantUserId)
    {
        var sut = CreateSut();
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = applicationId,
            ApplicantAgreed = false,
            ApplicantDisagreementReason = null
        };

        var result = await sut.UpdateFellingAndRestockingAmendmentReviewAsync(record, applicantUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        // No repository calls expected
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task UpdateAmendmentReview_ReturnsFailure_WhenNotInWoReviewState(
        Guid applicationId,
        Guid applicantUserId)
    {
        var sut = CreateSut();
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = applicationId,
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>()); // not in WO review

        var result = await sut.UpdateFellingAndRestockingAmendmentReviewAsync(record, applicantUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task UpdateAmendmentReview_ReturnsFailure_WhenUpdateLastUpdatedFails(
        Guid applicationId,
        Guid applicantUserId)
    {
        var sut = CreateSut();
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = applicationId,
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };

        // Pass AssertApplicationAsApplicant
        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.Author, AssignedUserId = applicantUserId } });

        // Cause UpdateWoodlandOfficerReviewLastUpdateDateAndBy to fail
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("wo failure"));

        var result = await sut.UpdateFellingAndRestockingAmendmentReviewAsync(record, applicantUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoData]
    public async Task UpdateAmendmentReview_ReturnsFailure_WhenGetCurrentFails(
        Guid applicationId,
        Guid applicantUserId)
    {
        var sut = CreateSut();
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = applicationId,
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };

        // Pass AssertApplicationAsApplicant
        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.Author, AssignedUserId = applicantUserId } });
        // Make UpdateWoodlandOfficerReviewLastUpdateDateAndBy succeed
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);
        FellingLicenceApplicationRepository
            .Setup(x => x.AddWoodlandOfficerReviewAsync(It.IsAny<WoodlandOfficerReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Fail the current review retrieval
        FellingLicenceApplicationRepository
            .Setup(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(Result.Failure<Maybe<FellingAndRestockingAmendmentReview>>("db error"));

        var result = await sut.UpdateFellingAndRestockingAmendmentReviewAsync(record, applicantUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoData]
    public async Task UpdateAmendmentReview_ReturnsFailure_WhenNoCurrentReviewFound(
        Guid applicationId,
        Guid applicantUserId)
    {
        var sut = CreateSut();
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = applicationId,
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };

        // Pass AssertApplicationAsApplicant and update date
        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.Author, AssignedUserId = applicantUserId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);
        FellingLicenceApplicationRepository
            .Setup(x => x.AddWoodlandOfficerReviewAsync(It.IsAny<WoodlandOfficerReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // No current review
        FellingLicenceApplicationRepository
            .Setup(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(Result.Success(Maybe<FellingAndRestockingAmendmentReview>.None));

        var result = await sut.UpdateFellingAndRestockingAmendmentReviewAsync(record, applicantUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoData]
    public async Task UpdateAmendmentReview_ReturnsFailure_WhenResponseAlreadyReceived(
        Guid applicationId,
        Guid applicantUserId)
    {
        var sut = CreateSut();
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = applicationId,
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };

        // Pass initial checks
        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.Author, AssignedUserId = applicantUserId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);
        FellingLicenceApplicationRepository
            .Setup(x => x.AddWoodlandOfficerReviewAsync(It.IsAny<WoodlandOfficerReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var review = new FellingAndRestockingAmendmentReview
        {
            WoodlandOfficerReviewId = Guid.NewGuid(),
            ResponseReceivedDate = DateTime.UtcNow
        };

        FellingLicenceApplicationRepository
            .Setup(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(Result.Success(Maybe<FellingAndRestockingAmendmentReview>.From(review)));

        var result = await sut.UpdateFellingAndRestockingAmendmentReviewAsync(record, applicantUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoData]
    public async Task UpdateAmendmentReview_ReturnsFailure_WhenSaveThrows(
        Guid applicationId,
        Guid applicantUserId)
    {
        var sut = CreateSut();
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = applicationId,
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };

        // Pass checks and provide current review
        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.Author, AssignedUserId = applicantUserId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);
        FellingLicenceApplicationRepository
            .Setup(x => x.AddWoodlandOfficerReviewAsync(It.IsAny<WoodlandOfficerReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var review = new FellingAndRestockingAmendmentReview { WoodlandOfficerReviewId = Guid.NewGuid() };
        FellingLicenceApplicationRepository
            .Setup(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(Result.Success(Maybe<FellingAndRestockingAmendmentReview>.From(review)));

        // Throw on save
        UnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("save failed"));

        var result = await sut.UpdateFellingAndRestockingAmendmentReviewAsync(record, applicantUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}
