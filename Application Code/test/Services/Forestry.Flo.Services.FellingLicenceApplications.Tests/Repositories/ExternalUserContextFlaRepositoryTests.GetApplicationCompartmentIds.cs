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
    public async Task CanGetApplicationCompartmentIds(FellingLicenceApplication application)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var expectedIds = application.LinkedPropertyProfile.ProposedFellingDetails.Select(x => x.PropertyProfileCompartmentId).ToList();

        var result = await _sut.GetApplicationComparmentIdsAsync(application.Id, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equivalent(expectedIds, result.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationCompartmentIdsShouldReturnEmptyCollection_WhenNoApplicationExistsForId(Guid applicationId)
    {
        //arrange
        //act
        var result = await _sut.GetApplicationComparmentIdsAsync(applicationId, CancellationToken.None);
        //assert
        Assert.True(result.HasNoValue);
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationCompartmentIdsReturnEmptyCollection_WhenNoFellingExistsForApplication(FellingLicenceApplication application)
    {
        application.LinkedPropertyProfile.ProposedFellingDetails.Clear();

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetApplicationComparmentIdsAsync(application.Id, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }
}