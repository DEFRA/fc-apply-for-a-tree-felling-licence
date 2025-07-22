using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;

/// <summary>
/// Legacy Document entity class
/// </summary>
public class LegacyDocument
{
    /// <summary>
    /// Gets and sets the entity id.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets and sets the ID of the <see cref="WoodlandOwner"/> that owns this document.
    /// </summary>
    /// <remarks>
    /// This is intentionally not set up as a foreign key (and there is no list of <see cref="LegacyDocument"/>
    /// property on <see cref="WoodlandOwner"/>) as the migration process is the only time this table will ever
    /// have data added to it - this data is to be considered read-only within the FLOv2 application.
    /// </remarks>
    [Required]
    public Guid WoodlandOwnerId { get; protected set; }

    /// <summary>
    /// Gets and sets the <see cref="LegacyDocumentType"/> for this legacy document.
    /// </summary>
    [Required]
    public LegacyDocumentType DocumentType { get; protected set; }

    /// <summary>
    /// Gets and sets the name of the file.
    /// </summary>
    [Required]
    public string FileName { get; protected set; }

    /// <summary>
    /// Gets and sets the size of the file.
    /// </summary>
    [Required]
    public long FileSize { get; protected set; }

    /// <summary>
    /// Gets and sets the type of the file.
    /// </summary>
    [Required]
    public string FileType { get; protected set; }

    /// <summary>
    /// Gets and sets the content type of the document.
    /// </summary>
    [Required]
    public string MimeType { get; protected set; }

    /// <summary>
    /// Gets and sets the location of this document.
    /// </summary>
    [Required]
    public string Location { get; protected set; }
}