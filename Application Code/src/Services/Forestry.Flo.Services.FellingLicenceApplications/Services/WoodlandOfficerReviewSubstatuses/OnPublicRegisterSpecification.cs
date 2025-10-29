using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;

public class OnPublicRegisterSpecification : ISubStatusSpecification
{
    /// <inheritdoc />
    public WoodlandOfficerReviewSubStatus SubStatus => WoodlandOfficerReviewSubStatus.OnPublicRegister;

    /// <inheritdoc />
    public bool IsSatisfiedBy(FellingLicenceApplication application)
        => application.PublicRegister is
        {
            ConsultationPublicRegisterPublicationTimestamp: not null,
            ConsultationPublicRegisterRemovedTimestamp: null
        };
}