namespace Forestry.Flo.Services.FellingLicenceApplications.Configuration;

/// <summary>
/// Configuration settings for tree health issues.
/// </summary>
public class TreeHealthOptions
{
    public static string ConfigurationKey => "TreeHealth";

    /// <summary>
    /// Gets and sets the list of tree health issues.
    /// </summary>
    /// <remarks>
    /// The UI will also make available an "Other" option so this should
    /// not be included in this configured list.
    /// </remarks>
    public List<string> TreeHealthIssues { get; set; } = new()
    {
        "Ash dieback (Hymenoscyphus fraxineus)",
        "Ramorum dieback (Phytophora ramorum)",
        "Phytophora pluvialis",
        "European spruce bark beetle (Ips typographus)",
        "Windblow",
        "Wildfire"
    };
}