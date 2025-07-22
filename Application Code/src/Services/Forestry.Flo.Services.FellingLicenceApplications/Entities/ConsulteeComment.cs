using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

public class ConsulteeComment
{
    /// <summary>
    /// Gets and sets the ConsulteeComment ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the ID of the Felling Licence Application this ConsulteeComment is for.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the point in time this comment was added.
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the name of the author of this consultee comment.
    /// </summary>
    public string AuthorName { get; set; }

    /// <summary>
    /// Gets and sets the email address of the author of this consultee comment.
    /// </summary>
    public string AuthorContactEmail { get; set; }

    /// <summary>
    /// Gets and sets the text of this consultee comment
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// Gets and sets the optional section of the felling licence application this comment applies to.
    /// </summary>
    public ApplicationSection? ApplicableToSection { get; set; }
}