using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;

public class AmendmentsWithApplicantSpecification : ISubStatusSpecification
{
    /// <inheritdoc />
    public WoodlandOfficerReviewSubStatus SubStatus => WoodlandOfficerReviewSubStatus.AmendmentsWithApplicant;

    /// <inheritdoc />
    public bool IsSatisfiedBy(FellingLicenceApplication application)
    {
        var latestAmendmentReview = application.WoodlandOfficerReview?.FellingAndRestockingAmendmentReviews
            .Where(x => x.AmendmentReviewCompleted != true)
            .OrderByDescending(x => x.AmendmentsSentDate)
            .FirstOrDefault();

        return latestAmendmentReview is not null && latestAmendmentReview.ResponseReceivedDate is null;
    }
}