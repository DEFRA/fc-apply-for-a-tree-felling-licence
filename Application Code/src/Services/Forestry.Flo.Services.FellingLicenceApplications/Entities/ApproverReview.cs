using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Approver data entry entity class.
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

    /// <summary>
    /// Gets and sets a navigation property to the felling licence application.
    /// </summary>
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
    /// Gets and sets the status the approver has selected for the felling licence application.
    /// </summary>
    public FellingLicenceStatus RequestedStatus { get; set; } = FellingLicenceStatus.SentForApproval;

    /// <summary>
    /// Gets and sets a value indicating whether the application has been checked.
    /// </summary>
    public bool CheckedApplication { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the documentation has been checked.
    /// </summary>
    public bool CheckedDocumentation { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the case notes have been checked.
    /// </summary>
    public bool CheckedCaseNotes { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the Woodland Officer review has been checked.
    /// </summary>
    public bool CheckedWOReview { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the applicant has been informed of the decision.
    /// </summary>
    public bool InformedApplicant { get; set; }

    /// <summary>
    /// Gets and sets the approved licence duration.
    /// </summary>
    public RecommendedLicenceDuration? ApprovedLicenceDuration { get; set; }

    /// <summary>
    /// Gets and sets the reason for changing the duration of the licence from that recommended by the Woodland Officer.
    /// </summary>
    public string? DurationChangeReason { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the application should be published in the public register.
    /// </summary>
    public bool? PublicRegisterPublish { get; set; }

    /// <summary>
    /// Gets and sets the reason for exemption from the public register, if applicable.
    /// </summary>
    public string? PublicRegisterExemptionReason { get; set; }

    /// <summary>
    /// Gets and sets the reason for referring the application to the local authority, if applicable.
    /// </summary>
    public string? ReferToLocalAuthorityReason { get; set; }

    /// <summary>
    /// Gets and sets the reason for refusing the application, if applicable.
    /// </summary>
    public string? ApplicationRefusedReason { get; set; }

}