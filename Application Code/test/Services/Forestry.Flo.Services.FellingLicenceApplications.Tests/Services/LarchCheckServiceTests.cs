using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class LarchCheckServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();
    private readonly Mock<IClock> _clockMock = new();
    private readonly DateTime _now = DateTime.UtcNow;

    [Theory, AutoData]
    public async Task GetLarchCheckWithNoExistingDetails(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.None);

        var result = await sut.GetLarchCheckDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetLarchCheckWithExistingDetails(Guid applicationId, LarchCheckDetails details)
    {
        var sut = CreateSut();
     
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.From(details));
        
        var result = await sut.GetLarchCheckDetailsAsync(applicationId, CancellationToken.None);
        
        Assert.True(result.HasValue);

        Assert.Equal(details.Id, result.Value.Id);
        Assert.Equal(details.FellingLicenceApplicationId, result.Value.FellingLicenceApplicationId);
        Assert.Equal(details.ConfirmLarchOnly, result.Value.ConfirmLarchOnly);
        Assert.Equal(details.Zone1, result.Value.Zone1);
        Assert.Equal(details.Zone2, result.Value.Zone2);
        Assert.Equal(details.Zone3, result.Value.Zone3);
        Assert.Equal(details.ConfirmMoratorium, result.Value.ConfirmMoratorium);
        Assert.Equal(details.ConfirmInspectionLog, result.Value.ConfirmInspectionLog);
        Assert.Equal(details.RecommendSplitApplicationDue, result.Value.RecommendSplitApplicationDue);
        Assert.Equal(details.FlightDate, result.Value.FlightDate);
        Assert.Equal(details.FlightObservations, result.Value.FlightObservations);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddLarchCheckDetailsWhenSaveFails(
       LarchCheckDetailsModel model,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SaveLarchCheckDetailsAsync(model.FellingLicenceApplicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(model.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(It.Is<LarchCheckDetails>(d =>
            d.FellingLicenceApplicationId == model.FellingLicenceApplicationId
            && d.ConfirmLarchOnly == model.ConfirmLarchOnly
            && d.Zone1 == model.Zone1
            && d.Zone2 == model.Zone2
            && d.Zone3 == model.Zone3
            && d.ConfirmMoratorium == model.ConfirmMoratorium
            && d.ConfirmInspectionLog == model.ConfirmInspectionLog
            && d.RecommendSplitApplicationDue == model.RecommendSplitApplicationDue
            && d.LastUpdatedById == userId
            && d.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddLarchCheckDetailsWhenSaveThrows(
        LarchCheckDetailsModel model,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.SaveLarchCheckDetailsAsync(model.FellingLicenceApplicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(model.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(It.Is<LarchCheckDetails>(d =>
            d.FellingLicenceApplicationId == model.FellingLicenceApplicationId
            && d.ConfirmLarchOnly == model.ConfirmLarchOnly
            && d.Zone1 == model.Zone1
            && d.Zone2 == model.Zone2
            && d.Zone3 == model.Zone3
            && d.ConfirmMoratorium == model.ConfirmMoratorium
            && d.ConfirmInspectionLog == model.ConfirmInspectionLog
            && d.RecommendSplitApplicationDue == model.RecommendSplitApplicationDue
            && d.LastUpdatedById == userId
            && d.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddLarchCheckDetailsWhenSaveSuccess(
        LarchCheckDetailsModel model,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SaveLarchCheckDetailsAsync(model.FellingLicenceApplicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(model.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(It.Is<LarchCheckDetails>(d =>
            d.FellingLicenceApplicationId == model.FellingLicenceApplicationId
            && d.ConfirmLarchOnly == model.ConfirmLarchOnly
            && d.Zone1 == model.Zone1
            && d.Zone2 == model.Zone2
            && d.Zone3 == model.Zone3
            && d.ConfirmMoratorium == model.ConfirmMoratorium
            && d.ConfirmInspectionLog == model.ConfirmInspectionLog
            && d.RecommendSplitApplicationDue == model.RecommendSplitApplicationDue
            && d.LastUpdatedById == userId
            && d.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateLarchCheckDetailsWhenSaveFails(
        LarchCheckDetailsModel model,
        LarchCheckDetails existingEntity,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SaveLarchCheckDetailsAsync(model.FellingLicenceApplicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(model.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(model.FellingLicenceApplicationId, existingEntity.FellingLicenceApplicationId);
        Assert.Equal(model.ConfirmLarchOnly, existingEntity.ConfirmLarchOnly);
        Assert.Equal(model.Zone1, existingEntity.Zone1);
        Assert.Equal(model.Zone2, existingEntity.Zone2);
        Assert.Equal(model.Zone3, existingEntity.Zone3);
        Assert.Equal(model.ConfirmMoratorium, existingEntity.ConfirmMoratorium);
        Assert.Equal(model.ConfirmInspectionLog, existingEntity.ConfirmInspectionLog);
        Assert.Equal(model.RecommendSplitApplicationDue, existingEntity.RecommendSplitApplicationDue);
        Assert.Equal(userId, existingEntity.LastUpdatedById);
        Assert.Equal(_now, existingEntity.LastUpdatedDate);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateLarchCheckDetailsWhenSaveThrows(
        LarchCheckDetailsModel model, 
        LarchCheckDetails existingEntity,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.SaveLarchCheckDetailsAsync(model.FellingLicenceApplicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(model.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(model.FellingLicenceApplicationId, existingEntity.FellingLicenceApplicationId);
        Assert.Equal(model.ConfirmLarchOnly, existingEntity.ConfirmLarchOnly);
        Assert.Equal(model.Zone1, existingEntity.Zone1);
        Assert.Equal(model.Zone2, existingEntity.Zone2);
        Assert.Equal(model.Zone3, existingEntity.Zone3);
        Assert.Equal(model.ConfirmMoratorium, existingEntity.ConfirmMoratorium);
        Assert.Equal(model.ConfirmInspectionLog, existingEntity.ConfirmInspectionLog);
        Assert.Equal(model.RecommendSplitApplicationDue, existingEntity.RecommendSplitApplicationDue);
        Assert.Equal(userId, existingEntity.LastUpdatedById);
        Assert.Equal(_now, existingEntity.LastUpdatedDate);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateLarchCheckDetailsWhenSaveSuccess(
        LarchCheckDetailsModel model,
        LarchCheckDetails existingEntity,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SaveLarchCheckDetailsAsync(model.FellingLicenceApplicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(model.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(model.FellingLicenceApplicationId, existingEntity.FellingLicenceApplicationId);
        Assert.Equal(model.ConfirmLarchOnly, existingEntity.ConfirmLarchOnly);
        Assert.Equal(model.Zone1, existingEntity.Zone1);
        Assert.Equal(model.Zone2, existingEntity.Zone2);
        Assert.Equal(model.Zone3, existingEntity.Zone3);
        Assert.Equal(model.ConfirmMoratorium, existingEntity.ConfirmMoratorium);
        Assert.Equal(model.ConfirmInspectionLog, existingEntity.ConfirmInspectionLog);
        Assert.Equal(model.RecommendSplitApplicationDue, existingEntity.RecommendSplitApplicationDue);
        Assert.Equal(userId, existingEntity.LastUpdatedById);
        Assert.Equal(_now, existingEntity.LastUpdatedDate);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddLarchFlyoverDetailsWhenSaveFails(
        Guid applicationId,
        DateTime flightDate,
        string flightObservations,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SaveLarchFlyoverAsync(applicationId, flightDate, flightObservations, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(It.Is<LarchCheckDetails>(d =>
            d.FellingLicenceApplicationId == applicationId
            && d.FlightDate == flightDate
            && d.FlightObservations == flightObservations
            && d.LastUpdatedById == userId
            && d.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddLarchFlyoverDetailsWhenSaveThrows(
        Guid applicationId,
        DateTime flightDate,
        string flightObservations,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.SaveLarchFlyoverAsync(applicationId, flightDate, flightObservations, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(It.Is<LarchCheckDetails>(d =>
            d.FellingLicenceApplicationId == applicationId
            && d.FlightDate == flightDate
            && d.FlightObservations == flightObservations
            && d.LastUpdatedById == userId
            && d.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddLarchFlyoverDetailsWhenSaveSuccess(
        Guid applicationId,
        DateTime flightDate,
        string flightObservations,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SaveLarchFlyoverAsync(applicationId, flightDate, flightObservations, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(It.Is<LarchCheckDetails>(d =>
            d.FellingLicenceApplicationId == applicationId
            && d.FlightDate == flightDate
            && d.FlightObservations == flightObservations
            && d.LastUpdatedById == userId
            && d.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateLarchFlyoverDetailsWhenSaveFails(
        Guid applicationId,
        DateTime flightDate,
        string flightObservations,
        LarchCheckDetails existingEntity,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SaveLarchFlyoverAsync(applicationId, flightDate, flightObservations, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(flightDate, existingEntity.FlightDate);
        Assert.Equal(flightObservations, existingEntity.FlightObservations);
        Assert.Equal(userId, existingEntity.LastUpdatedById);
        Assert.Equal(_now, existingEntity.LastUpdatedDate);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateLarchFlyoverDetailsWhenSaveThrows(
        Guid applicationId,
        DateTime flightDate,
        string flightObservations,
        LarchCheckDetails existingEntity,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.SaveLarchFlyoverAsync(applicationId, flightDate, flightObservations, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(flightDate, existingEntity.FlightDate);
        Assert.Equal(flightObservations, existingEntity.FlightObservations);
        Assert.Equal(userId, existingEntity.LastUpdatedById);
        Assert.Equal(_now, existingEntity.LastUpdatedDate);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateLarchFlyoverDetailsWhenSaveSuccess(
        Guid applicationId,
        DateTime flightDate,
        string flightObservations,
        LarchCheckDetails existingEntity,
        Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetLarchCheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<LarchCheckDetails>.From(existingEntity));

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddOrUpdateLarchCheckDetailsAsync(It.IsAny<LarchCheckDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SaveLarchFlyoverAsync(applicationId, flightDate, flightObservations, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fellingLicenceApplicationRepository.Verify(x => x.GetLarchCheckDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.AddOrUpdateLarchCheckDetailsAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(flightDate, existingEntity.FlightDate);
        Assert.Equal(flightObservations, existingEntity.FlightObservations);
        Assert.Equal(userId, existingEntity.LastUpdatedById);
        Assert.Equal(_now, existingEntity.LastUpdatedDate);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    private LarchCheckService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();

        _clockMock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_now));

        return new LarchCheckService(
            _fellingLicenceApplicationRepository.Object,
            _clockMock.Object,
            new NullLogger<LarchCheckService>());
    }
}