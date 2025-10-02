using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ExtendApplicationsServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _internalFLARepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClock> _clockMock;

    private TimeSpan _extensionLength;
    private TimeSpan _threshold;

    public ExtendApplicationsServiceTests()
    {
        _internalFLARepositoryMock = new Mock<IFellingLicenceApplicationInternalRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clockMock = new Mock<IClock>();
    }

    private ExtendApplicationsService CreateSut()
    {
        _internalFLARepositoryMock.Reset();
        _unitOfWorkMock.Reset();
        _clockMock.Reset();

        _extensionLength = new TimeSpan(90, 0, 0, 0);
        _threshold = new TimeSpan(10, 0, 0, 0);

        return new ExtendApplicationsService(
            _clockMock.Object,
            _internalFLARepositoryMock.Object,
            new NullLogger<ExtendApplicationsService>());
    } 

    [Theory, AutoMoqData]
    public async Task ApplicationsShouldBeExtended_WhenFinalActionDateReached(List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var currentTime = DateTime.UtcNow.Date;
        var previousFinalActionDate = currentTime.AddDays(-1);

        foreach (var application in applications)
        {
            application.FinalActionDate = previousFinalActionDate;
            application.FinalActionDateExtended = false;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow,
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                }
            };
        }

        _internalFLARepositoryMock.Setup(s =>
                s.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications);

        _internalFLARepositoryMock.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(UnitResult.Success<UserDbErrorReason>()));

        _clockMock.Setup(s => s.GetCurrentInstant())
            .Returns(Instant.FromDateTimeUtc(currentTime));

        var result = await sut.ApplyApplicationExtensionsAsync(_extensionLength, _threshold, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(applications.Count, result.Value.Count);

        // assert final action date has been extended, and the flag changed

        foreach (var application in result.Value)
        {
            Assert.Equal(previousFinalActionDate.Add(_extensionLength), application.FinalActionDate);
            Assert.Equal(_extensionLength, application.ExtensionLength);
        }

        _internalFLARepositoryMock.Verify(v => v.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(currentTime, _threshold, CancellationToken.None), Times.Once);
        _internalFLARepositoryMock.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Once);
        _clockMock.Verify(v => v.GetCurrentInstant(), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationsShouldNotBeExtended_WhenFinalActionDateThresholdMet(List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var currentTime = DateTime.UtcNow.Date;
        var previousFinalActionDate = currentTime.AddDays(5);

        foreach (var application in applications)
        {
            application.FinalActionDate = previousFinalActionDate;
            application.FinalActionDateExtended = false;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow,
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                }
            };
        }

        _internalFLARepositoryMock.Setup(s =>
                s.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications);

        _internalFLARepositoryMock.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(UnitResult.Success<UserDbErrorReason>()));

        _clockMock.Setup(s => s.GetCurrentInstant())
            .Returns(Instant.FromDateTimeUtc(currentTime));

        var result = await sut.ApplyApplicationExtensionsAsync(_extensionLength, _threshold, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(applications.Count, result.Value.Count);

        // assert final action date has been extended, and the flag changed

        foreach (var application in result.Value)
        {
            Assert.NotEqual(previousFinalActionDate, application.FinalActionDate);
            Assert.NotNull(application.ExtensionLength);
        }

        _internalFLARepositoryMock.Verify(v => v.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(currentTime, _threshold, CancellationToken.None), Times.Once);
        _internalFLARepositoryMock.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Once);
        _clockMock.Verify(v => v.GetCurrentInstant(), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationsShouldNotBeExtended_WhenFinalActionDateNotReached(List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var currentTime = DateTime.UtcNow.Date;

        foreach (var application in applications)
        {
            application.FinalActionDate = currentTime.AddDays(10);
            application.FinalActionDateExtended = false;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow,
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                }
            };
        }

        _internalFLARepositoryMock.Setup(s =>
                s.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FellingLicenceApplication>());

        _internalFLARepositoryMock.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(UnitResult.Success<UserDbErrorReason>()));

        _clockMock.Setup(s => s.GetCurrentInstant())
            .Returns(Instant.FromDateTimeUtc(currentTime));

        var result = await sut.ApplyApplicationExtensionsAsync(_extensionLength, _threshold, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _internalFLARepositoryMock.Verify(v => v.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(currentTime, _threshold, CancellationToken.None), Times.Once);
        _internalFLARepositoryMock.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Once);
        _clockMock.Verify(v => v.GetCurrentInstant(), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationsShouldNotBeExtended_WhenAlreadyExtended(List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var currentTime = DateTime.UtcNow.Date;

        foreach (var application in applications)
        {
            application.FinalActionDate = currentTime.AddDays(-1);
            application.FinalActionDateExtended = true;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow,
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                }
            };
        }

        _internalFLARepositoryMock.Setup(s =>
                s.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications);

        _internalFLARepositoryMock.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(UnitResult.Success<UserDbErrorReason>()));

        _clockMock.Setup(s => s.GetCurrentInstant())
            .Returns(Instant.FromDateTimeUtc(currentTime));

        var result = await sut.ApplyApplicationExtensionsAsync(_extensionLength, _threshold, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(applications.Count, result.Value.Count);

        // assert final action date has not been extended, and the flag remains true

        foreach (var application in result.Value)
        {
            Assert.NotNull(application.ExtensionLength);
        }

        _internalFLARepositoryMock.Verify(v => v.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(currentTime, _threshold, CancellationToken.None), Times.Once);
        _internalFLARepositoryMock.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Once);
        _clockMock.Verify(v => v.GetCurrentInstant(), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SomeApplicationsShouldBeExtended_WithMixtureOfDatesAndFlags(List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var currentTime = DateTime.UtcNow.Date;

        foreach (var application in applications)
        {
            application.StatusHistories = new List<StatusHistory>()
            {
                new()
                {
                    Created = currentTime,
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                }
            };
        }

        applications[0].FinalActionDate = currentTime.AddDays(-1);
        applications[0].FinalActionDateExtended = true;

        applications[1].FinalActionDate = currentTime.AddDays(-40);
        applications[1].FinalActionDateExtended = false;

        applications[2].FinalActionDate = currentTime.AddDays(-1);
        applications[2].FinalActionDateExtended = false;

        _internalFLARepositoryMock.Setup(s =>
                s.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications);

        _internalFLARepositoryMock.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(UnitResult.Success<UserDbErrorReason>()));

        _clockMock.Setup(s => s.GetCurrentInstant())
            .Returns(Instant.FromDateTimeUtc(currentTime));

        var result = await sut.ApplyApplicationExtensionsAsync(_extensionLength, _threshold, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);

        // assert final action date has been extended, and the flag changed

        Assert.Equal(3, applications.Count(x => x.FinalActionDateExtended == true));
        Assert.Equal(3, applications.Count(x => x.FinalActionDate > currentTime));

        _internalFLARepositoryMock.Verify(v => v.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(currentTime, _threshold, CancellationToken.None), Times.Once);
        _internalFLARepositoryMock.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Once);
        _clockMock.Verify(v => v.GetCurrentInstant(), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenChangesNotSaved(List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var currentTime = DateTime.UtcNow.Date;

        _internalFLARepositoryMock.Setup(s =>
                s.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications);

        _internalFLARepositoryMock.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(UnitResult.Failure(UserDbErrorReason.NotFound)));

        _clockMock.Setup(s => s.GetCurrentInstant())
            .Returns(Instant.FromDateTimeUtc(currentTime));

        var result = await sut.ApplyApplicationExtensionsAsync(_extensionLength, _threshold, CancellationToken.None);

        Assert.True(result.IsFailure);

        _internalFLARepositoryMock.Verify(v => v.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(currentTime, _threshold, CancellationToken.None), Times.Once);
        _internalFLARepositoryMock.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Once);
        _clockMock.Verify(v => v.GetCurrentInstant(), Times.Once);
    }
}