namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a InformFcStaffOfApplicationRemovedFromDecisionPublicRegister notification.
/// </summary>
public class InformFCStaffOfDecisionPublicRegisterAutomaticRemovalOnExpiryDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the public register period expiry date for the application.
    /// </summary>
    public string DecisionPublicRegisterExpiryDate { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the name of the property for the application.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the name of the register the application is being removed from.
    /// </summary>
    public string RegisterName { get; set; }

    /// <summary>
    /// Gets and sets the date the application was added to the register.
    /// </summary>
    public string PublishDate { get; set; }
}