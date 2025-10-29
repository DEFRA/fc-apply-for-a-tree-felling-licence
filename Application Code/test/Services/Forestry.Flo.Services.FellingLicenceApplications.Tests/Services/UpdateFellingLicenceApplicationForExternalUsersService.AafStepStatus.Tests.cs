using AutoFixture;
using AutoFixture.AutoMoq;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationForExternalUsersService_AafStepStatus_Tests
{
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _mockRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IClock> _mockClock = new();
    private readonly Instant _now = Instant.FromDateTimeUtc(DateTime.UtcNow);
    private readonly FellingLicenceApplicationOptions _options = new() { CitizensCharterDateLength = TimeSpan.FromDays(7), FinalActionDateDaysFromSubmission = 30 };
    private readonly IFixture _fixture;

    public UpdateFellingLicenceApplicationForExternalUsersService_AafStepStatus_Tests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    private UpdateFellingLicenceApplicationForExternalUsersService CreateSut()
    {
        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);
        _mockRepository.Reset();
        _mockUnitOfWork.Reset();
        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitOfWork.Object);
        return new UpdateFellingLicenceApplicationForExternalUsersService(
            _mockRepository.Object,
            _mockClock.Object,
            new OptionsWrapper<FellingLicenceApplicationOptions>(_options),
            new NullLogger<UpdateFellingLicenceApplicationForExternalUsersService>());
    }

    [Fact]
    public async Task Returns_Failure_When_Application_Not_Found()
    {
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var userAccess = _fixture.Create<UserAccessModel>();
        _mockRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(Maybe<FellingLicenceApplication>.None);
        var result = await sut.UpdateAafStepAsync(applicationId, userAccess, true, CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Returns_Failure_When_User_Cannot_Manage_WoodlandOwner()
    {
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var application = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.Id, applicationId)
            .With(x => x.WoodlandOwnerId, Guid.NewGuid())
            .With(x => x.FellingLicenceApplicationStepStatus, new FellingLicenceApplicationStepStatus())
            .Create();
        var userAccess = _fixture.Build<UserAccessModel>()
            .With(x => x.IsFcUser, false)
            .With(x => x.WoodlandOwnerIds, new List<Guid> { Guid.NewGuid() })
            .Create();
        _mockRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(Maybe.From(application));
        var result = await sut.UpdateAafStepAsync(applicationId, userAccess, true, CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Returns_Failure_When_Save_Fails()
    {
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var woodlandOwnerId = Guid.NewGuid();
        var application = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.Id, applicationId)
            .With(x => x.WoodlandOwnerId, woodlandOwnerId)
            .With(x => x.FellingLicenceApplicationStepStatus, new FellingLicenceApplicationStepStatus())
            .Create();
        var userAccess = _fixture.Build<UserAccessModel>()
            .With(x => x.IsFcUser, true)
            .Create();
        _mockRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(Maybe.From(application));
        _mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));
        var result = await sut.UpdateAafStepAsync(applicationId, userAccess, false, CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Sets_AafStepStatus_And_Saves(bool aafStepStatus)
    {
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var woodlandOwnerId = Guid.NewGuid();
        var stepStatus = new FellingLicenceApplicationStepStatus();
        var application = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.Id, applicationId)
            .With(x => x.WoodlandOwnerId, woodlandOwnerId)
            .With(x => x.FellingLicenceApplicationStepStatus, stepStatus)
            .Create();
        var userAccess = _fixture.Build<UserAccessModel>()
            .With(x => x.IsFcUser, true)
            .Create();
        _mockRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(Maybe.From(application));
        _mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        var result = await sut.UpdateAafStepAsync(applicationId, userAccess, aafStepStatus, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(aafStepStatus, stepStatus.AafStepStatus);
        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
