using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CanDeleteFla(
        FellingLicenceApplication application)
    {
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var taskResult = await _sut.DeleteFlaAsync(application, CancellationToken.None);
        Assert.True(taskResult.IsSuccess);

        Assert.Empty(_fellingLicenceApplicationsContext.FellingLicenceApplications);
        Assert.Empty(_fellingLicenceApplicationsContext.ProposedFellingDetails);
        Assert.Empty(_fellingLicenceApplicationsContext.LinkedPropertyProfiles);
        Assert.Empty(_fellingLicenceApplicationsContext.AssigneeHistories);
        Assert.Empty(_fellingLicenceApplicationsContext.StatusHistories);
    }
}