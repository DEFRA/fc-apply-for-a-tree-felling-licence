using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// FellingLicenceApplication entity class
/// </summary>
public class CaseNote
{
    /// <summary>
    /// Gets and Sets the Case Note ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and Sets the Felling Licence Application Id.
    /// </summary>
    [Required]
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and Sets the Case Note type.
    /// </summary>
    [Required]
    public CaseNoteType Type { get; set; } = CaseNoteType.CaseNote;

    /// <summary>
    /// Gets and Sets the Case Note's contents.
    /// </summary>
    [Required]
    public string? Text { get; set; }

    /// <summary>
    /// Gets and Sets the visibility of a Case Note to the applicant.
    /// </summary>
    [Required]
    public bool VisibleToApplicant { get; set; }

    /// <summary>
    /// Gets and Sets the visibility of a Case Note to consultees.
    /// </summary>
    [Required]
    public bool VisibleToConsultee { get; set; }

    /// <summary>
    /// Gets and Sets the created time stamp.
    /// </summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Gets and Sets the User Id of the Case Note creator.
    /// </summary>
    [Required]
    public Guid CreatedByUserId { get; set; }
}