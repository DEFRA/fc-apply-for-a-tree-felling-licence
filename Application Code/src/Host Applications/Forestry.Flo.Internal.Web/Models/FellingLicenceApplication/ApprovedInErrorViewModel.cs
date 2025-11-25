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
    /// </summary>
    [HiddenInput]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the internal user viewing the record.
    /// </summary>
    public InternalUser? ViewingUser { get; set; }

    /// <summary>
    /// Gets or sets the application ID that this record belongs to.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the application's old reference number prior to the Approved In Error process.
    /// </summary>
    public string PreviousReference { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a flag indicating the expiry date was a reason for the approval in error.
    /// </summary>
    public bool ReasonExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating supplementary points were a reason for the approval in error.
    /// </summary>
    public bool ReasonSupplementaryPoints { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating another reason (not covered by other flags) applied.
    /// </summary>
    public bool ReasonOther { get; set; }

    /// <summary>
    /// Gets or sets optional case notes providing additional context for the approval in error.
    /// </summary>
    public string? CaseNote { get; set; }
}