using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;

/// <summary>
/// A specification to determine if an application is in the Consultation sub-status.
/// </summary>
public class ConsultationSpecification : ISubStatusSpecification
{
    /// <inheritdoc />
    public WoodlandOfficerReviewSubStatus SubStatus => WoodlandOfficerReviewSubStatus.Consultation;

    /// <inheritdoc />
    public bool IsSatisfiedBy(FellingLicenceApplication application) =>
        application.WoodlandOfficerReview?.ConsultationsComplete is not true &&
        application.ExternalAccessLinks.Any();
}