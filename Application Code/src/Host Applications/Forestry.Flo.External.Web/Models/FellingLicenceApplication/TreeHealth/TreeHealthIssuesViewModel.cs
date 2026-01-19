using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.TreeHealth;

/// <summary>
/// View model for collecting information about tree health issues in a felling licence application.
/// </summary>
public class TreeHealthIssuesViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets the name of the current task.
    /// </summary>
    public string TaskName => FellingLicenceApplicationSection.TreeHealthIssues.GetDescription();

    /// <summary>
    /// Gets or sets the reference number of the application.
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets or sets the breadcrumbs navigation model for the current page.
    /// </summary>
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets and sets the application summary details.
    /// </summary>
    public FellingLicenceApplicationSummary ApplicationSummary { get; set; }

    /// <summary>
    /// Gets and sets the tree health issues details.
    /// </summary>
    public TreeHealthIssuesModel TreeHealthIssues { get; set; }

    /// <summary>
    /// Get or sets a list of documents related to the tree health or public safety issues.
    /// </summary>
    public IEnumerable<DocumentModel> TreeHealthDocuments { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the application was created through data import.
    /// </summary>
    public bool FromDataImport { get; set; }
}