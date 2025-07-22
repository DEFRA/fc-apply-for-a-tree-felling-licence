using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.Reports;

public enum FellingLicenceApplicationStatusesForReporting
{
    [Display(Name = "Draft")]
    Draft,

    [Display(Name = "Received")]
    Received,

    [Display(Name = "Submitted")]
    Submitted,

    [Display(Name = "With Applicant")]
    WithApplicant,

    [Display(Name = "Woodland Officer Review")]
    WoodlandOfficerReview,

    [Display(Name = "Sent For Approval")]
    SentForApproval,

    [Display(Name = "Approved")]
    Approved,

    [Display(Name = "Refused")]
    Refused,

    [Display(Name = "Withdrawn")]
    Withdrawn,

    [Display(Name = "Returned to Applicant")]
    ReturnedToApplicant,

    [Display(Name = "Operations Admin Officer Review")]
    AdminOfficerReview,

    [Display(Name = "Referred to Local Authority")]
    ReferredToLocalAuthority
}
