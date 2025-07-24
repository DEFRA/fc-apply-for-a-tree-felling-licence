namespace Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;

/// <summary>
/// Model class for the overall status of the admin officer review of an application.
/// </summary>
public class AdminOfficerReviewStatusModel
{
    /// <summary>
    /// Gets and sets a populated <see cref="Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview.AdminOfficerReviewTaskListStates"/> indicating the status of each
    /// step of the admin officer review process.
    /// </summary>
    public AdminOfficerReviewTaskListStates AdminOfficerReviewTaskListStates { get; init; }
}