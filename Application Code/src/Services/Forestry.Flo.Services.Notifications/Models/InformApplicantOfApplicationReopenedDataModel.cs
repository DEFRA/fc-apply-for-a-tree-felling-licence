namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a InformApplicantOfApplicationReopened notification.
/// </summary>
public class InformApplicantOfApplicationReopenedDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets the full URL for the applicant to view the application.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
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

    /// <summary>
    /// Gets and sets the name of the property the application is for.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets a formatted string of the date the application was submitted.
    /// </summary>
    public string? SubmittedDate { get; set; }

    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string? Name { get; set; }
}