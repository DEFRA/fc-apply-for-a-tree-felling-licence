using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationServiceTests
{
    private readonly InternalUserContextFlaRepository _internalFlaRepository;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;
    private readonly Mock<IGetConfiguredFcAreas> _mockGetConfiguredFcAreas = new();
    private readonly UpdateFellingLicenceApplicationService _sut;
    private readonly Mock<IClock> _mockClock = new();
    private readonly Mock<IAmendCaseNotes> _mockCaseNotes = new();

    private static readonly Fixture FixtureInstance = new();

    private static readonly FellingLicenceApplicationOptions Options = new()
    {
        FinalActionDateDaysFromSubmission = 10,
        CitizensCharterDateLength = new TimeSpan(10, 0, 0)
    };

    public UpdateFellingLicenceApplicationServiceTests()
    {
        FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => FixtureInstance.Behaviors.Remove(b));
        FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());

        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _internalFlaRepository = new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext);

        var optionsMock = new Mock<IOptions<FellingLicenceApplicationOptions>>();
        optionsMock.Setup(s => s.Value).Returns(Options);

        _sut = new UpdateFellingLicenceApplicationService(
            _internalFlaRepository, 
            _mockCaseNotes.Object, 
            _mockGetConfiguredFcAreas.Object,
            _mockClock.Object, 
            new NullLogger<UpdateFellingLicenceApplicationService>(), 
            optionsMock.Object);
    }

    [Theory]
    [InlineData(FellingLicenceStatus.Received)]
    [InlineData(FellingLicenceStatus.Draft)]
    [InlineData(FellingLicenceStatus.Submitted)]
    [InlineData(FellingLicenceStatus.WithApplicant)]
    [InlineData(FellingLicenceStatus.ReturnedToApplicant)]
    public async Task UpdatesDateReceivedForApplicationInValidState(FellingLicenceStatus currentStatus)
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        var requestedTime = FixtureInstance.Create<DateTime>();

        application.DateReceived = null;

        application.StatusHistories = new List<StatusHistory>
            { new StatusHistory { Created = DateTime.UtcNow, Status = currentStatus } };

        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateDateReceivedAsync(application.Id, requestedTime, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        updatedApp.HasValue.Should().BeTrue();
        updatedApp.Value.DateReceived.Should().Be(requestedTime);
    }

    [Theory]
    [AutoMoqData]
    public async Task UpdatesCitizenCharterDate(FellingLicenceApplication application, DateTime requestedTime)
    {
        application.DateReceived = null;

        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateDateReceivedAsync(application.Id, requestedTime, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        updatedApp.HasValue.Should().BeTrue();

        var expectedCitizenCharterDate = updatedApp.Value.DateReceived!.Value.Add(Options.CitizensCharterDateLength);

        updatedApp.Value.CitizensCharterDate.Should().Be(expectedCitizenCharterDate);
    }

    [Theory]
    [AutoMoqData]
    public async Task ReturnsFailure_WhenApplicationNotFound(FellingLicenceApplication application, DateTime requestedTime)
    {
        var result = await _sut.UpdateDateReceivedAsync(application.Id, requestedTime, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        updatedApp.HasValue.Should().BeFalse();
    }

    [Theory]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview)]
    [InlineData(FellingLicenceStatus.Approved)]
    [InlineData(FellingLicenceStatus.Refused)]
    [InlineData(FellingLicenceStatus.SentForApproval)]
    [InlineData(FellingLicenceStatus.Withdrawn)]
    public async Task ReturnsFailure_WhenIncorrectStatus(FellingLicenceStatus status)
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        var requestedTime = FixtureInstance.Create<DateTime>();

        application.DateReceived = null;

        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.Now,
                FellingLicenceApplication = application,
                FellingLicenceApplicationId = application.Id,
                Status = status
            }
        };

        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateDateReceivedAsync(application.Id, requestedTime, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        updatedApp.HasValue.Should().BeTrue();
        updatedApp.Value.DateReceived.Should().BeNull();
    }

    [Theory, AutoMoqData]
    public async Task CanSetApplicationApprover(
        Guid? approverId)
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();

        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.SetApplicationApproverAsync(application.Id, approverId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        updatedApp.HasValue.Should().BeTrue();
        updatedApp.Value.ApproverId.Should().Be(approverId);
    }

    [Fact]
    public async Task CanResetApplicationApprover()
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        application.ApproverId = Guid.NewGuid();

        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.SetApplicationApproverAsync(application.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        updatedApp.HasValue.Should().BeTrue();
        updatedApp.Value.ApproverId.Should().BeNull();
    }
}