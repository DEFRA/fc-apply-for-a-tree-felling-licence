namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the current status of the conditions in the Woodland Officer review.
/// </summary>
public class ConditionsStatusModel
{
    /// <summary>
    /// Gets and sets a flag indicating whether or not the application is conditional.
    /// </summary>
    public bool? IsConditional { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the completed conditions were sent to the applicant.
    /// </summary>
    public DateTime? ConditionsToApplicantDate { get; set; }
}