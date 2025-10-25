using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CanVerifyWoodlandOwnerIdForApplication(FellingLicenceApplication application)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.VerifyWoodlandOwnerIdForApplicationAsync(application.WoodlandOwnerId, application.Id, CancellationToken.None);

        Assert.True(result);
    }

    [Theory, AutoMoqData]
    public async Task VerifyWoodlandOwnerIdForApplicationShouldReturnFalse_WhenNoApplicationsExist(Guid woodlandOwnerId)
    {
        //arrange
        //act
        var result = await _sut.VerifyWoodlandOwnerIdForApplicationAsync(woodlandOwnerId, Guid.NewGuid(), CancellationToken.None);
        //assert
        Assert.False(result);
    }

    [Theory, AutoMoqData]
    public async Task VerifyWoodlandOwnerIdForApplicationShouldReturnFalse_WhenIncorrectWoodlandOwnerIdGivenForApplication(
        FellingLicenceApplication application,
        Guid otherWoodlandOwnerId)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.VerifyWoodlandOwnerIdForApplicationAsync(otherWoodlandOwnerId, application.Id, CancellationToken.None);

        Assert.False(result);
    }
}