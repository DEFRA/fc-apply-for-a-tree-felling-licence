namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

/// <summary>
/// View model for the check tree health issues step in the admin officer review process.
/// </summary>
public class CheckTreeHealthIssuesViewModel : AdminOfficerReviewCheckModelBase
{
    /// <summary>
    /// View model for the shared tree health issues details display.
    /// </summary>
    public TreeHealthIssuesViewModel TreeHealthIssuesViewModel { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the user has indicated that they have reviewed the tree health data.
    /// </summary>
    public bool Confirmed { get; set; }
}