namespace Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;

/// <summary>
/// Model class representing a comment made by an external consultee on a felling licence application.
/// </summary>
public record ConsulteeCommentModel
{
    /// <summary>
    /// The ID of the felling licence application this comment is associated with.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; init; }

    /// <summary>
    /// The name of the author of the comment.
    /// </summary>
    public string AuthorName { get; init; } = null!;

    /// <summary>
    /// The contact email of the author of the comment.
    /// </summary>
    public string AuthorContactEmail { get; init; } = null!;

    /// <summary>
    /// The content of the comment made by the external consultee.
    /// </summary>
    public string Comment { get; set; } = null!;

    /// <summary>
    /// The timestamp when the comment was created.
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// A collection of attachment IDs associated with the consultee comment.
    /// </summary>
    public IEnumerable<Guid> ConsulteeAttachmentIds { get; set; }
}