namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into an environmental impact assessment reminder notification.
/// </summary>
public class EnvironmentalImpactAssessmentReminderDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets or sets the unique reference for the application.
    /// </summary>
    public required string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public required Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the property name associated with the application.
    /// </summary>
    public required string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the submission time of the application, formatted as a string.
    /// </summary>
    public required string ApplicationSubmissionTime { get; set; }

    /// <summary>
    /// Gets or sets the name of the notification recipient.
    /// </summary>
    public required string RecipientName { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the sender of the notification.
    /// </summary>
    public required string SenderName { get; set; }

    /// <summary>
    /// Gets or sets the URI to the application form.
    /// </summary>
    public required string ApplicationFormUri { get; set; }

    /// <summary>
    /// Gets or sets the contact email address for further information.
    /// </summary>
    public required string ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact phone number for further information.
    /// </summary>
    public required string ContactNumber { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application on the external site.
    /// </summary>
    public string ViewApplicationURL { get; set; }
}