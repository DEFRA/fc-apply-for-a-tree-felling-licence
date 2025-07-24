using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

public enum AgentAuthorityStatus
{
    [Display(Name = "Created")]
    Created = 0,

    [Display(Name = "Form Uploaded")]
    FormUploaded = 1,

    [Display(Name = "Deactivated")]
    Deactivated = 2
}