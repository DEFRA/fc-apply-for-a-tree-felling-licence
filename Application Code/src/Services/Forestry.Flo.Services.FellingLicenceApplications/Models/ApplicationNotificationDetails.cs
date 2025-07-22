namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Model class representing details of an application for use in notifications.
/// </summary>
public class ApplicationNotificationDetails
{
    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the name of the property the application is for.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the name of the admin hub of the application.
    /// </summary>
    public string? AdminHubName { get; set; }
}