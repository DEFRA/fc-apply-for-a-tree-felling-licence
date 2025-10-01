using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;

/// <summary>
/// View model for reviewing amendments to felling and restocking details in a felling licence application.
/// </summary>
public class ReviewAmendmentsViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the application.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the amended felling and restocking details for review.
    /// </summary>
    public required AmendedFellingRestockingDetailsModel AmendedFellingAndRestockingDetails { get; set; }

    /// <summary>
    /// Gets or sets the date when amendments were sent to the applicant.
    /// </summary>
    public required DateTime AmendmentsSentDate { get; set; }

    /// <summary>
    /// Gets or sets the date when a response was received from the applicant.
    /// </summary>
    /// <remarks>
    /// If this is null, it indicates that a response has not yet been received.
    /// </remarks>
    public DateTime? ResponseReceivedDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the applicant agreed to the amendments.
    /// </summary>
    [Required]
    public bool? ApplicantAgreed { get; set; }

    /// <summary>
    /// Gets or sets the reason provided by the applicant for disagreement, if any.
    /// </summary>
    /// <remarks>
    /// This should be null if the applicant agreed to the amendments or has not yet responded.
    /// </remarks>
    public string? ApplicantDisagreementReason { get; set; }

    /// <summary>
    /// Gets or sets the deadline by which the review must be completed.
    /// </summary>
    public required DateTime ReviewDeadline { get; set; }

    public bool IsEditable => ResponseReceivedDate.HasNoValue();
}