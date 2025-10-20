using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;

public class WoodlandOfficerReviewSubStatusService(IEnumerable<ISubStatusSpecification> specs) : IWoodlandOfficerReviewSubStatusService
{
    /// <inheritdoc />
    public HashSet<WoodlandOfficerReviewSubStatus> GetCurrentSubStatuses(FellingLicenceApplication application)
    {
        if (application.GetCurrentStatus() is not FellingLicenceStatus.WoodlandOfficerReview)
        {
            return [];
        }

        return specs
            .Where(spec => spec.IsSatisfiedBy(application))
            .Select(spec => spec.SubStatus)
            .ToHashSet();
    }
}