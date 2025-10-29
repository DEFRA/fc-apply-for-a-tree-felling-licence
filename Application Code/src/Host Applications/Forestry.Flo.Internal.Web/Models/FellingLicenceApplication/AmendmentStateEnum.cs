using System.ComponentModel;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public enum AmendmentStateEnum
{
    [Description("")]
    NoAmendment,
    [Description("")]
    NewAmendment,
    [Description("")]
    SentToApplicant,
    [Description("")]
    ApplicantAgreed,
    [Description("")]
    ApplicantDisagreed,
    [Description("")]
    Completed
}
