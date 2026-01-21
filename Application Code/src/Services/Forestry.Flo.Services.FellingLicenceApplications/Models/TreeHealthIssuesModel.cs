namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Model class for tree health issues information in a felling licence application.
/// </summary>
public class TreeHealthIssuesModel
{
    /// <summary>
    /// Gets and sets the status of the configurable tree health issues.
    /// </summary>
    public Dictionary<string, bool> TreeHealthIssueSelections { get; set; } = new();

    /// <summary>
    /// Gets and sets whether there is another tree health issue not listed in the configurable options.
    /// </summary>
    public bool OtherTreeHealthIssue { get; set; }

    /// <summary>
    /// Gets and sets the details of the other tree health issue if applicable.
    /// </summary>
    public string? OtherTreeHealthIssueDetails { get; set; }

    /// <summary>
    /// Gets and sets whether there are no tree health issues i.e. the user has checked
    /// the "No tree health or public safety reasons" option.
    /// </summary>
    public bool NoTreeHealthIssues { get; set; }
}