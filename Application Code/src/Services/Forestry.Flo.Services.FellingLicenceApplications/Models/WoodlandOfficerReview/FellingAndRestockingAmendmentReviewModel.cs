namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the PW14 checks of the woodland officer review of an application.
/// </summary>
public class FellingAndRestockingAmendmentReviewModel
{    /// <summary>
    /// Gets and Sets the entity ID.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the felling licence application ID.
    /// </summary>
    public required Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the date when amendments were sent to the applicant.
    /// </summary>
    public required DateTime AmendmentsSentDate { get; set; }

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

/// <summary>
/// A record representing an update to the felling and restocking amendment review details.
/// </summary>
public record FellingAndRestockingAmendmentReviewUpdateRecord
{
    /// <summary>
    /// Gets or sets the felling licence application ID.
    /// </summary>
    public required Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the applicant agreed to the amendments.
    /// </summary>
    public required bool ApplicantAgreed { get; set; }

    /// <summary>
    /// Gets or sets the reason provided by the applicant for disagreement, if any.
    /// </summary>
    /// <remarks>
    /// This should be null if the applicant agreed to the amendments or has not yet responded.
    /// </remarks>
    public required string? ApplicantDisagreementReason { get; set; }
}