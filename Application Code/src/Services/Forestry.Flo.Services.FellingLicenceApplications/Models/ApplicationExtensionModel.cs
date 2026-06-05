namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class ApplicationExtensionModel
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
    /// Gets and sets the application's created by ID.
    /// </summary>
    public Guid CreatedById { get; set; }
    /// <summary>
    /// Gets and sets the woodland owner ID.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }
    /// <summary>
    /// Gets and sets the IDs of FC users assigned to the application.
    /// </summary>
    public IList<Guid> AssignedFCUserIds { get; set; } = new List<Guid>();
    /// <summary>
    /// Gets and sets the final action date for the application.
    /// </summary>
    public DateTime FinalActionDate { get; set; }
    /// <summary>
    /// Gets and sets the extension length for the application, if it has been extended.
    /// </summary>
    public TimeSpan? ExtensionLength { get; set; }
    /// <summary>
    /// Gets and sets the submission date for the application.
    /// </summary>
    public DateTime SubmissionDate { get; set; }

    /// <summary>
    /// Gets and sets the name of the admin hub for the application.
    /// </summary>
    public string? AdminHubName { get; set; }

    /// <summary>
    /// Gets and sets the name of the property for the application. Null if the application is currently with the
    /// applicant, as in that case the property details need to be retrieved from the property profile service.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the linked property profile ID for the application. This is used to retrieve property details
    /// from the property profile service when the application is with the applicant, as in that case the application
    /// itself does not necessarily contain the up-to-date property name.
    /// </summary>
    public Guid? LinkedPropertyProfileId { get; set; }
}