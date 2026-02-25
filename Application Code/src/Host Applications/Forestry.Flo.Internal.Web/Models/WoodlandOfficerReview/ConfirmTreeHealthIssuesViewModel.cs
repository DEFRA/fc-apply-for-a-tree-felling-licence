using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model for the woodland officer to confirm the tree health issues input by the applicant.
/// </summary>
public class ConfirmTreeHealthIssuesViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// View model for the shared tree health issues details display.
    /// </summary>
    public TreeHealthIssuesViewModel TreeHealthIssuesViewModel { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the user has indicated that there is a tree
    /// health or public safety reason to expedite the application.
    /// </summary>
    public bool? IsTreeHealthReasonToExpedite { get; set; }
}