using Forestry.Flo.Services.Common.Models;
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
using NodaTime.Testing;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationForExternalUsersServiceUpdateApplicationTreeHealthIssuesDataTests
{
    private IFellingLicenceApplicationExternalRepository? _fellingLicenceApplicationRepository;
    private FellingLicenceApplicationsContext? _fellingLicenceApplicationsContext;

    [Theory, AutoMoqData]
    public async Task WhenCannotFindApplication(
        Guid applicationId,
        UserAccessModel uam,
        TreeHealthIssuesModel treeHealthData)
    {
        var sut = CreateSut();

        var result = await sut.UpdateApplicationTreeHealthIssuesDataAsync(
            applicationId,
            uam,
            treeHealthData,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserIsNotAuthorisedForApplication(
        TreeHealthIssuesModel treeHealthData,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsTreeHealthIssue = null;
        application.TreeHealthIssues = [];
        application.TreeHealthIssueOther = null;
        application.TreeHealthIssueOtherDetails = null;

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [Guid.NewGuid()]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
        
        var result = await sut.UpdateApplicationTreeHealthIssuesDataAsync(
            application.Id,
            uam,
            treeHealthData,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.False(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.Null(updatedApplication.Value.IsTreeHealthIssue);
        Assert.Empty(updatedApplication.Value.TreeHealthIssues);
        Assert.Null(updatedApplication.Value.TreeHealthIssueOther);
        Assert.Null(updatedApplication.Value.TreeHealthIssueOtherDetails);
    }


    [Theory, AutoMoqData]
    public async Task WhenNoTreeHealthIssues(
        TreeHealthIssuesModel treeHealthData,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsTreeHealthIssue = null;
        application.TreeHealthIssues = [];
        application.TreeHealthIssueOther = null;
        application.TreeHealthIssueOtherDetails = null;

        application.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus = null;

        treeHealthData.NoTreeHealthIssues = true; // this being true should then prevent the other fields being set

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateApplicationTreeHealthIssuesDataAsync(
            application.Id,
            uam,
            treeHealthData,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.False(updatedApplication.Value.IsTreeHealthIssue);
        Assert.Empty(updatedApplication.Value.TreeHealthIssues);
        Assert.Null(updatedApplication.Value.TreeHealthIssueOther);
        Assert.Null(updatedApplication.Value.TreeHealthIssueOtherDetails);

        Assert.True(application.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus);
    }

    [Theory, AutoMoqData]
    public async Task WhenSomeTreeHealthIssuesButNotOther(
        TreeHealthIssuesModel treeHealthData,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsTreeHealthIssue = null;
        application.TreeHealthIssues = [];
        application.TreeHealthIssueOther = null;
        application.TreeHealthIssueOtherDetails = null;

        application.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus = false;

        treeHealthData.NoTreeHealthIssues = false; // this being false should then allow the other fields to be set
        treeHealthData.OtherTreeHealthIssue = false;  // this being false should prevent other details being set

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateApplicationTreeHealthIssuesDataAsync(
            application.Id,
            uam,
            treeHealthData,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApplication.Value.IsTreeHealthIssue);

        var expectedList = treeHealthData.TreeHealthIssueSelections.Where(x => x.Value).Select(x => x.Key).ToList();

        Assert.Equivalent(expectedList, updatedApplication.Value.TreeHealthIssues);
        Assert.False(updatedApplication.Value.TreeHealthIssueOther);
        Assert.Null(updatedApplication.Value.TreeHealthIssueOtherDetails);

        Assert.True(application.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus);
    }

    [Theory, AutoMoqData]
    public async Task WhenSomeTreeHealthIssuesIncludingOther(
        TreeHealthIssuesModel treeHealthData,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.IsTreeHealthIssue = null;
        application.TreeHealthIssues = [];
        application.TreeHealthIssueOther = null;
        application.TreeHealthIssueOtherDetails = null;

        application.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus = false;

        treeHealthData.NoTreeHealthIssues = false; // this being false should then allow the other fields to be set
        treeHealthData.OtherTreeHealthIssue = true;  // this being true should allow other details to be set

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.UpdateApplicationTreeHealthIssuesDataAsync(
            application.Id,
            uam,
            treeHealthData,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.True(updatedApplication.Value.IsTreeHealthIssue);

        var expectedList = treeHealthData.TreeHealthIssueSelections.Where(x => x.Value).Select(x => x.Key).ToList();

        Assert.Equivalent(expectedList, updatedApplication.Value.TreeHealthIssues);
        Assert.True(updatedApplication.Value.TreeHealthIssueOther);
        Assert.Equal(treeHealthData.OtherTreeHealthIssueDetails, updatedApplication.Value.TreeHealthIssueOtherDetails);

        Assert.True(application.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus);
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