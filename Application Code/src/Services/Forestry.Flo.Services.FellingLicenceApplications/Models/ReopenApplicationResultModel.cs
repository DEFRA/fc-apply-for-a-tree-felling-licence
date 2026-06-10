namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Model class of data items to return from a successful reopen application operation in
/// order to send confirmation notifications.
/// </summary>
public class ReopenApplicationResultModel
{
    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the name of the property the application is for, if the application is currently with FC.
    /// </summary>
    /// <remarks>
    /// If the application has reopened into a "with-FC" state, this will be the name of the property
    /// in the submitted property snapshot.  If the application has reopened into a "with-applicant" state,
    /// this will be null and the <see cref="LinkedPropertyProfileId"/> field will be populated instead.
    /// </remarks>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the id of the linked property profile, if the application is currently with the applicant.
    /// </summary>
    /// <remarks>
    /// If the application has reopened into a "with-applicant" state, this will be the id of the property profile
    /// linked to the application. If the application has reopened into a "with-FC" state, this will be null and
    /// the <see cref="PropertyName"/> field will be populated instead.
    /// </remarks>
    public Guid? LinkedPropertyProfileId { get; set; }

    /// <summary>
    /// Gets and sets the name of the admin hub the application is managed by.
    /// </summary>
    public string AdminHubName { get; set; }

    /// <summary>
    /// Gets and sets the date the application was originally submitted, to be included in the notification to the
    /// applicant.
    /// </summary>
    public DateTime SubmittedDate { get; set; }

    /// <summary>
    /// Gets and sets the id of the application author, used to retrieve their name and email address in order
    /// to send the confirmation notification.
    /// </summary>
    public Guid AuthorId { get; set; }
}