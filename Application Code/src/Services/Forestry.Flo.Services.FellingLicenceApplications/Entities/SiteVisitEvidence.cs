using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Site visit evidence entity class.
/// </summary>
public class SiteVisitEvidence
{
    /// <summary>
    /// Gets and sets the Id of this entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the woodland officer review id.
    /// </summary>
    [Required]
    public Guid WoodlandOfficerReviewId { get; set; }

    [Required]
    public WoodlandOfficerReview WoodlandOfficerReview { get; set; }

    /// <summary>
    /// Gets and sets the supporting document id that this evidence metadata relates to.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Gets and sets the date and time this was last updated.
    /// </summary>
    [Required]
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets and sets the id of the user that last updated this.
    /// </summary>
    [Required]
    public Guid LastUpdatedById { get; set; }

    /// <summary>
    /// Gets and sets the label for the evidence, e.g. "Photo of fallen tree".
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets and sets any comment for the evidence.
    /// </summary>
    public string? Comment { get; set; }
}