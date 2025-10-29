using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetFellingLicenceApplicationForInternalUsersServiceTests
{
    [Theory, AutoMoqData]
    public async Task RetrieveApplicationNotificationDetailsWhenUnableToCheckAccessToApplication(
        Guid applicationId,
        UserAccessModel uam)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));

        var result = await sut.RetrieveApplicationNotificationDetailsAsync(applicationId, uam, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RetrieveApplicationNotificationDetailsWhenCannotAccessApplication(
        Guid applicationId,
        UserAccessModel uam)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var result = await sut.RetrieveApplicationNotificationDetailsAsync(applicationId, uam, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RetrieveApplicationNotificationDetailsWhenCannotLocateApplication(
        Guid applicationId,
        UserAccessModel uam)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.RetrieveApplicationNotificationDetailsAsync(applicationId, uam, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RetrieveApplicationNotificationDetailsWhenSuccess(
        Guid applicationId,
        UserAccessModel uam,
        FellingLicenceApplication entity)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(entity));

        var result = await sut.RetrieveApplicationNotificationDetailsAsync(applicationId, uam, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(entity.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(entity.AdministrativeRegion, result.Value.AdminHubName);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }
}