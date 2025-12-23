using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Represents the Approved In Error record for a felling licence application.
/// This entity captures the details when an approved application needs to be corrected or reversed
/// due to errors identified after the initial approval.
/// </summary>
/// <remarks>
/// When a felling licence is marked as "Approved in Error", an additional record is created
/// to allow corrections to be made. The original application reference is preserved in this record,
/// and the reasons for the error are documented to maintain an audit trail.
/// </remarks>
public class ApprovedInError
{
    /// <summary>
    /// Gets and sets the unique identifier of this Approved In Error entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the felling licence application id that this record is associated with.
    /// This is the ID of the duplicated/corrected application, not the original application.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the navigation property to the associated felling licence application.
    /// </summary>
    public FellingLicenceApplication FellingLicenceApplication { get; set; }

    /// <summary>
    /// Gets and sets the date and time this record was last updated.
    /// </summary>
    [Required]
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets and sets the id of the user that last updated this record.
    /// Typically this will be an Account Administrator who is authorized to mark applications as approved in error.
    /// </summary>
    [Required]
    public Guid LastUpdatedById { get; set; }

    /// <summary>
    /// Gets and sets the old reference number of the application before the approved-in-error process.
    /// This preserves the original application reference for audit and traceability purposes.
    /// </summary>
    /// <example>FLO/2024/123</example>
    public string PreviousReference { get; set; } = string.Empty;

    /// <summary>
    /// Gets and sets a flag indicating whether the expiry date was a reason for marking this application as approved in error.
    /// When true, indicates that the licence expiry date in the approved licence was incorrect.
    /// </summary>
    public bool ReasonExpiryDate { get; set; }

    /// <summary>
    /// Gets and sets the textual explanation for why the expiry date was incorrect.
    /// This provides additional context when <see cref="ReasonExpiryDate"/> is true.
    /// </summary>
    public string? ReasonExpiryDateText { get; set; }

    /// <summary>
    /// Gets and sets the corrected licence expiry date that should be used when re-approving the application.
    /// This is populated when <see cref="ReasonExpiryDate"/> is true and represents the correct expiry date.
    /// </summary>
    public DateTime? LicenceExpiryDate { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether supplementary points were a reason for marking this application as approved in error.
    /// When true, indicates that the supplementary points (Parameter 6) in the conditions were incorrect or incomplete.
    /// </summary>
    public bool ReasonSupplementaryPoints { get; set; }

    /// <summary>
    /// Gets and sets the textual explanation or corrected content for the supplementary points.
    /// This provides the corrected supplementary points text when <see cref="ReasonSupplementaryPoints"/> is true.
    /// Corresponds to Parameter 6 in the conditions builder system.
    /// </summary>
    public string? SupplementaryPointsText { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether other reasons exist for marking this application as approved in error.
    /// When true, indicates that there are additional reasons not covered by the expiry date or supplementary points flags.
    /// </summary>
    public bool ReasonOther { get; set; }

    /// <summary>
    /// Gets and sets the textual explanation for other reasons the application was marked as approved in error.
    /// This is required when <see cref="ReasonOther"/> is true and provides details of issues not covered
    /// by the specific reason flags.
    /// </summary>
    public string? ReasonOtherText { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier of the Field Manager (approver) who last approved the application
    /// after it was marked as approved in error.
    /// This is used to identify and notify the approver who made the decision that is being corrected.
    /// The approver will be notified when the application is marked as approved in error so they are aware
    /// of the issue and can review what went wrong.
    /// </summary>
    public Guid? ApproverId { get; set; }
}