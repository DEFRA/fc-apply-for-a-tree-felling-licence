using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Woodland Officer Review entity class.
/// </summary>
public class WoodlandOfficerReview
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
    /// Gets and sets whether the Woodland Officer has confirmed that the land information search has been checked.
    /// </summary>
    public bool? LandInformationSearchChecked { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has confirmed that the proposals are UKFS compliant.
    /// </summary>
    public bool? AreProposalsUkfsCompliant { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has confirmed that a TPO or CA has been declared.
    /// </summary>
    public bool? TpoOrCaDeclared { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has confirmed that the application is valid.
    /// </summary>
    public bool? IsApplicationValid { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has confirmed that the EIA threshold is exceeded.
    /// </summary>
    public bool? EiaThresholdExceeded { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has confirmed that the EIA tracker has been completed.
    /// </summary>
    /// <remarks>
    /// This should be set if the <see cref="EiaThresholdExceeded"/> flag is set to true.
    /// </remarks>
    public bool? EiaTrackerCompleted { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has confirmed that the EIA checklist has been done.
    /// </summary>
    /// <remarks>
    /// This should be set if the <see cref="EiaThresholdExceeded"/> flag is set to true.
    /// </remarks>
    public bool? EiaChecklistDone { get; set; }

    /// <summary>
    /// Gets and sets whether the local authority been consulted or advice sought, if needed.
    /// </summary>
    /// <remarks>
    /// This should be set if the <see cref="TpoOrCaDeclared"/> flag is set to true.
    /// </remarks>
    public bool? LocalAuthorityConsulted { get; set; }

    /// <summary>
    /// Gets and sets whether anyone processing this application needs to declare an interest.
    /// </summary>
    public bool? InterestDeclared { get; set; }

    /// <summary>
    /// Gets and sets whether the declaration of interest form been filled in and sent to the FS Compliance Team.
    /// </summary>
    /// <remarks>
    /// This should be set if the <see cref="InterestDeclared"/> flag is set to true.
    /// </remarks>
    public bool? InterestDeclarationCompleted { get; set; }

    /// <summary>
    /// Gets and sets whether the FS Compliance Team's recommendations have been put in place.
    /// </summary>
    /// <remarks>
    /// This should be set if the <see cref="InterestDeclared"/> flag is set to true.
    /// </remarks>
    public bool? ComplianceRecommendationsEnacted { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has confirmed that the map accuracy has been checked.
    /// </summary>
    public bool? MapAccuracyConfirmed { get; set; }

    /// <summary>
    /// Gets and sets whether the need for an EPS licence been considered and discussed with the applicant.
    /// </summary>
    public bool? EpsLicenceConsidered { get; set; }

    /// <summary>
    /// Gets and sets whether a Stage 1 Habitat Regulations Assessment is needed.
    /// </summary>
    public bool? Stage1HabitatRegulationsAssessmentRequired { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has confirmed that all the PW14 checks have been completed.
    /// </summary>
    public bool Pw14ChecksComplete { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has indicated that the application is conditional.
    /// </summary>
    public bool? IsConditional { get; set; }

    /// <summary>
    /// Gets and sets when the conditions were sent to the applicant, or null if they have not.
    /// </summary>
    public DateTime? ConditionsToApplicantDate { get; set; }

    /// <summary>
    /// Gets and sets whether the woodland officer has completed entering the confirmed felling and restocking details.
    /// </summary>
    public bool ConfirmedFellingAndRestockingComplete { get; set; }

    /// <summary>
    /// Gets and sets whether the Larch details have been checked.
    /// </summary>
    public bool? LarchCheckComplete { get; set; }

    /// <summary>
    /// Gets and sets whether the EIA screening has been completed.
    /// </summary>
    /// <remarks>
    /// This corresponds to the EIA task on the WO review task list.
    /// </remarks>
    public bool? EiaScreeningComplete { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has completed the review.
    /// </summary>
    public bool WoodlandOfficerReviewComplete { get; set; }

    /// <summary>
    /// Gets and sets the licence duration recommended by the Woodland Officer.
    /// </summary>
    public RecommendedLicenceDuration? RecommendedLicenceDuration { get; set; }

    /// <summary>
    /// Gets and sets a bool to indicate whether the Woodland Officer has recommended that the application
    /// is published to the decision public register.
    /// </summary>
    public bool? RecommendationForDecisionPublicRegister { get; set; }

    /// <summary>
    /// Gets and sets the reason provided by the Woodland Officer for their recommendation
    /// that the application is or is not published to the decision public register.
    /// </summary>
    public string? RecommendationForDecisionPublicRegisterReason { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has indicated that a site visit is not needed.
    /// </summary>
    public bool? SiteVisitNeeded { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating that the site visit arrangements have been made.
    /// </summary>
    public bool? SiteVisitArrangementsMade { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating that the site visit has been completed and resulting notes/attachments have been uploaded.
    /// </summary>
    public bool SiteVisitComplete { get; set; }
    
    /// <summary>
    /// Gets or sets the site visit evidence metadata.
    /// </summary>
    public IList<SiteVisitEvidence>? SiteVisitEvidences { get; set; } = new List<SiteVisitEvidence>();

    /// <summary>
    /// Gets and sets whether any consultations are required for this application.
    /// </summary>
    public bool? ApplicationNeedsConsultations { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has completed the consultations stage of the review.
    /// </summary>
    public bool ConsultationsComplete { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has completed the designations stage of the review.
    /// </summary>
    public bool DesignationsComplete { get; set; }

    /// Gets and sets the felling and restocking amendment reviews associated with this WO review.
    /// </summary>
    public IList<FellingAndRestockingAmendmentReview> FellingAndRestockingAmendmentReviews { get; set; } = new List<FellingAndRestockingAmendmentReview>();
}