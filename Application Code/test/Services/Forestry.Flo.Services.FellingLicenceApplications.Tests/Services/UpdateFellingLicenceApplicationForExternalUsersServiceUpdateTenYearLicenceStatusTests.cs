using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationForExternalUsersServiceUpdateTenYearLicenceStatusTests
{
    private IFellingLicenceApplicationExternalRepository? _fellingLicenceApplicationRepository;
    private FellingLicenceApplicationsContext? _fellingLicenceApplicationsContext;

    [Theory, AutoMoqData]
    public async Task WhenCannotFindApplication(
        Guid applicationId,
        UserAccessModel uam,
        bool isForTenYearLicence,
        string? woodlandManagementPlanReference)
    {
        woodlandManagementPlanReference = isForTenYearLicence ? woodlandManagementPlanReference ?? "WMP123" : null;

        var sut = CreateSut();

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            applicationId,
            uam,
            isForTenYearLicence,
            woodlandManagementPlanReference,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserIsNotAuthorisedForApplication(
        bool isForTenYearLicence,
        string? woodlandManagementPlanReference,
        FellingLicenceApplication application)
    {
        woodlandManagementPlanReference = isForTenYearLicence ? woodlandManagementPlanReference ?? "WMP123" : null;

        var sut = CreateSut();

        application.IsForTenYearLicence = null;
        application.WoodlandManagementPlanReference = null;
        application.Source = FellingLicenceApplicationSource.ApplicantUser;

        application.Documents = [];
        application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = null;

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [Guid.NewGuid()]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            application.Id,
            uam,
            isForTenYearLicence,
            woodlandManagementPlanReference,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.False(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.Null(updatedApplication.Value.IsForTenYearLicence);
        Assert.Null(updatedApplication.Value.WoodlandManagementPlanReference);
        Assert.Equal(FellingLicenceApplicationSource.ApplicantUser, updatedApplication.Value.Source);

    }

    [Theory, AutoMoqData]
    public async Task WhenIsNotTenYearLicenceUpdatesApplicationAndStepStatus(
        string woodlandManagementPlanReference,  // this will be ignored as it's not a ten-year licence
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsForTenYearLicence = null;
        application.WoodlandManagementPlanReference = null;
        application.Source = FellingLicenceApplicationSource.ApplicantUser;

        application.Documents = [];
        application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = null;

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            application.Id,
            uam,
            false,
            woodlandManagementPlanReference,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.False(updatedApplication.Value.IsForTenYearLicence);
        Assert.Null(updatedApplication.Value.WoodlandManagementPlanReference);
        Assert.Equal(FellingLicenceApplicationSource.ApplicantUser, updatedApplication.Value.Source);
        Assert.True(updatedApplication.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus);
    }

    [Theory, AutoMoqData]
    public async Task WhenIsNotTenYearLicenceWasPreviouslySetToTenYearLicenceUpdatesApplicationAndStepStatus(
        string woodlandManagementPlanReference,  // this will be removed as it's not a ten-year licence
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsForTenYearLicence = true;
        application.WoodlandManagementPlanReference = woodlandManagementPlanReference;
        application.Source = FellingLicenceApplicationSource.WoodlandManagementPlan;

        application.Documents.First().Purpose = DocumentPurpose.WmpDocument;
        application.Documents.First().DeletionTimestamp = null;
        application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = true;

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            application.Id,
            uam,
            false,
            woodlandManagementPlanReference,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.False(updatedApplication.Value.IsForTenYearLicence);
        Assert.Null(updatedApplication.Value.WoodlandManagementPlanReference);
        Assert.Equal(FellingLicenceApplicationSource.ApplicantUser, updatedApplication.Value.Source);
        Assert.True(updatedApplication.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus);
    }

    [Theory, AutoMoqData]
    public async Task WhenIsTenYearLicenceNoWmpDocsOrStepStatusUpdatesApplicationAndStepStatus(
        string woodlandManagementPlanReference,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsForTenYearLicence = null;
        application.WoodlandManagementPlanReference = null;
        application.Source = FellingLicenceApplicationSource.ApplicantUser;

        application.Documents = [];
        application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = null;

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            application.Id,
            uam,
            true,
            woodlandManagementPlanReference,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApplication.Value.IsForTenYearLicence);
        Assert.Equal(woodlandManagementPlanReference, updatedApplication.Value.WoodlandManagementPlanReference);
        Assert.Equal(FellingLicenceApplicationSource.WoodlandManagementPlan, updatedApplication.Value.Source);
        Assert.False(updatedApplication.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus);
    }

    [Theory, AutoMoqData]
    public async Task WhenIsTenYearLicenceWasPreviouslyNotTenYearLicenceUpdatesApplicationAndStepStatus(
        string woodlandManagementPlanReference,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsForTenYearLicence = false;
        application.WoodlandManagementPlanReference = null;
        application.Source = FellingLicenceApplicationSource.ApplicantUser;

        application.Documents = [];
        application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = true;

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            application.Id,
            uam,
            true,
            woodlandManagementPlanReference,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApplication.Value.IsForTenYearLicence);
        Assert.Equal(woodlandManagementPlanReference, updatedApplication.Value.WoodlandManagementPlanReference);
        Assert.Equal(FellingLicenceApplicationSource.WoodlandManagementPlan, updatedApplication.Value.Source);
        Assert.False(updatedApplication.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus);
    }

    [Theory, AutoMoqData]
    public async Task WhenIsTenYearLicenceWasPreviouslyCompleteTenYearLicenceStepUpdatesWmpRef(
        string woodlandManagementPlanReference,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsForTenYearLicence = true;
        application.Source = FellingLicenceApplicationSource.WoodlandManagementPlan;

        application.Documents.First().Purpose = DocumentPurpose.WmpDocument;
        application.Documents.First().DeletionTimestamp = null;
        application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = true;

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateTenYearLicenceStatusAsync(
            application.Id,
            uam,
            true,
            woodlandManagementPlanReference,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApplication.Value.IsForTenYearLicence);
        Assert.Equal(woodlandManagementPlanReference, updatedApplication.Value.WoodlandManagementPlanReference);
        Assert.Equal(FellingLicenceApplicationSource.WoodlandManagementPlan, updatedApplication.Value.Source);
        Assert.True(updatedApplication.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus);
    }

    private UpdateFellingLicenceApplicationForExternalUsersService CreateSut()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _fellingLicenceApplicationRepository = new ExternalUserContextFlaRepository(
            _fellingLicenceApplicationsContext,
            new Mock<IApplicationReferenceHelper>().Object,
            new Mock<IFellingLicenceApplicationReferenceRepository>().Object);

        return new UpdateFellingLicenceApplicationForExternalUsersService(
            _fellingLicenceApplicationRepository,
            new FakeClock(Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime())),
            new OptionsWrapper<FellingLicenceApplicationOptions>(new FellingLicenceApplicationOptions()),
            new NullLogger<UpdateFellingLicenceApplicationForExternalUsersService>());
    }
}