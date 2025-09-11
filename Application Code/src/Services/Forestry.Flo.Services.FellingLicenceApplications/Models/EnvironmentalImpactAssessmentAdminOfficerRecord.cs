using CSharpFunctionalExtensions;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record EnvironmentalImpactAssessmentAdminOfficerRecord
{
    /// <summary>
    /// Gets or sets a value indicating whether the EIA form has been marked as received.
    /// </summary>
    public Maybe<bool> HasTheEiaFormBeenReceived { get; set; } = Maybe<bool>.None;

    /// <summary>
    /// Gets or sets a value indicating whether the attached forms have been marked as correct.
    /// </summary>
    public Maybe<bool> AreAttachedFormsCorrect { get; set; } = Maybe<bool>.None;

    /// <summary>
    /// Gets and sets the EIA tracker reference number.
    /// </summary>
    /// <remarks>
    /// This field is required if either HasTheEiaFormBeenReceived or AreAttachedFormsCorrect are true.
    /// </remarks>
    public Maybe<string> EiaTrackerReferenceNumber { get; set; } = Maybe<string>.None;

    /// <summary>
    /// Determines if the record is valid.
    /// </summary>
    public bool IsValid => HasMutualExclusivity && HasRequiredReferenceNumber;

    /// <summary>
    /// Exactly one of EIA tracker reference number or attached forms correctness must be set.
    /// </summary>
    private bool HasMutualExclusivity =>
        HasTheEiaFormBeenReceived.HasValue ^ AreAttachedFormsCorrect.HasValue;

    /// <summary>
    /// Gets and sets the EIA tracker reference number.
    /// </summary>
    /// <summary>
    /// Ensures that if either HasTheEiaFormBeenReceived or AreAttachedFormsCorrect are true,
    /// then EiaTrackerReferenceNumber must also have a value.
    /// </summary>
    private bool HasRequiredReferenceNumber =>
        (HasTheEiaFormBeenReceived is not { HasValue: true, Value: true } && AreAttachedFormsCorrect is not { HasValue: true, Value: true }) 
        || EiaTrackerReferenceNumber.HasValue;
}