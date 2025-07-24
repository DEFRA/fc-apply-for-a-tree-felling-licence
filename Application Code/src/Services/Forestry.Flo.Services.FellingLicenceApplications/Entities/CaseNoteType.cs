using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

public enum CaseNoteType
{
    [Display(Name = "Case note")]
    CaseNote,
    [Display(Name = "Operations admin officer review comment")]
    AdminOfficerReviewComment,
    [Display(Name = "Woodland officer review comment")]
    WoodlandOfficerReviewComment,
    [Display(Name = "Site visit comment")]
    SiteVisitComment,
    [Display(Name = "Return to applicant comment")]
    ReturnToApplicantComment,
    [Display(Name = "Larch check comment")]
    LarchCheckComment,
    [Display(Name = "Cricket bat willow comment")]
    CBWCheckComment,
}