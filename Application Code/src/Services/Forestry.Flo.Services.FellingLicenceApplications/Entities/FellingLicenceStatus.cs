using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public enum FellingLicenceStatus
    {
        [DisplayNames("Received", "Received")]
        Received,

        [DisplayNames("Draft", "Draft")]
        Draft,

        [DisplayNames("Submitted", "Submitted")]
        Submitted,

        [DisplayNames("Applicant", "Applicant")]
        WithApplicant,

        [DisplayNames("WO review", "Woodland officer")]
        WoodlandOfficerReview,

        [DisplayNames("To approve", "Sent for approval")]
        SentForApproval,

        [DisplayNames("Approved", "Approved")]
        Approved,

        [DisplayNames("Refused", "Refused")]
        Refused,

        [DisplayNames("Withdrawn", "Withdrawn")]
        Withdrawn,

        [DisplayNames("Returned", "Returned to you")]
        ReturnedToApplicant,

        [DisplayNames("OAO review", "Admin officer")]
        AdminOfficerReview,

        [DisplayNames("LA referral", "In consultation")]
        ReferredToLocalAuthority
    }
}
