namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class VoluntaryWithdrawalNotificationModel
{
    /// <summary>
    /// Gets and sets the application ID.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the name of the property the application is for.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the application's created by ID.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets and sets the woodland owner ID.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets and sets the With Applicant date for the application.
    /// </summary>
    public DateTime WithApplicantDate { get; set; }

    /// <summary>
    /// Gets and sets the Notification Date.
    /// </summary>
    public DateTime NotificationDateSent { get; set; }

    /// <summary>
    /// Gets and sets the admin region/hub name of the application.
    /// </summary>
    public string? AdministrativeRegion { get; set; }
}