namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a ExternalConsulteeCommentConfirmation notification.
/// </summary>
public class ExternalConsulteeCommentConfirmationDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets a URL to the application; not actually used in this notification but
    /// included as this is an application specific notification and hence implements
    /// <see cref="IApplicationNotification"/>.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the application reference for the application the comment was made on.
    /// </summary>
    public string ApplicationReference { get; set; }
    
    /// <summary>
    /// Gets and sets the id of the application that the comment was made on.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name and address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the name of the property involved in the application.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the full name of the consultee that made the comment.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets and sets a formatted date string of when the comment was made.
    /// </summary>
    public string? CommentReceivedDate { get; set; }

    /// <summary>
    /// Gets and sets the text of the comment that was made by the consultee.
    /// </summary>
    public string CommentText { get; set; }

    /// <summary>
    /// Gets and sets a list of the file names of any attachments that were included with the
    /// consultee's comment.
    /// </summary>
    public List<string> CommentAttachments { get; set; }
}