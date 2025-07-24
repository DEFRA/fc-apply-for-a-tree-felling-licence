using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

/// <summary>
/// Entity class representing a document file that is or is part of an AAF.
/// </summary>
public class AafDocument
{
    /// <summary>
    /// Gets the unique internal identifier for the AAF document file.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets and sets the Id of the <see cref="AgentAuthorityForm"/> this <see cref="AafDocument"/>
    /// is for.
    /// </summary>
    public Guid AgentAuthorityFormId { get; set; }

    /// <summary>
    /// Gets and sets the filename of the file.
    /// </summary>
    [Required]
    public string FileName { get; set; }

    /// <summary>
    /// Gets and sets the size of the file.
    /// </summary>
    [Required]
    public long FileSize { get; set; }

    /// <summary>
    /// Gets and sets the type of the file.
    /// </summary>
    [Required]
    public string FileType { get; set; }

    /// <summary>
    /// Gets and sets the content type of the document.
    /// </summary>
    [Required]
    public string MimeType { get; set; }

    /// <summary>
    /// Gets and sets the location of this document.
    /// </summary>
    public string Location { get; set; }
}