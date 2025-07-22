namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into an InviteWoodlandOwnerToOrganisation notification.
/// </summary>
//TODO - this is a sample model; when implementing the code to call this service for this notification we
//should analyse if this model is needed or whether the view model from the UI/use case that triggers it
//is sufficient. Content for all notifications (and hence the required data values to populate the notification)
//will need to be defined by FC.
public class InviteWoodlandOwnerToOrganisationDataModel
{
    /// <summary>
    /// Gets and sets the name of the individual being invited.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the name of the woodland owner (organisation or person) inviting the individual.
    /// </summary>
    public string WoodlandOwnerName { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to accept the invitation.
    /// </summary>
    public string InviteLink { get; set; }
}