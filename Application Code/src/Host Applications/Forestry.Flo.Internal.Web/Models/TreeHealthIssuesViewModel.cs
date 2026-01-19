using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Models;

/// <summary>
/// Shared view model for admin officer and woodland officer tree health issues checks.
/// </summary>
public class TreeHealthIssuesViewModel
{
    /// <summary>
    /// Gets and sets the id of the application.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Get or sets a list of documents related to the tree health or public safety issues.
    /// </summary>
    public IEnumerable<DocumentModel> TreeHealthDocuments { get; set; } = [];

    /// <summary>
    /// Gets and sets the tree health issues indicated by the applicant.
    /// </summary>
    public TreeHealthIssuesModel TreeHealthIssues { get; set; }
}