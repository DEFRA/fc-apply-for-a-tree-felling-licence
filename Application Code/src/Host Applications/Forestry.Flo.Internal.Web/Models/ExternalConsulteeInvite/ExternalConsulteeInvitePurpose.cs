using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

/// <summary>
/// Enum of the purposes for inviting an external consultee.
/// </summary>
public enum ExternalConsulteeInvitePurpose
{
    [Display(Name = "Statutory")]
    [Description("Consultation is required by law")]
    Statutory,

    [Display(Name = "Mandatory")]
    [Description("Consultation is required under Forestry Commission policy")]
    Mandatory,

    [Display(Name = "Advice and guidance")]
    [Description("Consultation is not required, but their knowledge or expertise may help the G&R consultation process")]
    AdviceAndGuidance
}