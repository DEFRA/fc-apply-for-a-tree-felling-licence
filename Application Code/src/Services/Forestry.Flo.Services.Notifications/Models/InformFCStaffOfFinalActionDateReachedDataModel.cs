namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a InformFCStaffOfFinalActionDateReached notification.
/// </summary>
public class InformFCStaffOfFinalActionDateReachedDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the length of the extension in days.
    /// </summary>
    /// <remarks>Will be null if the application hasn't been extended.</remarks>
    public int? ExtensionLength { get; set; }

    /// <summary>
    /// Gets and sets the new final action date for the application.
    /// </summary>
    public string? FinalActionDate { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the number of days remaining until the final action date.
    /// </summary>
    /// <remarks>
    /// This will be positive if the final action date is yet to be reached, and negative when it has been exceeded.
    /// </remarks>
    public int DaysUntilFinalActionDate { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public required Guid ApplicationId { get; set; }
}