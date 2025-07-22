using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewServicePublishedToSiteVisitMobileLayersTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    [Theory, AutoData]
    public async Task ReturnsFailureWhenRepositoryThrows(
        Guid applicationId,
        Guid userId,
        DateTime publishedDateTime)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("done gone wrong"));

        var result = await sut.PublishedToSiteVisitMobileLayersAsync(applicationId, userId, publishedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNotInCorrectState(
        Guid applicationId,
        Guid userId,
        DateTime publishedDateTime)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>(0));

        var result = await sut.PublishedToSiteVisitMobileLayersAsync(applicationId, userId, publishedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNoAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        DateTime publishedDateTime)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>{new StatusHistory{Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview}});
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));

        var result = await sut.PublishedToSiteVisitMobileLayersAsync(applicationId, userId, publishedDateTime, CancellationToken.None);

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
        DateTime publishedDateTime)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>{new AssigneeHistory {Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = Guid.NewGuid()}});

        var result = await sut.PublishedToSiteVisitMobileLayersAsync(applicationId, userId, publishedDateTime, CancellationToken.None);

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
        DateTime publishedDateTime)
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

        var result = await sut.PublishedToSiteVisitMobileLayersAsync(applicationId, userId, publishedDateTime, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddWoodlandOfficerReviewAsync(It.Is<WoodlandOfficerReview>(x => x.FellingLicenceApplicationId == applicationId && x.LastUpdatedById == userId && x.LastUpdatedDate == publishedDateTime && x.SiteVisitArtefactsCreated == publishedDateTime && x.SiteVisitNotNeeded == false), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenWoodlandOfficerReviewEntityExists(
        Guid applicationId,
        Guid userId,
        DateTime publishedDateTime)
    {
        var sut = CreateSut();

        var entity = new WoodlandOfficerReview
        {
            FellingLicenceApplicationId = applicationId,
            SiteVisitNotNeeded = true,
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

        var result = await sut.PublishedToSiteVisitMobileLayersAsync(applicationId, userId, publishedDateTime, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        Assert.False(entity.SiteVisitNotNeeded);
        Assert.Equal(userId, entity.LastUpdatedById);
        Assert.Equal(publishedDateTime, entity.LastUpdatedDate);
        Assert.Equal(publishedDateTime, entity.SiteVisitArtefactsCreated);
    }
}