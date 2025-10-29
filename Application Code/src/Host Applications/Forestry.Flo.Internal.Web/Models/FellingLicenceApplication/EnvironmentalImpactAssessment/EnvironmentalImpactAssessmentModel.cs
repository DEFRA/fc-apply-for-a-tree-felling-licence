namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;

public class EnvironmentalImpactAssessmentModel
{
    /// <summary>
    /// Gets and sets the id of the environmental impact assessment entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the felling licence application id.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string? ApplicationReference { get; set; }

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
    public IList<EnvironmentalImpactAssessmentRequestModel> EiaRequests { get; set; } = [];

    /// <summary>
    /// A collection of documents related to the Environmental Impact Assessment (EIA).
    /// </summary>
    public IList<DocumentModel> EiaDocuments { get; set; } = [];
}