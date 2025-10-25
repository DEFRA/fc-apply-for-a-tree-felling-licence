using System;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CanUpdateSubmittedFlaPropertyCompartmentZones(
        FellingLicenceApplication application,
        bool zone1,
        bool zone2,
        bool zone3)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var toUpdate = application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments[0];

        var result = await _sut.UpdateSubmittedFlaPropertyCompartmentZonesAsync(
            toUpdate.Id, zone1, zone2, zone3, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await _fellingLicenceApplicationsContext.SubmittedFlaPropertyCompartments.FindAsync(toUpdate.Id);
        Assert.NotNull(updated);
        Assert.Equal(zone1, updated!.Zone1);
        Assert.Equal(zone2, updated.Zone2);
        Assert.Equal(zone3, updated.Zone3);
    }

    [Theory, AutoMoqData]
    public async Task UpdateSubmittedFlaPropertyCompartmentZonesWithUnknownCompartmentId(
        FellingLicenceApplication application,
        Guid unknownCompartmentId,
        bool zone1,
        bool zone2,
        bool zone3)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateSubmittedFlaPropertyCompartmentZonesAsync(
            unknownCompartmentId, zone1, zone2, zone3, CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}