using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using LinqKit;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{
    [Theory, AutoMoqData]
    public async Task GetSiteVisitShouldReturnFailureIfRepositoryThrows(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetSiteVisitShouldReturnMaybeNoneIfNoEntryOrCommentsExist(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _viewCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetSiteVisitShouldReturnDefaultEntryAndCommentsIfJustCommentsExist(
        Guid applicationId,
        IList<CaseNoteModel> comments)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _viewCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var model = result.Value.Value;

        Assert.Null(model.SiteVisitNeeded);
        Assert.Null(model.SiteVisitArrangementsMade);
        Assert.False(model.SiteVisitComplete);
        Assert.Equal(comments, model.SiteVisitComments);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetSiteVisitShouldReturnExpectedValuesIfDataExistsWithoutAnyAttachments(
        Guid applicationId,
        WoodlandOfficerReview reviewEntity,
        IList<CaseNoteModel> comments)
    {
        var sut = CreateSut();

        reviewEntity.SiteVisitEvidences = new List<SiteVisitEvidence>();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(reviewEntity));

        _viewCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var model = result.Value.Value;

        Assert.Equal(reviewEntity.SiteVisitNeeded, model.SiteVisitNeeded);
        Assert.Equal(reviewEntity.SiteVisitArrangementsMade, model.SiteVisitArrangementsMade);
        Assert.Equal(reviewEntity.SiteVisitComplete, model.SiteVisitComplete);
        Assert.Equal(comments, model.SiteVisitComments);
        Assert.Empty(model.SiteVisitAttachments);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetSiteVisitShouldReturnExpectedValuesIfDataExistsWithAttachments(
        Guid applicationId,
        WoodlandOfficerReview reviewEntity,
        IList<CaseNoteModel> comments,
        Document document)
    {
        var sut = CreateSut();

        reviewEntity.SiteVisitEvidences.ForEach(x => x.DocumentId = document.Id);

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(reviewEntity));

        _viewCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApplicationDocumentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([document]);

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var model = result.Value.Value;

        Assert.Equal(reviewEntity.SiteVisitNeeded, model.SiteVisitNeeded);
        Assert.Equal(reviewEntity.SiteVisitArrangementsMade, model.SiteVisitArrangementsMade);
        Assert.Equal(reviewEntity.SiteVisitComplete, model.SiteVisitComplete);
        Assert.Equal(comments, model.SiteVisitComments);
        Assert.Equal(reviewEntity.SiteVisitEvidences.Count, model.SiteVisitAttachments.Count);
        foreach (var evidence in reviewEntity.SiteVisitEvidences)
        {
            Assert.Contains(model.SiteVisitAttachments, x =>
                x.DocumentId == document.Id
                && x.FileName == document.FileName
                && x.Label == evidence.Label
                && x.Comment == evidence.Comment
                && x.VisibleToApplicant == document.VisibleToApplicant
                && x.VisibleToConsultee == document.VisibleToConsultee);
        }

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
    }

}