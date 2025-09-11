using AutoFixture;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
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
using Xunit;
using AssignedUserRole = Forestry.Flo.Services.FellingLicenceApplications.Entities.AssignedUserRole;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewServiceUpdateEiaScreeningCheckTests
{
    private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();
    private readonly FellingLicenceApplicationsContext _context = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
    private readonly InternalUserContextFlaRepository _fellingLicenceApplicationRepository;
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IViewCaseNotesService> _mockCaseNotesService = new();
    private readonly Mock<IAddDocumentService> _mockAddDocumentService = new();
    private readonly Mock<IOptions<DocumentVisibilityOptions>> _visibilityOptions = new();
    private readonly Instant _now = Instant.FromDateTimeUtc(DateTime.UtcNow);

    public UpdateWoodlandOfficerReviewServiceUpdateEiaScreeningCheckTests()
    {
        _fellingLicenceApplicationRepository = new InternalUserContextFlaRepository(_context);
    }

    [Fact]
    public async Task WhenWoodlandOfficerSuccessfullyMarksEiaScreeningAsComplete()
    {
        var sut = CreateSut();
        var woodlandOfficerId = Guid.NewGuid();

        var fla = CreateFellingLicenceApplication(woodlandOfficerId);

        _context.FellingLicenceApplications.Add(fla);
        await _context.SaveChangesAsync();

        var result = await sut.CompleteEiaScreeningCheckAsync(
            fla.Id, 
            woodlandOfficerId, 
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedFla = _context.FellingLicenceApplications
            .Include(fellingLicenceApplication => fellingLicenceApplication.WoodlandOfficerReview!)
            .First(x => x.Id == fla.Id);

        Assert.NotNull(updatedFla.WoodlandOfficerReview);
        Assert.True(updatedFla.WoodlandOfficerReview.EiaScreeningComplete);
        Assert.InRange(
            updatedFla.WoodlandOfficerReview.LastUpdatedDate,
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));
        Assert.Equal(woodlandOfficerId, updatedFla.WoodlandOfficerReview.LastUpdatedById);
    }

    [Fact]
    public async Task CompleteEiaScreeningCheckAsync_ReturnsFailure_WhenApplicationNotInWoodlandOfficerReviewState()
    {
        var sut = CreateSut();
        var woodlandOfficerId = Guid.NewGuid();
        var fla = CreateFellingLicenceApplication(woodlandOfficerId);

        // Change status to something else to fail the state check
        fla.StatusHistories.Clear();
        fla.StatusHistories.Add(new StatusHistory
        {
            Created = DateTime.UtcNow.AddDays(-1),
            Status = FellingLicenceStatus.Draft
        });

        _context.FellingLicenceApplications.Add(fla);
        await _context.SaveChangesAsync();

        var result = await sut.CompleteEiaScreeningCheckAsync(fla.Id, woodlandOfficerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("unable to be updated", result.Error, StringComparison.OrdinalIgnoreCase);

        var updatedFla = _context.FellingLicenceApplications
            .Include(fellingLicenceApplication => fellingLicenceApplication.WoodlandOfficerReview!)
            .First(x => x.Id == fla.Id);

        Assert.NotNull(updatedFla.WoodlandOfficerReview);
        Assert.False(updatedFla.WoodlandOfficerReview.EiaScreeningComplete);
    }

    [Fact]
    public async Task CompleteEiaScreeningCheckAsync_ReturnsFailure_WhenUserIsNotAssignedWoodlandOfficer()
    {
        var sut = CreateSut();
        var woodlandOfficerId = Guid.NewGuid();
        var fla = CreateFellingLicenceApplication(woodlandOfficerId);

        // Remove WO assignment to fail the user check
        fla.AssigneeHistories.Clear();

        _context.FellingLicenceApplications.Add(fla);
        await _context.SaveChangesAsync();

        var result = await sut.CompleteEiaScreeningCheckAsync(fla.Id, woodlandOfficerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("unable to be updated", result.Error, StringComparison.OrdinalIgnoreCase);

        var updatedFla = _context.FellingLicenceApplications
            .Include(fellingLicenceApplication => fellingLicenceApplication.WoodlandOfficerReview!)
            .First(x => x.Id == fla.Id);

        Assert.NotNull(updatedFla.WoodlandOfficerReview);
        Assert.False(updatedFla.WoodlandOfficerReview.EiaScreeningComplete);
    }

    private UpdateWoodlandOfficerReviewService CreateSut()
    {
        _clock.Reset();
        _mockCaseNotesService.Reset();
        _mockAddDocumentService.Reset();

        _clock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        var configuration = new WoodlandOfficerReviewOptions
        {
            PublicRegisterPeriod = TimeSpan.FromDays(30)
        };

        _visibilityOptions.Setup(x => x.Value).Returns(new DocumentVisibilityOptions
        {
            ApplicationDocument = new DocumentVisibilityOptions.VisibilityOptions
            {
                VisibleToApplicant = true,
                VisibleToConsultees = true
            },
            ExternalLisConstraintReport = new DocumentVisibilityOptions.VisibilityOptions
            {
                VisibleToApplicant = true,
                VisibleToConsultees = true
            },
            FcLisConstraintReport = new DocumentVisibilityOptions.VisibilityOptions
            {
                VisibleToApplicant = false,
                VisibleToConsultees = false
            },
            SiteVisitAttachment = new DocumentVisibilityOptions.VisibilityOptions
            {
                VisibleToApplicant = true,
                VisibleToConsultees = true
            }
        });

        return new UpdateWoodlandOfficerReviewService(
            _fellingLicenceApplicationRepository,
            _clock.Object,
            new OptionsWrapper<WoodlandOfficerReviewOptions>(configuration),
            _mockCaseNotesService.Object,
            _mockAddDocumentService.Object,
            new NullLogger<UpdateWoodlandOfficerReviewService>(),
            _visibilityOptions.Object);
    }

    private FellingLicenceApplication CreateFellingLicenceApplication(Guid? assignedWoId = null)
    {
        var fla = _fixture.Create<FellingLicenceApplication>();

        fla.StatusHistories.Clear();

        var now = DateTime.UtcNow;

        fla.StatusHistories.Add(_fixture.Build<StatusHistory>()
            .Without(x => x.FellingLicenceApplicationId)
            .Without(x => x.FellingLicenceApplication)
            .With(x => x.Created, now.AddDays(-1))
            .With(x => x.Status, FellingLicenceStatus.WoodlandOfficerReview)
            .Create());

        fla.AssigneeHistories.Add(_fixture.Build<AssigneeHistory>()
            .Without(x => x.FellingLicenceApplicationId)
            .Without(x => x.FellingLicenceApplication)
            .Without(x => x.TimestampUnassigned)
            .With(x => x.TimestampAssigned, now.AddDays(-1))
            .With(x => x.Role, AssignedUserRole.WoodlandOfficer)
            .With(x => x.AssignedUserId, assignedWoId ?? Guid.NewGuid())
            .Create());

        fla.WoodlandOfficerReview ??= new WoodlandOfficerReview();

        fla.WoodlandOfficerReview.WoodlandOfficerReviewComplete = false;
        fla.WoodlandOfficerReview.EiaScreeningComplete = false;
        fla.WoodlandOfficerReview.LastUpdatedDate = now.AddDays(-1);

        return fla;
    }
}