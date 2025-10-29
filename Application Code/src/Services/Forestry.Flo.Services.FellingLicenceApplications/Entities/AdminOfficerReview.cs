using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Admin Officer Review entity class.
/// </summary>
public class AdminOfficerReview
{
    /// <summary>
    /// Gets and sets the Id of this entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the felling licence application id.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    public FellingLicenceApplication FellingLicenceApplication { get; set; }

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
    /// Gets and sets whether the Admin Officer has confirmed that the agent authority has been checked.
    /// </summary>
    public bool? AgentAuthorityFormChecked { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the agent authority form has passed its check.
    /// </summary>
    public bool? AgentAuthorityCheckPassed { get; set; }

    /// <summary>
    /// Gets and sets a textual description of why the mapping check has failed.
    /// </summary>
    public string? MappingCheckFailureReason { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the mapping has passed its check.
    /// </summary>
    public bool? MappingCheckPassed { get; set; }

    /// <summary>
    /// Gets and sets a textual description of why the agent authority check has failed.
    /// </summary>
    public string? AgentAuthorityCheckFailureReason { get; set; }

    /// <summary>
    /// Gets and sets whether the Admin Officer has confirmed that the mapping has been checked.
    /// </summary>
    public bool? MappingChecked { get; set; }

    /// <summary>
    /// Gets and sets whether the Admin Officer has confirmed that the constraints have been checked.
    /// </summary>
    public bool? ConstraintsChecked { get; set; }

    /// <summary>
    /// Gets and sets whether the Larch details have been checked.
    /// </summary>
    public bool? LarchChecked { get; set; }

    /// <summary>
    /// Gets and sets whether the CBW have been checked.
    /// </summary>
    public bool? CBWChecked { get; set; }

    /// <summary>
    /// Gets and sets whether the Admin Officer has confirmed that the EIA details have been checked.
    /// </summary>
    public bool? EiaChecked { get; set; }

    /// <summary>
    /// Gets and sets whether the Admin Officer has completed the review.
    /// </summary>
    public bool AdminOfficerReviewComplete { get; set; }
}