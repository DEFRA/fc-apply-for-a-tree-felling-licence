namespace Forestry.Flo.Services.ConditionsBuilder.Models;

/// <summary>
/// Response class representing the output of the conditions builder.
/// </summary>
public class ConditionsResponse
{
    /// <summary>
    /// Gets and sets the list of conditions as calculated by the conditions builder engine.
    /// </summary>
    public List<CalculatedCondition> Conditions { get; set; }
}