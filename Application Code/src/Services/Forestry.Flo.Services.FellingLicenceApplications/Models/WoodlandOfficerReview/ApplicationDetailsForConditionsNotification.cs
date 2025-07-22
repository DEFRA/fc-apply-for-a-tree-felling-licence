namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class representing application data required for the conditions to applicant notification.
/// </summary>
public record ApplicationDetailsForConditionsNotification
{
    /// <summary>
    /// Gets and sets the ID of the applicant user that authored the application.
    /// </summary>
    public Guid ApplicationAuthorId { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the property name.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the ID of the woodland owner for the application.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets and sets the administrative region for the application.
    /// </summary>
    public string? AdministrativeRegion { get; set; }
}