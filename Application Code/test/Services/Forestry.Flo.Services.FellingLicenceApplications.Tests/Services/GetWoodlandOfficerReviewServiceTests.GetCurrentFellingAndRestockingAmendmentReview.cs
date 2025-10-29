using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{
    [Theory, AutoData]
    public async Task GetCurrentFellingAndRestockingAmendmentReviewWhenRepositoryThrows(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception());

        var result = await sut.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GetCurrentFellingAndRestockingAmendmentReviewWhenRepositoryReturnsFailure(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Failure<Maybe<FellingAndRestockingAmendmentReview>>("error"));

        var result = await sut.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GetCurrentFellingAndRestockingAmendmentReviewWhenNoReviewFound(Guid applicationId)
    {
        var sut = CreateSut();
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success(Maybe<FellingAndRestockingAmendmentReview>.None));

        var result = await sut.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetCurrentFellingAndRestockingAmendmentReviewWhenReviewFound(
        Guid applicationId, 
        FellingAndRestockingAmendmentReview currentReview)
    {
        var sut = CreateSut();
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success(Maybe<FellingAndRestockingAmendmentReview>.From(currentReview)));

        var result = await sut.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        Assert.Equal(currentReview.Id, result.Value.Value.Id);
        Assert.Equal(applicationId, result.Value.Value.FellingLicenceApplicationId);
        Assert.Equal(currentReview.AmendmentsSentDate, result.Value.Value.AmendmentsSentDate);
        Assert.Equal(currentReview.ResponseReceivedDate, result.Value.Value.ResponseReceivedDate);
        Assert.Equal(currentReview.ApplicantAgreed, result.Value.Value.ApplicantAgreed);
        Assert.Equal(currentReview.ApplicantDisagreementReason, result.Value.Value.ApplicantDisagreementReason);
        Assert.Equal(currentReview.ResponseDeadline, result.Value.Value.ResponseDeadline);
        Assert.Equal(currentReview.AmendmentsReason, result.Value.Value.AmendmentsReason);
        Assert.Equal(currentReview.AmendmentReviewCompleted, result.Value.Value.AmendmentReviewCompleted);

        _fellingLicenceApplicationRepository.Verify(x => x.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId, It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }
}