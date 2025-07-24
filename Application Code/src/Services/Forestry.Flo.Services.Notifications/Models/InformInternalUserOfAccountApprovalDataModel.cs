namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a InformInternalUserOfAccountApproval notification.
/// </summary>
public class InformInternalUserOfAccountApprovalDataModel
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the internal user to login.
    /// </summary>
    public string? LoginUrl { get; set; }
}