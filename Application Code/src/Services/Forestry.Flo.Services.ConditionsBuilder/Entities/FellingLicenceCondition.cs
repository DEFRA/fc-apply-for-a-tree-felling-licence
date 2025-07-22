using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.ConditionsBuilder.Entities;

/// <summary>
/// Entity class representing a felling licence condition within the system.
/// </summary>
public class FellingLicenceCondition
{
    /// <summary>
    /// Gets the unique internal identifier for the felling licence condition record on the system.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier for the felling licence this condition is linked to.
    /// </summary>
    [Required]
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the collection of one or more submitted compartment ids that this condition relates to.
    /// </summary>
    [Required]
    public List<Guid> AppliesToSubmittedCompartmentIds { get; set; }

    /// <summary>
    /// Gets and sets the text representation of the condition.
    /// </summary>
    /// <remarks>This will include placeholder strings in format {0}, {1} etc which should be replaced with the values
    /// in the <see cref="Parameters"/> property when displayed to the user, printed etc.</remarks>
    [Required]
    public List<string> ConditionsText { get; set; }

    /// <summary>
    /// Gets and sets the list of parameter values for substituting into the conditions text.
    /// </summary>
    [Required]
    public List<ConditionParameter> Parameters { get; set; }
}