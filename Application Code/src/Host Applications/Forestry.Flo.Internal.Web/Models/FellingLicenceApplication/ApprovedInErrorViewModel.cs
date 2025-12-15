using Forestry.Flo.Internal.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

/// <summary>
/// View model for the Approved In Error process for a felling licence application.
/// Used to capture and display details when an application is marked as approved in error.
/// </summary>
public class ApprovedInErrorViewModel : FellingLicenceApplicationPageViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the Approved In Error record.
    /// This property is marked as a hidden input for form submission.
    /// </summary>
    [HiddenInput]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the internal user viewing the record.
    /// This identifies the user currently viewing or working with the approved-in-error process.
    /// </summary>
    public InternalUser? ViewingUser { get; set; }

    /// <summary>
    /// Gets or sets the application ID that this record belongs to.
    /// This is the ID of the duplicated/corrected application, not the original application.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the application's old reference number prior to the Approved In Error process.
    /// This preserves the original application reference for audit and traceability purposes.
    /// </summary>
    /// <example>FLO/2024/123</example>
    public string PreviousReference { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a flag indicating whether the expiry date was a reason for marking this application as approved in error.
    /// When true, indicates that the licence expiry date in the approved licence was incorrect.
    /// </summary>
    public bool ReasonExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the textual explanation for why the expiry date was incorrect.
    /// This provides additional context when <see cref="ReasonExpiryDate"/> is true.
    /// </summary>
    public string? ReasonExpiryDateText { get; set; }

    /// <summary>
    /// Gets and sets the corrected licence expiry date that should be used when re-approving the application.
    /// This is populated when <see cref="ReasonExpiryDate"/> is true and represents the correct expiry date.
    /// </summary>
    public DateTime? LicenceExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether supplementary points were a reason for marking this application as approved in error.
    /// When true, indicates that the supplementary points (Parameter 6) in the conditions were incorrect or incomplete.
    /// </summary>
    public bool ReasonSupplementaryPoints { get; set; }

    /// <summary>
    /// Gets or sets the textual explanation or corrected content for the supplementary points.
    /// This provides the corrected supplementary points text when <see cref="ReasonSupplementaryPoints"/> is true.
    /// Corresponds to Parameter 6 in the conditions builder system.
    /// </summary>
    public string? SupplementaryPointsText { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether other reasons exist for marking this application as approved in error.
    /// When true, indicates that there are additional reasons not covered by the expiry date or supplementary points flags.
    /// </summary>
    public bool ReasonOther { get; set; }

    /// <summary>
    /// Gets or sets the textual explanation for other reasons the application was marked as approved in error.
    /// This is required when <see cref="ReasonOther"/> is true and provides details of issues not covered
    /// by the specific reason flags.
    /// </summary>
    public string? ReasonOtherText { get; set; }
}