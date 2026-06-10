namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Data model for the required information to send a notification to FC staff when a
/// new consultee comment is made on an application in the woodland officer review stage.
/// </summary>
/// <remarks>
/// This model is designed to be used as part of storing a consultee comment, as such the
/// details of the comment itself (author, content etc) will already be available to the
/// code sending the notification, and this 
/// </remarks>
public class ConsulteeCommentNotificationModel
{
    /// <summary>
    /// Gets and sets the application reference
    /// </summary>
    public string ApplicationReference { get; set; }
    
    /// <summary>
    /// Gets and sets the name of the admin hub managing the application, with which to
    /// retrieve the full admin hub address to go in the notification.
    /// </summary>
    public string AdminHub { get; set; }

    /// <summary>
    /// Gets and sets the name of the property that this application relates to.  This will be populated
    /// if the application is currently with FC; otherwise <see cref="LinkedPropertyProfileId"/> will
    /// be populated instead.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the ID of the linked property profile that this application relates to.  This will be populated
    /// if the application is currently with the applicant; otherwise <see cref="PropertyName"/> will be populated instead.
    /// </summary>
    public Guid? LinkedPropertyProfileId { get; set; }

    /// <summary>
    /// Gets and sets the IDs of any FC staff assigned to the application, to send
    /// the notification to.
    /// </summary>
    public Guid[] AssignedFcStaff { get; set; }
}