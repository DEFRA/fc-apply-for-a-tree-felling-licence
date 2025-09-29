namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into an InformFCStaffOfReturnedApplication notification.
/// </summary>

public class InformFCStaffOfReturnedApplicationDataModel : IApplicationNotification
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
    /// Gets and sets the property name of the application.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the case note content detailing why the application has been returned.
    /// </summary>
    public string CaseNoteContent { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application on the internal site.
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