namespace Forestry.Flo.Services.ConditionsBuilder.Models;

/// <summary>
/// Model class for a particular species being restocked.
/// </summary>
public class RestockingSpecies
{
    /// <summary>
    /// Gets and sets the coded identifier for the species.
    /// </summary>
    public string SpeciesCode { get; set; }

    /// <summary>
    /// Gets and sets the english common name of the species.
    /// </summary>
    public string SpeciesName { get; set; }

    /// <summary>
    /// Gets and sets the percentage of the restock area being restocked with this species.
    /// </summary>
    public double Percentage { get; set; }
}