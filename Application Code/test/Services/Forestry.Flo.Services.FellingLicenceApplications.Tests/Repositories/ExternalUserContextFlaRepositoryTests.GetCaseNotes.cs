using System.Linq;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CanGetCaseNotesForFla(
        FellingLicenceApplication application)
    {
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetCaseNotesAsync(application.Id, false, CancellationToken.None);
        
        Assert.Equivalent(application.CaseNotes, result);
    }

    [Theory, AutoMoqData]
    public async Task CanGetCaseNotesForFlaRestrictedToApplicant(
        FellingLicenceApplication application)
    {
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetCaseNotesAsync(application.Id, true, CancellationToken.None);

        Assert.Equivalent(application.CaseNotes.Where(x => x.VisibleToApplicant), result);
    }
}