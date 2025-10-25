using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;

public class AmendmentsWithApplicantSpecification : ISubStatusSpecification
{
    /// <inheritdoc />
    public WoodlandOfficerReviewSubStatus SubStatus => WoodlandOfficerReviewSubStatus.AmendmentsWithApplicant;

    /// <inheritdoc />
    public bool IsSatisfiedBy(FellingLicenceApplication application)
        => application.WoodlandOfficerReview?.FellingAndRestockingAmendmentReviews
            .Any(x => x.AmendmentReviewCompleted is not true) ?? false;
}