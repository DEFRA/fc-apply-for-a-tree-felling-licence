using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// An enumeration of sub-statuses within the Woodland Officer review status.
/// </summary>
public enum WoodlandOfficerReviewSubStatus
{
    /// <summary>
    /// When the application is active on the public register.
    /// </summary>
    [Display(Name = "Public register")]
    OnPublicRegister,
    /// <summary>
    /// When the applicant is currently reviewing amendments made by the woodland officer.
    /// </summary>
    [Display(Name = "Amendments")]
    AmendmentsWithApplicant,
}