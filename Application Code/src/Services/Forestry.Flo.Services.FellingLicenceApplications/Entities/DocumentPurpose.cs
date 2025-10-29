using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public enum DocumentPurpose
    {
        [Display(Name = "Supporting document")]
        Attachment,
        [Display(Name = "Licence document")]
        ApplicationDocument,
        [Display(Name = "Correspondence")]
        Correspondence,
        [Display(Name = "Constraint report")]
        FcLisConstraintReport,
        [Display(Name = "External constraint report")]
        ExternalLisConstraintReport,
        [Display(Name = "Site visit evidence")]
        SiteVisitAttachment,
        [Display(Name = "Consultation attachment")]
        ConsultationAttachment,
        [Display(Name = "Environmental Impact Assessment")]
        EiaAttachment,
        [Display(Name = "WMP document")]
        WmpDocument
    }
}
