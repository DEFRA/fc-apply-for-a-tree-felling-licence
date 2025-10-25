using System;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CanUpdateExistingWoodlandOfficerReviewForResubmission(
        FellingLicenceApplication application,
        DateTime updatedDate)
    {
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateExistingWoodlandOfficerReviewFlagsForResubmission(application.Id, updatedDate, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await _fellingLicenceApplicationsContext.WoodlandOfficerReviews
            .FirstOrDefaultAsync(w => w.FellingLicenceApplicationId == application.Id);
        Assert.NotNull(updated);
        Assert.Equal(updatedDate, updated!.LastUpdatedDate);
        Assert.False(updated.DesignationsComplete);
        Assert.False(updated.ConfirmedFellingAndRestockingComplete);
        Assert.Null(updated.ConditionsToApplicantDate);
        Assert.False(updated.WoodlandOfficerReviewComplete);
    }

    [Theory, AutoMoqData]
    public async Task UpdateExistingWoodlandOfficerReviewForResubmissionWithNoReviewExists(
        FellingLicenceApplication application,
        DateTime updatedDate)
    {
        application.WoodlandOfficerReview = null;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.UpdateExistingWoodlandOfficerReviewFlagsForResubmission(application.Id, updatedDate, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_fellingLicenceApplicationsContext.WoodlandOfficerReviews);
    }
}