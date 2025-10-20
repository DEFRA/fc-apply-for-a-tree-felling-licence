using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{
    [Theory, AutoMoqData]
    public async Task GetPw14ChecksShouldReturnFailureWhenRepositoryThrows(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.GetPw14ChecksAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPw14ChecksShouldReturnMaybeNoneIfNoEntryExists(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        var result = await sut.GetPw14ChecksAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPw14ChecksShouldReturnExpectedValuesIfDataExists(
        Guid applicationId,
        WoodlandOfficerReview reviewEntity)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(reviewEntity));

        var result = await sut.GetPw14ChecksAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var model = result.Value.Value;

        Assert.Equal(reviewEntity.LandInformationSearchChecked, model.LandInformationSearchChecked);
        Assert.Equal(reviewEntity.AreProposalsUkfsCompliant, model.AreProposalsUkfsCompliant);
        Assert.Equal(reviewEntity.TpoOrCaDeclared, model.TpoOrCaDeclared);
        Assert.Equal(reviewEntity.IsApplicationValid, model.IsApplicationValid);
        Assert.Equal(reviewEntity.EiaThresholdExceeded, model.EiaThresholdExceeded);
        Assert.Equal(reviewEntity.EiaTrackerCompleted, model.EiaTrackerCompleted);
        Assert.Equal(reviewEntity.EiaChecklistDone, model.EiaChecklistDone);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

}