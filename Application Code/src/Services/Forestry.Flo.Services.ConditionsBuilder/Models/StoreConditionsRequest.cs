namespace Forestry.Flo.Services.ConditionsBuilder.Models;

/// <summary>
/// Request class representing a set of conditions to be stored for a felling
/// licence application.
/// </summary>
/// <remarks>Any existing conditions for the felling licence application will be removed
/// before storing the conditions in the request.</remarks>
public class StoreConditionsRequest
{
    /// <summary>
    /// Gets and sets the id of the felling licence application that the conditions are for.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the list of conditions to be stored.
    /// </summary>
    public List<CalculatedCondition> Conditions { get; set; }
}