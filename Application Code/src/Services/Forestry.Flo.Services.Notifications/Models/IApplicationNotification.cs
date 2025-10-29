using Newtonsoft.Json;

namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Contract for a notification data model referring to a specific application.
/// </summary>
public interface IApplicationNotification
{
    /// <summary>
    /// A URL to view the application in one of the portals.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the application reference id.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }
}