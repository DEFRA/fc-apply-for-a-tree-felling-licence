using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Environmental Impact Assessment entity class
/// </summary>
public class EnvironmentalImpactAssessment
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

    /// <summary>
    /// Gets and sets the felling licence application.
    /// </summary>
    public FellingLicenceApplication? FellingLicenceApplication { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the environmental impact assessment application has been completed.
    /// </summary>
    public bool? HasApplicationBeenCompleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the environmental impact assessment application has been sent.
    /// </summary>
    public bool? HasApplicationBeenSent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the EIA form has been marked as received.
    /// </summary>
    public bool? HasTheEiaFormBeenReceived { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the attached forms have been marked as correct.
    /// </summary>
    public bool? AreAttachedFormsCorrect { get; set; }

    /// <summary>
    /// Gets and sets the EIA tracker reference number.
    /// </summary>
    public string? EiaTrackerReferenceNumber { get; set; }

    /// <summary>
    /// A collection of EIA requests associated with a particular Environmental Impact Assessment.
    /// </summary>
    public IList<EnvironmentalImpactAssessmentRequestHistory> EiaRequests { get; set; } = [];
}