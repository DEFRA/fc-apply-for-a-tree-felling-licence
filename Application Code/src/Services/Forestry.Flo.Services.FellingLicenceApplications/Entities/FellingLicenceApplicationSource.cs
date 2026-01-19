using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public enum FellingLicenceApplicationSource
    {
        [Display(Name = "Input by applicant")]
        ApplicantUser,

        [Display(Name = "Paper-based submission")]
        PaperBasedSubmission,

        [Display(Name="Digital assistance")]
        DigitalAssistance,

        [Display(Name="Woodland management plan (ten-year licence)")]
        WoodlandManagementPlan
    }
}
