using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
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

        Assert.True(result.IsSuccess);

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApp.HasValue);
        Assert.Equal(requestedTime, updatedApp.Value.DateReceived);
    }

    [Theory]
    [AutoMoqData]
    public async Task UpdatesCitizenCharterDate(FellingLicenceApplication application, DateTime requestedTime)
    {
        application.DateReceived = null;

        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateDateReceivedAsync(application.Id, requestedTime, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApp.HasValue);

        var expectedCitizenCharterDate = updatedApp.Value.DateReceived!.Value.Add(Options.CitizensCharterDateLength);

        Assert.Equal(expectedCitizenCharterDate, updatedApp.Value.CitizensCharterDate);
    }

    [Theory]
    [AutoMoqData]
    public async Task ReturnsFailure_WhenApplicationNotFound(FellingLicenceApplication application, DateTime requestedTime)
    {
        var result = await _sut.UpdateDateReceivedAsync(application.Id, requestedTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.False(updatedApp.HasValue);
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

        Assert.True(result.IsFailure);

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApp.HasValue);
        Assert.Null(updatedApp.Value.DateReceived);
    }

    [Theory, AutoMoqData]
    public async Task CanSetApplicationApprover(
        Guid? approverId)
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();

        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.SetApplicationApproverAsync(application.Id, approverId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApp.HasValue);
        Assert.Equal(approverId, updatedApp.Value.ApproverId);
    }

    [Fact]
    public async Task CanResetApplicationApprover()
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        application.ApproverId = Guid.NewGuid();

        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.SetApplicationApproverAsync(application.Id, null, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApp.HasValue);
        Assert.Null(updatedApp.Value.ApproverId);
    }

    [Theory, AutoMoqData]
    public async Task UpdateEnvironmentalImpactAssessmentAsync_ReturnsFailure_WhenRepositoryFails(
        FellingLicenceApplication application,
        EnvironmentalImpactAssessmentRecord eiaRecord)
    {
        var result = await _sut.UpdateEnvironmentalImpactAssessmentAsync(application.Id, eiaRecord, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task UpdateEnvironmentalImpactAssessmentAsync_UpdatesEnvironmentalImpactAssessmentRecord(
        EnvironmentalImpactAssessmentRecord eiaRecord)
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateEnvironmentalImpactAssessmentAsync(application.Id, eiaRecord, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApp = await _internalFlaRepository.GetAsync(application.Id, CancellationToken.None);
        Assert.True(updatedApp.HasValue);
        Assert.NotNull(updatedApp.Value.EnvironmentalImpactAssessment);
        Assert.Equal(eiaRecord.HasApplicationBeenCompleted, updatedApp.Value.EnvironmentalImpactAssessment!.HasApplicationBeenCompleted);
        Assert.Equal(eiaRecord.HasApplicationBeenSent, updatedApp.Value.EnvironmentalImpactAssessment!.HasApplicationBeenSent);
    }
    [Theory, AutoMoqData]
    public async Task UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync_ReturnsFailure_WhenRepositoryFails(
        FellingLicenceApplication application,
        EnvironmentalImpactAssessmentAdminOfficerRecord eiaRecord)
    {
        var result = await _sut.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(application.Id, eiaRecord, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync_ValidatesMutualExclusivity()
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // Both HasTheEiaFormBeenReceived and AreAttachedFormsCorrect set (invalid)
        var eiaRecord = new EnvironmentalImpactAssessmentAdminOfficerRecord
        {
            HasTheEiaFormBeenReceived = CSharpFunctionalExtensions.Maybe<bool>.From(true),
            AreAttachedFormsCorrect = CSharpFunctionalExtensions.Maybe<bool>.From(true),
            EiaTrackerReferenceNumber = CSharpFunctionalExtensions.Maybe<string>.None
        };

        var result = await _sut.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(application.Id, eiaRecord, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync_ValidatesRequiredReferenceNumber()
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // HasTheEiaFormBeenReceived is true, but EiaTrackerReferenceNumber is missing (invalid)
        var eiaRecord = new EnvironmentalImpactAssessmentAdminOfficerRecord
        {
            HasTheEiaFormBeenReceived = CSharpFunctionalExtensions.Maybe<bool>.From(true),
            AreAttachedFormsCorrect = CSharpFunctionalExtensions.Maybe<bool>.None,
            EiaTrackerReferenceNumber = CSharpFunctionalExtensions.Maybe<string>.None
        };

        var result = await _sut.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(application.Id, eiaRecord, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync_Succeeds_WithValidMutualExclusivityAndReferenceNumber()
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // Only HasTheEiaFormBeenReceived is set and EiaTrackerReferenceNumber is present (valid)
        var eiaRecord = new EnvironmentalImpactAssessmentAdminOfficerRecord
        {
            HasTheEiaFormBeenReceived = CSharpFunctionalExtensions.Maybe<bool>.From(true),
            AreAttachedFormsCorrect = CSharpFunctionalExtensions.Maybe<bool>.None,
            EiaTrackerReferenceNumber = CSharpFunctionalExtensions.Maybe<string>.From("TRACK-123")
        };

        var result = await _sut.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(application.Id, eiaRecord, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync_Succeeds_WithAttachedFormsCorrectAndReferenceNumber()
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // Only AreAttachedFormsCorrect is set and EiaTrackerReferenceNumber is present (valid)
        var eiaRecord = new EnvironmentalImpactAssessmentAdminOfficerRecord
        {
            HasTheEiaFormBeenReceived = CSharpFunctionalExtensions.Maybe<bool>.None,
            AreAttachedFormsCorrect = CSharpFunctionalExtensions.Maybe<bool>.From(true),
            EiaTrackerReferenceNumber = CSharpFunctionalExtensions.Maybe<string>.From("TRACK-456")
        };

        var result = await _sut.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(application.Id, eiaRecord, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync_Succeeds_WhenNeitherBoolIsTrue_AndReferenceNumberIsNotRequired()
    {
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        _fellingLicenceApplicationsContext.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // Neither HasTheEiaFormBeenReceived nor AreAttachedFormsCorrect is true, reference number not required
        var eiaRecord = new EnvironmentalImpactAssessmentAdminOfficerRecord
        {
            HasTheEiaFormBeenReceived = CSharpFunctionalExtensions.Maybe<bool>.From(false),
            AreAttachedFormsCorrect = CSharpFunctionalExtensions.Maybe<bool>.None,
            EiaTrackerReferenceNumber = CSharpFunctionalExtensions.Maybe<string>.None
        };

        var result = await _sut.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(application.Id, eiaRecord, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

}