namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into an InviteAgentUserToOrganisation notification.
/// </summary>
public class InviteAgentToOrganisationDataModel
{
    /// <summary>
    /// Gets and sets the name of the individual being invited.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the name of the woodland owner (organisation or person) inviting the individual.
    /// </summary>
    public string AgencyName { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to accept the invitation.
    /// </summary>
    public string InviteLink { get; set; }
}