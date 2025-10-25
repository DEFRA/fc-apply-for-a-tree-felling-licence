using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CanGetSubmittedFlaPropertyDetail(FellingLicenceApplication application)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetExistingSubmittedFlaPropertyDetailAsync(application.Id, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(application.SubmittedFlaPropertyDetail, result.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetSubmittedFlaPropertyDetailShouldReturnNone_WhenNoApplicationExistsForId(Guid applicationId, Guid woodlandOwnerId, Guid compartmentId)
    {
        //arrange
        //act
        var result = await _sut.GetExistingSubmittedFlaPropertyDetailAsync(applicationId, CancellationToken.None);
        //assert
        Assert.True(result.HasNoValue);
    }

    [Theory, AutoMoqData]
    public async Task GetSubmittedFlaPropertyDetailShouldReturnNone_WhenNoSubmittedFlaPropertyDetailExistsForApplication(FellingLicenceApplication application)
    {
        application.SubmittedFlaPropertyDetail = null;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
     
        var result = await _sut.GetExistingSubmittedFlaPropertyDetailAsync(application.Id, CancellationToken.None);
        
        Assert.True(result.HasNoValue);
    }
}