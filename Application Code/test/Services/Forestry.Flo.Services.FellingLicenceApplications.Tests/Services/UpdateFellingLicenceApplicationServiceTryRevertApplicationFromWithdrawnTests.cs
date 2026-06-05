using AutoFixture;
using AutoFixture.AutoMoq;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationServiceTryRevertApplicationFromWithdrawnTests
{
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
    private readonly Mock<IAmendCaseNotes> _mockCaseNotes = new();
    private readonly Mock<IGetConfiguredFcAreas> _mockGetConfiguredFcAreas = new();
    private readonly Mock<IClock> _mockClock = new();

    private readonly IFixture Fixture;

    public UpdateFellingLicenceApplicationServiceTryRevertApplicationFromWithdrawnTests()
    {
        Fixture = new Fixture().Customize(new CompositeCustomization(
            new AutoMoqCustomization(),
            new SupportMutableValueTypesCustomization()));

        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        Fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationNotFound(Guid performingUserId, Guid applicationId)
    {
        var sut = CreateSut();

        var result = await sut.TryRevertApplicationFromWithdrawnAsync(performingUserId, applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to retrieve felling licence application", result.Error);
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationNotInWithdrawnState(Guid performingUserId, FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.StatusHistories.Clear();

        application.StatusHistories.Add(new StatusHistory
        {
            Status = FellingLicenceStatus.Submitted,
            Created = DateTime.UtcNow
        });

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        var result = await sut.TryRevertApplicationFromWithdrawnAsync(performingUserId, application.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal($"Application is currently in state {FellingLicenceStatus.Submitted} and so cannot be reverted from withdrawn", result.Error);


    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoPreviousStatus(Guid performingUserId, FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.StatusHistories.Clear();

        application.StatusHistories.Add(new StatusHistory
        {
            Status = FellingLicenceStatus.Withdrawn,
            Created = DateTime.UtcNow
        });

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        var result = await sut.TryRevertApplicationFromWithdrawnAsync(performingUserId, application.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Application does not have a previous status to revert to", result.Error);

        var updatedApplication = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.StatusHistories)
            .FirstAsync(x => x.Id == application.Id);

        Assert.Equal(FellingLicenceStatus.Withdrawn, updatedApplication.GetCurrentStatus());
    }

    [Theory, CombinatorialData]
    public async Task WhenRevertingFromWithdrawnToPreviousStatus(FellingLicenceStatus previousStatus)
    {
        var sut = CreateSut();

        var application = Fixture.Create<FellingLicenceApplication>();
        var performingUserId = Guid.NewGuid();

        application.StatusHistories.Clear();

        application.StatusHistories.Add(new StatusHistory
        {
            Status = FellingLicenceStatus.Withdrawn,
            Created = DateTime.UtcNow
        });
        application.StatusHistories.Add(new StatusHistory
        {
            Status = previousStatus,
            Created = DateTime.UtcNow.AddDays(-1)
        });
        if (previousStatus != FellingLicenceStatus.Submitted)
        {
            application.StatusHistories.Add(new StatusHistory
            {
                Status = FellingLicenceStatus.Submitted,
                Created = DateTime.UtcNow.AddDays(-2)
            });
        }
        var submittedDate = application.StatusHistories
            .OrderByDescending(x => x.Created)
            .First(x => x.Status == FellingLicenceStatus.Submitted).Created;

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.TryRevertApplicationFromWithdrawnAsync(performingUserId, application.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApplication = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.StatusHistories)
            .FirstAsync(x => x.Id == application.Id);

        Assert.Equal(previousStatus, updatedApplication.GetCurrentStatus());
        Assert.Empty(updatedApplication.WithdrawalReasons);
        Assert.Null(updatedApplication.WithdrawalReasonOtherDetails);
        Assert.Null(updatedApplication.WithdrawnByUserId);

        var propertyName = FellingLicenceStatusConstants.SubmitStatuses.Contains(previousStatus)
            ? null
            : application.SubmittedFlaPropertyDetail!.Name;
        Guid? linkedPropertyProfileId = FellingLicenceStatusConstants.SubmitStatuses.Contains(previousStatus)
            ? application.LinkedPropertyProfile!.PropertyProfileId
            : null;

        Assert.Equal(application.Id, result.Value.ApplicationId);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(propertyName, result.Value.PropertyName);
        Assert.Equal(linkedPropertyProfileId, result.Value.LinkedPropertyProfileId);
        Assert.Equal(submittedDate, result.Value.SubmittedDate);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminHubName);
        Assert.Equal(application.CreatedById, result.Value.AuthorId);
    }

    [Theory, AutoMoqData]
    public async Task WhenRevertingFromWithdrawnToPreviousStatusClearsAdminOfficerReview(Guid performingUserId)
    {
        var sut = CreateSut();

        var application = Fixture.Create<FellingLicenceApplication>();

        application.StatusHistories.Clear();

        application.StatusHistories.Add(new StatusHistory
        {
            Status = FellingLicenceStatus.Withdrawn,
            Created = DateTime.UtcNow
        });
        application.StatusHistories.Add(new StatusHistory
        {
            Status = FellingLicenceStatus.ReturnedToApplicant,
            Created = DateTime.UtcNow.AddDays(-1)
        });
        application.StatusHistories.Add(new StatusHistory
        {
            Status = FellingLicenceStatus.Submitted,
            Created = DateTime.UtcNow.AddDays(-2)
        });

        application.AdminOfficerReview = new Entities.AdminOfficerReview
        {
            FellingLicenceApplicationId = application.Id,
            AdminOfficerReviewComplete = true
        };

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.TryRevertApplicationFromWithdrawnAsync(performingUserId, application.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApplication = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.StatusHistories)
            .Include(x => x.AdminOfficerReview)
            .FirstAsync(x => x.Id == application.Id);

        Assert.Equal(FellingLicenceStatus.ReturnedToApplicant, updatedApplication.GetCurrentStatus());
        Assert.Empty(updatedApplication.WithdrawalReasons);
        Assert.Null(updatedApplication.WithdrawalReasonOtherDetails);
        Assert.Null(updatedApplication.WithdrawnByUserId);
        Assert.NotNull(updatedApplication.AdminOfficerReview);
        Assert.Equal(performingUserId, updatedApplication.AdminOfficerReview.LastUpdatedById);
        Assert.True(updatedApplication.AdminOfficerReview.LastUpdatedDate <= DateTime.UtcNow);
        Assert.False(application.AdminOfficerReview.AdminOfficerReviewComplete);


        var submittedDate = application.StatusHistories
            .OrderByDescending(x => x.Created)
            .Single(x => x.Status == FellingLicenceStatus.Submitted).Created;
        var linkedPropertyProfileId = application.LinkedPropertyProfile!.PropertyProfileId;

        Assert.Equal(application.Id, result.Value.ApplicationId);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Null(result.Value.PropertyName);
        Assert.Equal(linkedPropertyProfileId, result.Value.LinkedPropertyProfileId);
        Assert.Equal(submittedDate, result.Value.SubmittedDate);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminHubName);
        Assert.Equal(application.CreatedById, result.Value.AuthorId);
    }

    [Theory, CombinatorialData]
    public async Task WhenRevertingFromWithdrawnToPreviousStatusShouldNotClearAdminOfficerReview(FellingLicenceStatus previousStatus)
    {
        if (previousStatus is FellingLicenceStatus.ReturnedToApplicant)
        {
            return;
        }

        var performingUserId = Guid.NewGuid();
        var sut = CreateSut();
        var application = Fixture.Create<FellingLicenceApplication>();

        application.StatusHistories.Clear();

        application.StatusHistories.Add(new StatusHistory
        {
            Status = FellingLicenceStatus.Withdrawn,
            Created = DateTime.UtcNow
        });
        application.StatusHistories.Add(new StatusHistory
        {
            Status = previousStatus,
            Created = DateTime.UtcNow.AddDays(-1)
        });
        if (previousStatus != FellingLicenceStatus.Submitted)
        {
            application.StatusHistories.Add(new StatusHistory
            {
                Status = FellingLicenceStatus.Submitted,
                Created = DateTime.UtcNow.AddDays(-2)
            });
        }

        var submittedDate = application.StatusHistories
            .OrderByDescending(x => x.Created)
            .First(x => x.Status == FellingLicenceStatus.Submitted).Created;

        application.AdminOfficerReview = new Entities.AdminOfficerReview
        {
            FellingLicenceApplicationId = application.Id,
            AdminOfficerReviewComplete = true
        };

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.TryRevertApplicationFromWithdrawnAsync(performingUserId, application.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApplication = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.StatusHistories)
            .Include(x => x.AdminOfficerReview)
            .FirstAsync(x => x.Id == application.Id);

        Assert.Equal(previousStatus, updatedApplication.GetCurrentStatus());
        Assert.Empty(updatedApplication.WithdrawalReasons);
        Assert.Null(updatedApplication.WithdrawalReasonOtherDetails);
        Assert.Null(updatedApplication.WithdrawnByUserId);
        Assert.NotNull(updatedApplication.AdminOfficerReview);
        Assert.True(updatedApplication.AdminOfficerReview.LastUpdatedDate <= DateTime.UtcNow);
        Assert.True(application.AdminOfficerReview.AdminOfficerReviewComplete);

        var propertyName = FellingLicenceStatusConstants.SubmitStatuses.Contains(previousStatus)
            ? null
            : application.SubmittedFlaPropertyDetail!.Name;
        Guid? linkedPropertyProfileId = FellingLicenceStatusConstants.SubmitStatuses.Contains(previousStatus)
            ? application.LinkedPropertyProfile!.PropertyProfileId
            : null;

        Assert.Equal(application.Id, result.Value.ApplicationId);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(propertyName, result.Value.PropertyName);
        Assert.Equal(linkedPropertyProfileId, result.Value.LinkedPropertyProfileId);
        Assert.Equal(submittedDate, result.Value.SubmittedDate);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminHubName);
        Assert.Equal(application.CreatedById, result.Value.AuthorId);
    }

    private UpdateFellingLicenceApplicationService CreateSut()
    {
        _mockCaseNotes.Reset();
        _mockClock.Reset();

        return new UpdateFellingLicenceApplicationService(
            new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext),
            _mockCaseNotes.Object,
            _mockGetConfiguredFcAreas.Object,
            _mockClock.Object,
            new NullLogger<UpdateFellingLicenceApplicationService>(),
            new Mock<IOptions<FellingLicenceApplicationOptions>>().Object);
    }
}
