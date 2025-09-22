namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a InformFcStaffOfApplicationReferredToLocalAuthority notification.
/// </summary>
public class InformFcStaffOfApplicationReferredToLocalAuthorityDataModel : IApplicationNotification
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
    /// Gets and sets the name of the individual whom referred the application to the local authority.
    /// </summary>
    public string? ReferredToLocalAuthorityByName { get; set; }

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