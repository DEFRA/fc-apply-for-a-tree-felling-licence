using System.ComponentModel;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public enum ApplicationStepStatus
{
    [Description("Cannot Start Yet")]
    CannotStartYet,
    [Description("Not Started")]
    NotStarted,
    [Description("In Progress")]
    InProgress,
    [Description("Completed")]
    Completed,
    [Description("Amendment Required")]
    AmendmentRequired,
}