using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Represents a single cycle of a felling and restocking amendment review initiated by a Woodland Officer.
/// </summary>
/// <remarks>
/// A new instance is created each time amendments are sent to the applicant during the Woodland Officer review.
/// If further amendments are needed after an applicant response, a new instance should be created to represent
/// the next review cycle.
/// </remarks>
public class FellingAndRestockingAmendmentReview
{
    public FellingAndRestockingAmendmentReview()
    {
    }

    public FellingAndRestockingAmendmentReview(bool? amendmentReviewCompleted)
    {
        AmendmentReviewCompleted = amendmentReviewCompleted;
    }

    /// <summary>
    /// Gets the unique identifier for this amendment review.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or sets the identifier of the parent <see cref="WoodlandOfficerReview"/> this amendment review belongs to.
    /// </summary>
    [Required]
    public Guid WoodlandOfficerReviewId { get; set; }

    /// <summary>
    /// Gets or sets the parent <see cref="WoodlandOfficerReview"/> navigation property.
    /// </summary>
    public WoodlandOfficerReview? WoodlandOfficerReview { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when amendments were sent to the applicant.
    /// </summary>
    /// <remarks>
    /// Use UTC throughout the system. This value is set at the point the Woodland Officer sends amendments.
    /// </remarks>
    public DateTime AmendmentsSentDate { get; set; }

    /// <summary>
    /// Gets or sets the reason provided by the Woodland Officer for requesting amendments.
    /// </summary>
    /// <remarks>
    /// This reason may be displayed to the applicant and shown in the activity feed.
    /// </remarks>
    public string? AmendmentsReason { get; set; }

    /// <summary>
    /// Gets or sets the UTC deadline by which the applicant must respond to the amendments.
    /// </summary>
    /// <remarks>
    /// If the deadline passes without a response, the application may be automatically withdrawn by a background process.
    /// </remarks>
    public DateTime ResponseDeadline { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when a response was received from the applicant, if any.
    /// </summary>
    /// <remarks>
    /// A null value indicates that a response has not yet been received.
    /// </remarks>
    public DateTime? ResponseReceivedDate { get; set; }

    /// <summary>
    /// Gets or sets whether the applicant agreed to the requested amendments.
    /// </summary>
    /// <remarks>
    /// True indicates agreement, false indicates disagreement, and null indicates no response yet.
    /// </remarks>
    public bool? ApplicantAgreed { get; set; }

    /// <summary>
    /// Gets or sets the reason provided by the applicant when disagreeing with the amendments.
    /// </summary>
    /// <remarks>
    /// Should be null if <see cref="ApplicantAgreed"/> is true or no response has been received yet.
    /// </remarks>
    public string? ApplicantDisagreementReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this amendment review has been completed.
    /// </summary>
    /// <remarks>
    /// This flag should be set to true when the review cycle is formally concluded (for example, once changes are reconciled and no further action is required).
    /// </remarks>
    public bool? AmendmentReviewCompleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when a reminder notification was sent to the applicant to respond to the amendments.
    /// </summary>
    /// <remarks>
    /// This may be used to determine if/when an automatic withdrawal reminder was issued.
    /// </remarks>
    public DateTime? ReminderNotificationTimeStamp { get; set; }

    /// <summary>
    /// Gets or sets the internal user identifier of the Woodland Officer who initiated these amendments.
    /// </summary>
    /// <remarks>
    /// Used for attribution in the activity feed and reporting.
    /// </remarks>
    public Guid? AmendingWoodlandOfficerId { get; set; }

    /// <summary>
    /// Gets or sets the external user identifier of the applicant who responded to these amendments, if any.
    /// </summary>
    /// <remarks>
    /// Populated when a response is received from an applicant user.
    /// </remarks>
    public Guid? RespondingApplicantId { get; set; }
}