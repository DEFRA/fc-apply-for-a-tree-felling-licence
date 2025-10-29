using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests 
{
    [Theory, AutoMoqData]
    public async Task CanListForWoodlandOwnerId(FellingLicenceApplication application)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.ListAsync(application.WoodlandOwnerId, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Single(result);

        var resultApplication = result.Single();
        Assert.Equal(application.Id, resultApplication.Id);
        Assert.Equivalent(application.StatusHistories, resultApplication.StatusHistories);
        Assert.Equivalent(application.LinkedPropertyProfile, resultApplication.LinkedPropertyProfile);
    }

    [Theory, AutoMoqData]
    public async Task ListAsyncShouldReturnEmptyCollection_WhenNoApplicationsExist(Guid woodlandOwnerId)
    {
        //arrange
        //act
        var result = await _sut.ListAsync(woodlandOwnerId, CancellationToken.None);
        //assert
        Assert.Empty(result);
    }

    [Theory, AutoMoqData]
    public async Task ListAsyncShouldReturnEmptyCollection_WhenNoApplicationsExistForWoodlandOwnerId(
        FellingLicenceApplication application,
        Guid otherWoodlandOwnerId)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.ListAsync(otherWoodlandOwnerId, CancellationToken.None);

        Assert.Empty(result);
    }
}