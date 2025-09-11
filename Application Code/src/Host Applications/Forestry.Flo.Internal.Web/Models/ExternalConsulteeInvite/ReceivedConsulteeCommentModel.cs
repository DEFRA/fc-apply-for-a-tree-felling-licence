namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

/// <summary>
/// View model for a received consultee comment.
/// </summary>
public class ReceivedConsulteeCommentModel
{
    /// <summary>
    /// Gets and sets the name of the author of the comment.
    /// </summary>
    public string AuthorName { get; set; }

    /// <summary>
    /// Gets and sets the comment text.
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// Gets and sets the timestamp when the comment was created.
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Gets and sets a dictionary of attachments associated with the comment,
    /// where the key is the attachment ID and the value is the attachment file name.
    /// </summary>
    public Dictionary<Guid, string> Attachments { get; set; }
}