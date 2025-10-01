namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into any notifications for assigned users
/// on an application transitioning between states.
/// </summary>

public class InformAssignedUserOfApplicationStatusTransitionDataModel: IApplicationNotification
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the application reference id.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the name of the assigned user that transitioned the application.
    /// </summary>
    public string PreviousAssignedUserName { get; set; }

    /// <summary>
    /// Gets and sets the email address of the assigned user that transitioned the application.
    /// </summary>
    public string PreviousAssignedEmailAddress { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public required Guid ApplicationId { get; set; }

}