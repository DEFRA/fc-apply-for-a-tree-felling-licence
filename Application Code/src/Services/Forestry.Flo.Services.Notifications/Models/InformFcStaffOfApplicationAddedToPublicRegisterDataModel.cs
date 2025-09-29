namespace Forestry.Flo.Services.Notifications.Models;

public class InformFcStaffOfApplicationAddedToPublicRegisterDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the name of the property the application is for.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the date the application was published to the public register.
    /// </summary>
    public string PublishedDate { get; set; }

    /// <summary>
    /// Gets and sets the date the application will expire from the public register.
    /// </summary>
    public string ExpiryDate { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public required Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application.
    /// </summary>
    public string ViewApplicationURL { get; set; }
}