using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CanAddSubmittedFlaPropertyDetail(
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail input)
    {
        application.SubmittedFlaPropertyDetail = null;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        input.FellingLicenceApplication = null;
        input.FellingLicenceApplicationId = application.Id;

        await _sut.AddSubmittedFlaPropertyDetailAsync(input, CancellationToken.None);

        var result = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(a => a.SubmittedFlaPropertyDetail)
            .ThenInclude(c => c!.SubmittedFlaPropertyCompartments)!
            .ThenInclude(f => f.ConfirmedFellingDetails)
            .ThenInclude(s => s!.ConfirmedFellingSpecies)
            .Include(a => a.SubmittedFlaPropertyDetail)
            .ThenInclude(c => c!.SubmittedFlaPropertyCompartments)!
            .ThenInclude(f => f.ConfirmedFellingDetails)!
            .ThenInclude(r => r!.ConfirmedRestockingDetails)!
            .ThenInclude(s => s!.ConfirmedRestockingSpecies)
            .Include(a => a.SubmittedFlaPropertyDetail)
            .ThenInclude(c => c!.SubmittedFlaPropertyCompartments)!
            .ThenInclude(c => c.SubmittedCompartmentDesignations)
            .SingleAsync();

        Assert.Equivalent(input, result.SubmittedFlaPropertyDetail);
    }
}