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
    public async Task CanGetSubmittedFlaPropertyCompartmentById(FellingLicenceApplication application)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var expected = application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments[0];

        var result = await _sut.GetSubmittedFlaPropertyCompartmentByIdAsync(expected.Id, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(expected, result.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetSubmittedFlaPropertyCompartmentByIdWithUnknownId(FellingLicenceApplication application, Guid unknownCompartmentId)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetSubmittedFlaPropertyCompartmentByIdAsync(unknownCompartmentId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }
}