using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public enum InternalReviewStepStatus
{
    [Display(Name = "Cannot start yet")]
    CannotStartYet,
    [Display(Name = "Not started")]
    NotStarted,
    [Display(Name = "In progress")]
    InProgress,
    [Display(Name = "Completed")]
    Completed,
    [Display(Name = "Failed")]
    Failed,
    [Display(Name = "Not required")]
    NotRequired
}

