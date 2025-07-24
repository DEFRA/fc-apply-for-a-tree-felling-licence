using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class WithdrawalNotificationServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _internalFLARepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClock> _clockMock;

    private TimeSpan _threshold;

    public WithdrawalNotificationServiceTests()
    {
        _internalFLARepositoryMock = new Mock<IFellingLicenceApplicationInternalRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
    }

    private WithdrawalNotificationService CreateSut()
    {
        _internalFLARepositoryMock.Reset();
        _unitOfWorkMock.Reset();
        _clockMock.Reset();

        _threshold = new TimeSpan(14, 0, 0, 0);

        return new WithdrawalNotificationService(
            _clockMock.Object,
            _internalFLARepositoryMock.Object,
            new NullLogger<WithdrawalNotificationService>());
    } 

    [Theory, AutoMoqData]
    public async Task ApplicationsShouldUpdate_WhenThresholdReached(List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var currentDate = DateTime.UtcNow.Date;

        var status = FellingLicenceStatus.WithApplicant;
        foreach (var application in applications)
        {
            application.VoluntaryWithdrawalNotificationTimeStamp = null;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = currentDate.AddDays(-15),
                    FellingLicenceApplication = application,
                    Status = status
                }
            };

            status = status == FellingLicenceStatus.WithApplicant
                ? FellingLicenceStatus.ReturnedToApplicant
                : FellingLicenceStatus.WithApplicant;
        }

        _internalFLARepositoryMock.Setup(s =>
                s.GetApplicationsThatAreWithinThresholdAutomaticWithdrawalDateAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications);

        _internalFLARepositoryMock.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(UnitResult.Success<UserDbErrorReason>()));

        _clockMock.Setup(s => s.GetCurrentInstant())
            .Returns(Instant.FromDateTimeUtc(currentDate));

        var result = await sut.GetApplicationsAfterThresholdForWithdrawalAsync(_threshold, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(applications.Count);

        foreach (var application in result.Value)
        {
            application.NotificationDateSent.Date.Should().Be(currentDate.Date);
        }

        _internalFLARepositoryMock.Verify(v => v.GetApplicationsThatAreWithinThresholdAutomaticWithdrawalDateAsync(
            It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _clockMock.Verify(v => v.GetCurrentInstant(), Times.Once);
    }
}