namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the PW14 checks of the woodland officer review of an application.
/// </summary>
public class Pw14ChecksModel
{
    /// <summary>
    /// Gets and sets a flag for the woodland officer confirming that the LIS search
    /// has been completed for the application.
    /// </summary>
    public bool? LandInformationSearchChecked { get; set; }

    /// <summary>
    /// Gets and sets a flag for the woodland officer confirming whether the proposals
    /// in the application are UKFS compliant.
    /// </summary>
    public bool? AreProposalsUkfsCompliant { get; set; }

    /// <summary>
    /// Gets and sets a flag for the woodland officer confirming whether a tree preservation order
    /// and/or conservation area has been declared for the areas covered by the application.
    /// </summary>
    public bool? TpoOrCaDeclared { get; set; }

    /// <summary>
    /// Gets and sets a flag for the woodland officer confirming whether or not the application
    /// is valid.
    /// </summary>
    public bool? IsApplicationValid { get; set; }

    /// <summary>
    /// Gets and sets a flag for the woodland officer confirming whether or not the EIA threshold
    /// is exceeded by the application.
    /// </summary>
    public bool? EiaThresholdExceeded { get; set; }

    /// <summary>
    /// Gets and sets a flag for the woodland officer to confirm when the EIA tracker has been
    /// completed for this application.
    /// </summary>
    /// <remarks>
    /// This should be set if the <see cref="EiaThresholdExceeded"/> flag is set to true.
    /// </remarks>
    public bool? EiaTrackerCompleted { get; set; }

    /// <summary>
    /// Gets and sets a flag for the woodland officer to confirm when the EIA checklist has been
    /// completed for this application.
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
    /// Gets and sets a flag for the woodland officer to confirm that the PW14 checks for the
    /// application have been completed.
    /// </summary>
    public bool Pw14ChecksComplete =>
        LandInformationSearchChecked.HasValue &&
        AreProposalsUkfsCompliant.HasValue &&
        IsApplicationValid.HasValue &&
        TpoOrCpaSectionComplete &&
        InterestDeclaredSectionComplete &&
        EiaSectionComplete &&
        MapAccuracyConfirmed.HasValue &&
        EpsLicenceConsidered.HasValue &&
        Stage1HabitatRegulationsAssessmentRequired.HasValue;

    /// <summary>
    /// Gets a flag indicating whether the interest declaration section is complete.
    /// </summary>
    private bool InterestDeclaredSectionComplete => 
        InterestDeclared is false || 
        (InterestDeclared is true && 
         InterestDeclarationCompleted.HasValue && 
         ComplianceRecommendationsEnacted.HasValue);

    /// <summary>
    /// Gets a flag indicating whether the TPO or CPA section is complete.
    /// </summary>
    private bool TpoOrCpaSectionComplete =>
        TpoOrCaDeclared is false ||
        (TpoOrCaDeclared is true && 
         LocalAuthorityConsulted.HasValue);

    /// <summary>
    /// Gets a flag indicating whether the EIA section is complete.
    /// </summary>
    private bool EiaSectionComplete =>
        EiaThresholdExceeded is false ||
        (EiaThresholdExceeded is true &&
         EiaTrackerCompleted.HasValue &&
         EiaChecklistDone.HasValue);
}