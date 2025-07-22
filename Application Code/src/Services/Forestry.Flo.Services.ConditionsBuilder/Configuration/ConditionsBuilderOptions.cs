namespace Forestry.Flo.Services.ConditionsBuilder.Configuration;

/// <summary>
/// Configuration options for the calculated conditions.
/// </summary>

public class ConditionsBuilderOptions
{
    /// <summary>
    /// Gets and sets the conditions options for condition Replanting.
    /// </summary>
    public ConditionOptions ReplantingOptions { get; set; }

    /// <summary>
    /// Gets and sets the conditions options for condition Natural Regeneration.
    /// </summary>
    public ConditionOptions NaturalRegenOptions { get; set; }

    /// <summary>
    /// Gets and sets the conditions options for condition Coppice Regrowth.
    /// </summary>
    public ConditionOptions CoppiceRegrowthOptions { get; set; }
}