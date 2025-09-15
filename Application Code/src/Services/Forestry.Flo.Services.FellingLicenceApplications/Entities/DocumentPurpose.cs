using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public enum DocumentPurpose
    {
        [Display(Name = "Attachment")]
        Attachment,
        [Display(Name = "Application document")]
        ApplicationDocument,
        [Display(Name = "Correspondence")]
        Correspondence,
        [Display(Name = "Constraint report")]
        FcLisConstraintReport,
        [Display(Name = "External constraint report")]
        ExternalLisConstraintReport,
        [Display(Name = "Site Visit Attachment")]
        SiteVisitAttachment,
        [Display(Name = "Consultation Attachment")]
        ConsultationAttachment,
        [Display(Name = "Environmental Impact Assessment")]
        EiaAttachment
    }
}
