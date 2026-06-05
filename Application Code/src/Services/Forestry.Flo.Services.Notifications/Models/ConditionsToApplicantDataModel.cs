
namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a ConditionsToApplicant notification.
/// </summary>
public class ConditionsToApplicantDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the name of the woodland owner (which may be different from <see cref="Name"/>).
    /// </summary>
    public string? WoodlandOwnerName { get; set; }

    /// <summary>
    /// Gets and sets the application reference id.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public required Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name of the property the application is for.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the conditions text calculated by the system.
    /// </summary>
    public string ConditionsText { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application on the external site.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the name of the user who sent the notification.
    /// </summary>
    public string? SenderName { get; set; }

    /// <summary>
    /// Gets and sets the email address of the user who sent the notification.
    /// </summary>
    public string? SenderEmail { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating that a conditions notification has already been sent for this
    /// application, and that this notification supersedes the previous one.
    /// </summary>
    public bool SupersedesPreviousNotification { get; set; }

    /// <summary>
    /// Gets and sets a formatted string representing the date that the conditions are deemed to have been accepted
    /// by the applicant, which is calculated as 28 days after the created date of the application. This is used for display purposes in the notification email, and is not intended to be used for any calculations or logic in the system.
    /// </summary>
    public string? DeemedAcceptanceDate { get; set; }
}