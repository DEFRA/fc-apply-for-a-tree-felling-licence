namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a InformAgentOfAgentAuthorityStatusChange notification.
/// </summary>
public class InformAgentOfAgentAuthorityStatusChangeDataModel
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the contact name of the woodland owner associated with the agent authority request.
    /// </summary>
    public string WoodlandOwnerName { get; set; }

    /// <summary>
    /// Gets and sets the name of the approval contact who processed the request.
    /// </summary>
    public string ProcessedByName { get; set; }

    /// <summary>
    /// Gets and sets the display name of the new agent authority status.
    /// </summary>
    public string AgentAuthorityStatus { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the request is successful.
    /// </summary>
    public bool AuthorityApproved { get; set; }
}