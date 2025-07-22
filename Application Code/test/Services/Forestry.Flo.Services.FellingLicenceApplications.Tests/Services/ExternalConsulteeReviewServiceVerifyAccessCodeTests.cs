using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ExternalConsulteeReviewServiceVerifyAccessCodeTests
{
    private readonly Mock<IClock> MockClock = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> MockRepository = new();

    [Theory, AutoData]
    public async Task WhenExternalAccessLinkIsNotFound(
        Guid applicationId,
        Guid accessCode,
        string emailAddress)
    {
        var timeStamp = new Instant();

        var sut = CreateSut();

        MockClock.Setup(x => x.GetCurrentInstant()).Returns(timeStamp);
        MockRepository
            .Setup(x => x.GetValidExternalAccessLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ExternalAccessLink>.None);

        var result = await sut.VerifyAccessCodeAsync(applicationId, accessCode, emailAddress, CancellationToken.None);

        Assert.True(result.HasNoValue);

        MockRepository.Verify(x => x.GetValidExternalAccessLinkAsync(applicationId, accessCode, emailAddress, timeStamp.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        MockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenExternalAccessLinkIsFound(
        ExternalAccessLink externalAccessLink)
    {
        var timeStamp = new Instant();

        var sut = CreateSut();

        MockClock.Setup(x => x.GetCurrentInstant()).Returns(timeStamp);
        MockRepository
            .Setup(x => x.GetValidExternalAccessLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ExternalAccessLink>.From(externalAccessLink));

        var result = await sut.VerifyAccessCodeAsync(externalAccessLink.FellingLicenceApplicationId, externalAccessLink.AccessCode, externalAccessLink.ContactEmail, CancellationToken.None);

        Assert.True(result.HasValue);

        Assert.Equal(externalAccessLink.Name, result.Value.ContactName);
        Assert.Equal(externalAccessLink.ContactEmail, result.Value.ContactEmail);
        Assert.Equal(externalAccessLink.Purpose, result.Value.Purpose);
        Assert.Equal(externalAccessLink.CreatedTimeStamp, result.Value.CreatedTimeStamp);
        Assert.Equal(externalAccessLink.ExpiresTimeStamp, result.Value.ExpiresTimeStamp);
        Assert.Equal(externalAccessLink.FellingLicenceApplicationId, result.Value.ApplicationId);

        MockRepository.Verify(x => x.GetValidExternalAccessLinkAsync(externalAccessLink.FellingLicenceApplicationId, externalAccessLink.AccessCode, externalAccessLink.ContactEmail, timeStamp.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        MockRepository.VerifyNoOtherCalls();
    }

    private ExternalConsulteeReviewService CreateSut()
    {
        MockClock.Reset();
        MockRepository.Reset();

        return new ExternalConsulteeReviewService(
            MockRepository.Object,
            MockClock.Object,
            new NullLogger<ExternalConsulteeReviewService>());
    }
}