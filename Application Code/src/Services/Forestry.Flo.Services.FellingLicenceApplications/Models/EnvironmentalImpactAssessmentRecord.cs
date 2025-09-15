namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record EnvironmentalImpactAssessmentRecord
{
    /// <summary>
    /// Gets or sets a value indicating whether the environmental impact assessment application has been completed.
    /// </summary>
    public bool? HasApplicationBeenCompleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the environmental impact assessment application has been sent.
    /// </summary>
    public bool? HasApplicationBeenSent { get; set; }
}