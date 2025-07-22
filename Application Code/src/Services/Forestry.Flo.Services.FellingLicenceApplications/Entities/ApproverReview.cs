using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Woodland Officer Review entity class.
/// </summary>
public class ApproverReview
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

    public FellingLicenceStatus RequestedStatus { get; set; } = FellingLicenceStatus.SentForApproval;

    public bool CheckedApplication { get; set; }

    public bool CheckedDocumentation { get; set; }

    public bool CheckedCaseNotes { get; set; }

    public bool CheckedWOReview { get; set; }

    public bool InformedApplicant { get; set; }

    public RecommendedLicenceDuration? ApprovedLicenceDuration { get; set; }

    public string? DurationChangeReason { get; set; }

    public bool? PublicRegisterPublish { get; set; }

    public string? PublicRegisterExemptionReason { get; set; }

}