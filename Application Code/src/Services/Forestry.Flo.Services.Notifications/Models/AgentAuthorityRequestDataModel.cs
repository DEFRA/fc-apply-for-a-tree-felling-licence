namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a AgentAuthorityRequest notification.
/// </summary>
public class AgentAuthorityRequestDataModel
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the name of the agency requesting authority.
    /// </summary>
    public string AgencyName { get; set; }

    /// <summary>
    /// Gets and sets a link for the recipient to approve or deny the request.
    /// </summary>
    public string ExternalAccessLink { get; set; }
}