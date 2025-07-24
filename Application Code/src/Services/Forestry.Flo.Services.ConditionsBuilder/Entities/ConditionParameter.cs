using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.ConditionsBuilder.Entities;

/// <summary>
/// Class for a single condition parameter value on a <see cref="FellingLicenceCondition"/>.
/// </summary>
/// <remarks>The list of <see cref="ConditionParameter"/> entries for a <see cref="FellingLicenceCondition"/>
/// is serialized to JSON when stored in the repository so there is no need for an id on this class.</remarks>
public class ConditionParameter
{
    /// <summary>
    /// Gets and sets the index of this condition parameter within the conditions text.
    /// </summary>
    [Required]
    public int Index { get; set; }

    /// <summary>
    /// Gets and sets the string value to substitute into the conditions text.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets and sets any descriptive help text to explain to the user the intent for this parameter.
    /// </summary>
    public string? Description { get; set; }
}