using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Common.User;

public enum ActorType
{
    [Display(Name = "External Applicant")]
    ExternalApplicant,
    [Display(Name = "Internal User")]
    InternalUser,
    [Display(Name = "System")]
    System,
   // ExternalSystem
}
