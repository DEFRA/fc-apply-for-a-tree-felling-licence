using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// FellingAndRestockingAmendmentReview entity class
/// </summary>
/// <remarks>
/// This represents a single amendment review cycle where amendments have been sent to the applicant.
/// Any further amendments required after the applicant has responded will create a new instance of this entity.
/// </remarks>
public class FellingAndRestockingAmendmentReview
{
    /// <summary>
    /// Gets and Sets the entity ID.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or sets the woodland officer review ID associated with this amendment review.
    /// </summary>
    [Required]
    public Guid WoodlandOfficerReviewId { get; set; }

    /// <summary>
    /// Gets or sets the woodland officer review associated with this amendment review.
    /// </summary>
    public WoodlandOfficerReview? WoodlandOfficerReview { get; set; }

    /// <summary>
    /// Gets or sets the date when amendments were sent to the applicant.
    /// </summary>
    public DateTime AmendmentsSentDate { get; set; }

    /// <summary>
    /// Gets or sets the deadline by which the applicant must respond to the amendments,
    /// else the application will be automatically withdrawn.
    /// </summary>
    public DateTime ResponseDeadline { get; set; }

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
    public bool? ApplicantAgreed { get; set; }

    /// <summary>
    /// Gets or sets the reason provided by the applicant for disagreement, if any.
    /// </summary>
    /// <remarks>
    /// This should be null if the applicant agreed to the amendments or has not yet responded.
    /// </remarks>
    public string? ApplicantDisagreementReason { get; set; }
}