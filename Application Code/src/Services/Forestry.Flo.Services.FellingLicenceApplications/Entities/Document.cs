using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Document entity class
/// </summary>
public class Document
{
    /// <summary>
    /// Gets and Sets the property document ID.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    [Required]
    public Guid FellingLicenceApplicationId { get; set; }

    [Required]
    public FellingLicenceApplication FellingLicenceApplication { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp date / time.
    /// </summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the purpose.
    /// </summary>
    [Required]
    public DocumentPurpose Purpose { get; set; }

    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    [Required]
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the size of the file.
    /// </summary>
    [Required]
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the type of the file.
    /// </summary>
    [Required]
    public string FileType { get; set; }

    /// <summary>
    /// The content type of the document.
    /// </summary>
    [Required]
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets and sets the type of actor that attached the document.
    /// </summary>
    [Required]
    public ActorType AttachedByType { get; set; }
    
    /// <summary>
    /// Gets or sets the Id of the user that attached the document.
    /// </summary>
    /// <remarks>This Id may be for an external applicant, an internal user, or null
    /// based on the value of <see cref="AttachedByType"/>.</remarks>
    public Guid? AttachedById { get; set; }

    /// <summary>
    /// The location of this document.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating the document is visible to external applicants.
    /// </summary>
    public bool VisibleToApplicant { get; set; } = true;

    /// <summary>
    /// Gets or sets a flag indicating the document is visible to consultees.
    /// </summary>
    public bool VisibleToConsultee { get; set; } = true;

    /// <summary>
    /// Gets or sets the Id of the user that deleted the document.
    /// </summary>
    public Guid? DeletedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the deletion timestamp date / time.
    /// </summary>
    public DateTime? DeletionTimestamp { get; set; }
}