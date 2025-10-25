using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{
    [Theory, AutoMoqData]
    public async Task GetDetailsForConditionsNotification_WhenRepositoryThrows(
        Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.GetDetailsForConditionsNotificationAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetDetailsForConditionsNotification_WhenApplicationNotFound(
        Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetDetailsForConditionsNotificationAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetDetailsForConditionsNotification_WhenApplicationFound(
        Guid applicationId,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.GetDetailsForConditionsNotificationAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(application.CreatedById, result.Value.ApplicationAuthorId);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

}