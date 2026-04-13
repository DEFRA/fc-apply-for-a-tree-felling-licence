namespace Forestry.Flo.Services.Notifications.Models;

public class InformFcStaffOfConsulteeCommentDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public required Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application.
    /// </summary>
    public string ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the name of the property for the application.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets a formatted date and time that the comment was received, to be merged into the notification template.
    /// </summary>
    public string CommentReceivedDate { get; set; }

    /// <summary>
    /// Gets and sets the full name of the consultee that made the comment.
    /// </summary>
    public string ConsulteeFullName { get; set; }

    /// <summary>
    /// Gets and sets the name of the organisation that the consultee that made the comment belongs to, if provided.
    /// </summary>
    public string ConsulteeOrganisation { get; set; }

    /// <summary>
    /// Gets and sets the job role of the consultee that made the comment, if provided.
    /// </summary>
    public string ConsulteeJobRole { get; set; }

    /// <summary>
    /// Gets and sets the content of the comment left by the consultee.
    /// </summary>
    public string CommentText { get; set; }
}