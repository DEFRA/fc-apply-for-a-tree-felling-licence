namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into an ApplicantReviewedAmendments notification.
/// </summary>

public class ApplicantReviewedAmendmentsDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets and sets the application reference id.
    /// </summary>
    public required string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name of the property the application is for.
    /// </summary>
    public required string PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application.
    /// </summary>
    public required string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public required string AdminHubFooter { get; set; }
}