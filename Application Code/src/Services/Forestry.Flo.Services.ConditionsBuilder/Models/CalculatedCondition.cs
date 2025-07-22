using System.Text;
using Forestry.Flo.Services.ConditionsBuilder.Entities;

namespace Forestry.Flo.Services.ConditionsBuilder.Models;

/// <summary>
/// Model class representing a condition that has been calculated by the conditions
/// builder engine.
/// </summary>
public class CalculatedCondition
{
    /// <summary>
    /// Gets and sets the collection of one or more submitted compartment ids that this condition relates to.
    /// </summary>
    public List<Guid> AppliesToSubmittedCompartmentIds { get; set; }

    /// <summary>
    /// Gets and sets the text representation of the condition, as an array of string lines.
    /// </summary>
    /// <remarks>This will include placeholder strings in format {0}, {1} etc which should be replaced with the values
    /// in the <see cref="Parameters"/> property when displayed to the user, printed etc.</remarks>
    public string[] ConditionsText { get; set; }

    /// <summary>
    /// Gets and sets the list of parameter values for substituting into the conditions text.
    /// </summary>
    public List<ConditionParameter> Parameters { get; set; }


    /// <summary>
    /// Returns a string containing all the lines of <see cref="ConditionsText"/>, with the parameter values of
    /// <see cref="Parameters"/> inserted in place of the placeholder strings.
    /// </summary>
    /// <returns>A formatted string array representing the condition with parameter values included.</returns>
    public string[] ToFormattedArray()
    {
        var lines = new List<string>(ConditionsText.Length);
        foreach (var line in ConditionsText)
        {
            var nextLine = line ?? string.Empty;
            foreach (var conditionParameter in Parameters)
            {
                nextLine = nextLine.Replace("{" + conditionParameter.Index.ToString() + "}", conditionParameter.Value ?? string.Empty);
            }

            lines.Add(nextLine);
        }

        return lines.ToArray();
    }
}