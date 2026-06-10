namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into an InformApplicantOfApplicationReferenceChange notification.
/// </summary>
public class InformApplicantOfApplicationReferenceChangeDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets a URL to the application for the external applicant application.
    /// <see cref="IApplicationNotification"/>.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the new application reference for the application.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the old application reference for the application.
    /// </summary>
    public string OldApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the id of the application that has changed reference number.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name and address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the full name of the applicant that the notification is for.
    /// </summary>
    public string? Name { get; set; }
}