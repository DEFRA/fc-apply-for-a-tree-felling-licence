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
}