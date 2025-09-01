using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewServiceSetSiteVisitNotNeededTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    [Theory, AutoData]
    public async Task ReturnsFailureWhenRepositoryThrows(
        Guid applicationId,
        Guid userId,
        FormLevelCaseNote reason)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("done gone wrong"));

        var result = await sut.SetSiteVisitNotNeededAsync(applicationId, userId, reason, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNotInCorrectState(
        Guid applicationId,
        Guid userId,
        FormLevelCaseNote reason)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>(0));

        var result = await sut.SetSiteVisitNotNeededAsync(applicationId, userId, reason, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNoAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        FormLevelCaseNote reason)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));

        var result = await sut.SetSiteVisitNotNeededAsync(applicationId, userId, reason, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenDifferentAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        FormLevelCaseNote reason)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>{new AssigneeHistory{Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = Guid.NewGuid()}});

        var result = await sut.SetSiteVisitNotNeededAsync(applicationId, userId, reason, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenNoWoodlandOfficerReviewEntityExists(
        Guid applicationId,
        Guid userId,
        FormLevelCaseNote reason)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });

        FellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        var result = await sut.SetSiteVisitNotNeededAsync(applicationId, userId, reason, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddWoodlandOfficerReviewAsync(It.Is<WoodlandOfficerReview>(x => x.FellingLicenceApplicationId == applicationId && x.LastUpdatedById == userId && x.LastUpdatedDate == Now.ToDateTimeUtc() && x.SiteVisitNeeded == false), It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c => c.CreatedByUserId == userId && c.FellingLicenceApplicationId == applicationId && c.Type == CaseNoteType.SiteVisitComment && c.Text == reason.CaseNote && c.VisibleToApplicant == reason.VisibleToApplicant && c.VisibleToConsultee == reason.VisibleToConsultee && c.CreatedTimestamp == Now.ToDateTimeUtc()), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenWoodlandOfficerReviewEntityExistsAndFlagIsNotSet(
        Guid applicationId,
        Guid userId,
        FormLevelCaseNote reason)
    {
        var sut = CreateSut();

        var entity = new WoodlandOfficerReview
        {
            FellingLicenceApplicationId = applicationId,
            SiteVisitNeeded = null,
            LastUpdatedById = Guid.NewGuid(),
            LastUpdatedDate = Now.ToDateTimeUtc().AddDays(-1)
        };

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(entity));

        var result = await sut.SetSiteVisitNotNeededAsync(applicationId, userId, reason, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c => c.CreatedByUserId == userId && c.FellingLicenceApplicationId == applicationId && c.Type == CaseNoteType.SiteVisitComment && c.Text == reason.CaseNote && c.VisibleToApplicant == reason.VisibleToApplicant && c.VisibleToConsultee == reason.VisibleToConsultee && c.CreatedTimestamp == Now.ToDateTimeUtc()), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        Assert.False(entity.SiteVisitNeeded);
        Assert.Equal(userId, entity.LastUpdatedById);
        Assert.Equal(Now.ToDateTimeUtc(), entity.LastUpdatedDate);
    }

    [Theory, AutoData]
    public async Task WhenWoodlandOfficerReviewEntityExistsAndFlagIsSetButReasonIsNew(
        Guid applicationId,
        Guid userId,
        FormLevelCaseNote newReason,
        FormLevelCaseNote oldReason)
    {
        var sut = CreateSut();

        var existingWoReview = new WoodlandOfficerReview
        {
            FellingLicenceApplicationId = applicationId,
            SiteVisitNeeded = false,
            LastUpdatedById = Guid.NewGuid(),
            LastUpdatedDate = Now.ToDateTimeUtc().AddDays(-1)
        };
        var existingCaseNote = new CaseNote
        {
            CreatedByUserId = Guid.NewGuid(),
            CreatedTimestamp = Now.ToDateTimeUtc().AddDays(-1),
            FellingLicenceApplicationId = applicationId,
            Type = CaseNoteType.SiteVisitComment,
            Text = oldReason.CaseNote
        };

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(existingWoReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNote> { existingCaseNote });

        var result = await sut.SetSiteVisitNotNeededAsync(applicationId, userId, newReason, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetCaseNotesAsync(applicationId, new [] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c => c.CreatedByUserId == userId && c.FellingLicenceApplicationId == applicationId && c.Type == CaseNoteType.SiteVisitComment && c.Text == newReason.CaseNote && c.VisibleToApplicant == newReason.VisibleToApplicant && c.VisibleToConsultee == newReason.VisibleToConsultee && c.CreatedTimestamp == Now.ToDateTimeUtc()), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
        Assert.False(existingWoReview.SiteVisitNeeded);
        Assert.Equal(userId, existingWoReview.LastUpdatedById);
        Assert.Equal(Now.ToDateTimeUtc(), existingWoReview.LastUpdatedDate);
    }
    [Theory, AutoData]
    public async Task WhenWoodlandOfficerReviewEntityExistsAndFlagIsSetAndReasonMatches(
        Guid applicationId,
        Guid userId,
        Guid existingUserId,
        FormLevelCaseNote reason)
    {
        var sut = CreateSut();

        var existingWoReview = new WoodlandOfficerReview
        {
            FellingLicenceApplicationId = applicationId,
            SiteVisitNeeded = false,
            LastUpdatedById = existingUserId,
            LastUpdatedDate = Now.ToDateTimeUtc().AddDays(-1)
        };
        var existingCaseNote = new CaseNote
        {
            CreatedByUserId = Guid.NewGuid(),
            CreatedTimestamp = Now.ToDateTimeUtc().AddDays(-1),
            FellingLicenceApplicationId = applicationId,
            Type = CaseNoteType.SiteVisitComment,
            Text = reason.CaseNote
        };

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(existingWoReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNote> { existingCaseNote });

        var result = await sut.SetSiteVisitNotNeededAsync(applicationId, userId, reason, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }
}