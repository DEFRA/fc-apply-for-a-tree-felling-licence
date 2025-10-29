using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{
    [Theory, AutoData]
    public async Task GetConditionsStatusWhenRepositoryThrows(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());
        
        var result = await sut.GetConditionsStatusAsync(applicationId, CancellationToken.None);
        
        Assert.True(result.IsFailure);
        
        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GetConditionsStatusWhenRepositoryReturnsNoWoodlandOfficerReview(Guid applicationId)
    {
        var sut = CreateSut();
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Entities.WoodlandOfficerReview>.None);

        var result = await sut.GetConditionsStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Null(result.Value.ConditionsToApplicantDate);
        Assert.Null(result.Value.IsConditional);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetConditionsStatusWhenRepositoryReturnsWoodlandOfficerReview(
        Guid applicationId,
        Entities.WoodlandOfficerReview reviewEntity)
    {
        var sut = CreateSut();
     
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Entities.WoodlandOfficerReview>.From(reviewEntity));
        
        var result = await sut.GetConditionsStatusAsync(applicationId, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(reviewEntity.ConditionsToApplicantDate, result.Value.ConditionsToApplicantDate);
        Assert.Equal(reviewEntity.IsConditional, result.Value.IsConditional);
        
        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }
}