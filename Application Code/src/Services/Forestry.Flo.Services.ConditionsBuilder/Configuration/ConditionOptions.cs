using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.ConditionsBuilder.Configuration;

public class ConditionOptions
{
    public const string SpeciesParameter = "{SPECIES}";
    public const string DensityParameter = "{DENSITY}";
    public const string CompartmentsParameter = "{COMPARTMENTS}";
    public const string FellingCompartmentsParameter = "{FELLINGCOMPARTMENTS}";
    public const string RegenerationParameter = "{REGEN}";

    /// <summary>
    /// Gets and sets the lines of text that make up the particular condition output.
    /// </summary>
    [Required]
    public string[] ConditionText { get; set; }

    /// <summary>
    /// Gets and sets configuration for the parameters within the condition text.
    /// </summary>
    [Required]
    public ConditionParameterOptions[] ConditionParameters { get; set; }
}

/// <summary>
/// Configuration class for the parameter values within the conditions text.
/// </summary>
public class ConditionParameterOptions
{
    /// <summary>
    /// Gets and sets the index of the condition parameter.
    /// </summary>
    [Required]
    public int Index { get; set; }

    /// <summary>
    /// Gets and sets the optional default value for the condition parameter.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets and sets any descriptive help text to explain to the user the intent for this parameter.
    /// </summary>
    public string? Description { get; set; }
}