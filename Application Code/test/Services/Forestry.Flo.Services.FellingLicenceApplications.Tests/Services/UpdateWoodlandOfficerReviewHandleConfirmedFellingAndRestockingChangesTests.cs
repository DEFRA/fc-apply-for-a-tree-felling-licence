using System;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewHandleConfirmedFellingAndRestockingChangesTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    private readonly FellingLicenceApplicationsContext _context;
    private readonly IFellingLicenceApplicationInternalRepository _internalRepository;

    public UpdateWoodlandOfficerReviewHandleConfirmedFellingAndRestockingChangesTests()
    {
        _context = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _internalRepository = new InternalUserContextFlaRepository(_context);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateWoodlandOfficerReviewEntity(
        FellingLicenceApplication application,
        Guid performingUserId)
    {
        //arrange

        var sut = CreateSut();

        application.StatusHistories =
        [
            new StatusHistory
            {
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                CreatedById = performingUserId,
                Created = DateTime.Today.ToUniversalTime()
            }
        ];
        application.AssigneeHistories =
        [
            new AssigneeHistory
            {
                Role = AssignedUserRole.WoodlandOfficer,
                AssignedUserId = performingUserId,
                TimestampAssigned = DateTime.Today.ToUniversalTime()
            }
        ];

        application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = false;

        _context.FellingLicenceApplications.Add(application);
        await _internalRepository.UnitOfWork.SaveEntitiesAsync();

        //act

        var result =
            await sut.HandleConfirmedFellingAndRestockingChangesAsync(application.Id, performingUserId, true,
                CancellationToken.None);

        //assert result is success

        Assert.True(result.IsSuccess);

        var updatedFlaMaybe = await _internalRepository.GetAsync(application.Id, CancellationToken.None);

        var updatedFla = updatedFlaMaybe.Value;

        //assert database values have been updated

        Assert.Equal(Now.ToDateTimeUtc(), updatedFla.WoodlandOfficerReview!.LastUpdatedDate);
        Assert.Equal(performingUserId, updatedFla.WoodlandOfficerReview.LastUpdatedById);
        Assert.True(updatedFla.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateWoodlandOfficerReviewEntity_WhenCBWrequireWOReviewIsFalse(
        FellingLicenceApplication application,
        Guid performingUserId)
    {
        // Arrange
        var sut = CreateSut();

        application.StatusHistories =
        [
            new StatusHistory
            {
                Status = FellingLicenceStatus.AdminOfficerReview,
                CreatedById = performingUserId,
                Created = DateTime.Today.ToUniversalTime()
            }
        ];
        application.AssigneeHistories =
        [
            new AssigneeHistory
            {
                Role = AssignedUserRole.WoodlandOfficer,
                AssignedUserId = performingUserId,
                TimestampAssigned = DateTime.Today.ToUniversalTime()
            }
        ];

        application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = false;

        _context.FellingLicenceApplications.Add(application);
        await _internalRepository.UnitOfWork.SaveEntitiesAsync();

        // Act
        var result = await sut.HandleConfirmedFellingAndRestockingChangesAsync(
            application.Id, performingUserId, true, CancellationToken.None, isSkippingWoReviewForCbw: true);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedFlaMaybe = await _internalRepository.GetAsync(application.Id, CancellationToken.None);
        var updatedFla = updatedFlaMaybe.Value;
        Assert.Equal(Now.ToDateTimeUtc(), updatedFla.WoodlandOfficerReview!.LastUpdatedDate);
        Assert.Equal(performingUserId, updatedFla.WoodlandOfficerReview.LastUpdatedById);
        Assert.True(updatedFla.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete);
    }

    [Theory, AutoMoqData]
    public async Task ShouldSucceed_WhenSkippingCbw_WithoutAssignedWoodlandOfficer(
        FellingLicenceApplication application,
        Guid performingUserId)
    {
        // Arrange
        var sut = CreateSut();

        application.StatusHistories =
        [
            new StatusHistory
            {
                Status = FellingLicenceStatus.AdminOfficerReview,
                CreatedById = performingUserId,
                Created = DateTime.Today.ToUniversalTime()
            }
        ];
        // No WO assignee on purpose to ensure bypass works

        application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = false;

        _context.FellingLicenceApplications.Add(application);
        await _internalRepository.UnitOfWork.SaveEntitiesAsync();

        // Act
        var result = await sut.HandleConfirmedFellingAndRestockingChangesAsync(
            application.Id, performingUserId, true, CancellationToken.None, isSkippingWoReviewForCbw: true);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedFlaMaybe = await _internalRepository.GetAsync(application.Id, CancellationToken.None);
        var updatedFla = updatedFlaMaybe.Value;
        Assert.Equal(Now.ToDateTimeUtc(), updatedFla.WoodlandOfficerReview!.LastUpdatedDate);
        Assert.Equal(performingUserId, updatedFla.WoodlandOfficerReview.LastUpdatedById);
        Assert.True(updatedFla.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete);
    }

    [Theory, AutoMoqData]
    public async Task ShouldFail_WhenSkippingCbw_ButNotInAdminOfficerReview(
        FellingLicenceApplication application,
        Guid performingUserId)
    {
        // Arrange
        var sut = CreateSut();

        // Put into WO review state instead of AO review
        application.StatusHistories =
        [
            new StatusHistory
            {
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                CreatedById = performingUserId,
                Created = DateTime.Today.ToUniversalTime()
            }
        ];
        // No WO assignee either

        application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = false;

        _context.FellingLicenceApplications.Add(application);
        await _internalRepository.UnitOfWork.SaveEntitiesAsync();

        // Act
        var result = await sut.HandleConfirmedFellingAndRestockingChangesAsync(
            application.Id, performingUserId, true, CancellationToken.None, isSkippingWoReviewForCbw: true);

        // Assert
        Assert.True(result.IsFailure);
        var updatedFlaMaybe = await _internalRepository.GetAsync(application.Id, CancellationToken.None);
        var updatedFla = updatedFlaMaybe.Value;
        Assert.False(updatedFla.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateConfirmedFlag_WhenSkippingCbw_SetToFalse(
        FellingLicenceApplication application,
        Guid performingUserId)
    {
        // Arrange
        var sut = CreateSut();

        application.StatusHistories =
        [
            new StatusHistory
            {
                Status = FellingLicenceStatus.AdminOfficerReview,
                CreatedById = performingUserId,
                Created = DateTime.Today.ToUniversalTime()
            }
        ];
        application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true; // start true to ensure change to false

        _context.FellingLicenceApplications.Add(application);
        await _internalRepository.UnitOfWork.SaveEntitiesAsync();

        // Act
        var result = await sut.HandleConfirmedFellingAndRestockingChangesAsync(
            application.Id, performingUserId, false, CancellationToken.None, isSkippingWoReviewForCbw: true);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedFlaMaybe = await _internalRepository.GetAsync(application.Id, CancellationToken.None);
        var updatedFla = updatedFlaMaybe.Value;
        Assert.Equal(Now.ToDateTimeUtc(), updatedFla.WoodlandOfficerReview!.LastUpdatedDate);
        Assert.Equal(performingUserId, updatedFla.WoodlandOfficerReview.LastUpdatedById);
        Assert.False(updatedFla.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete);
    }

    private new UpdateWoodlandOfficerReviewService CreateSut()
    {
        FellingLicenceApplicationRepository.Reset();
        UnitOfWork.Reset();
        Clock.Reset();
        MockCaseNotesService.Reset();
        MockAddDocumentService.Reset();
        Clock.Setup(x => x.GetCurrentInstant()).Returns(Now);

        var configuration = new WoodlandOfficerReviewOptions
        {
            PublicRegisterPeriod = TimeSpan.FromDays(30)
        };

        VisibilityOptions.Setup(x => x.Value).Returns(new DocumentVisibilityOptions
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
            _internalRepository,
            Clock.Object,
            new OptionsWrapper<WoodlandOfficerReviewOptions>(configuration),
            MockCaseNotesService.Object,
            MockAddDocumentService.Object,
            new NullLogger<UpdateWoodlandOfficerReviewService>(),
            VisibilityOptions.Object);
    }
}