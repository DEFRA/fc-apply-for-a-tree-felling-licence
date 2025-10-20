using System;
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
    public async Task CanDeleteSubmittedFlaPropertyDetailForApplication(
        FellingLicenceApplication application)
    {
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
        
        var taskResult = await _sut.DeleteSubmittedFlaPropertyDetailForApplicationAsync(application.Id, CancellationToken.None);
        Assert.True(taskResult.IsSuccess);

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

        Assert.Null(result!.SubmittedFlaPropertyDetail);
    }

    [Theory, AutoMoqData]
    public async Task DeleteSubmittedFlaPropertyDetailForApplicationWithUnknownApplicationId(
        FellingLicenceApplication application,
        Guid otherApplicationId)
    {
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var taskResult = await _sut.DeleteSubmittedFlaPropertyDetailForApplicationAsync(otherApplicationId, CancellationToken.None);
        Assert.True(taskResult.IsSuccess);

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

        Assert.NotNull(result!.SubmittedFlaPropertyDetail);
    }
}