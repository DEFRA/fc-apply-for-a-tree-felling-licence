namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into an ApplicationSubmissionConfirmation notification.
/// </summary>

public class AmendmentsSentToApplicantDataModel : IApplicationNotification
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
    /// Gets and sets the name of the WO user who is being assigned the application.
    /// </summary>
    public string WoName { get; set; }

    public string ResponseDeadline { get; set; }

    /// <summary>
    /// Gets and sets the name of the propert.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the applicant to view the application.
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