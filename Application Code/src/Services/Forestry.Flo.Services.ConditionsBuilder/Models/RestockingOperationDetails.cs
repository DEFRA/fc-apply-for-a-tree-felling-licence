namespace Forestry.Flo.Services.ConditionsBuilder.Models;

/// <summary>
/// Model class representing the details of a specific restocking operation
/// required for the conditions builder calculations.
/// </summary>
public class RestockingOperationDetails
{
    /// <summary>
    /// Gets and sets the compartment number where the felling is taking place.
    /// </summary>
    public string FellingCompartmentNumber { get; set; }

    /// <summary>
    /// Gets and sets the subcompartment name where the felling is taking place.
    /// </summary>
    public string FellingSubcompartmentName { get; set; }

    /// <summary>
    /// Gets and sets the felling operation type for this compartment.
    /// </summary>
    public FellingOperationType FellingOperationType { get; set; }

    /// <summary>
    /// Gets and sets the id of the submitted FLA property compartment that the restocking is in.
    /// </summary>
    public Guid RestockingSubmittedFlaPropertyCompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the compartment number where the restocking is taking place.
    /// </summary>
    public string RestockingCompartmentNumber { get; set; }

    /// <summary>
    /// Gets and sets the subcompartment name where the restocking is taking place.
    /// </summary>
    public string RestockingSubcompartmentName { get; set; }

    /// <summary>
    /// Gets and sets the type of restocking for this compartment (which may be physically located elsewhere).
    /// </summary>
    public RestockingProposalType RestockingProposalType { get; set; }

    /// <summary>
    /// Gets and sets a list of <see cref="Models.RestockingSpecies"/> detailing the species and the percentages
    /// of the restock area for each.
    /// </summary>
    public List<RestockingSpecies> RestockingSpecies { get; set; }

    /// <summary>
    /// Gets and sets the total area in hectares being restocked.
    /// </summary>
    public double TotalRestockingArea { get; set; }

    /// <summary>
    /// Gets and sets the percent of the restocking compartment being left to open space.
    /// </summary>
    public int PercentOpenSpace { get; set; }

    /// <summary>
    /// Gets and sets the density of the restocking.
    /// </summary>
    public double? RestockingDensity { get; set; }

    /// <summary>
    /// Gets or sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets and sets the percent of restocking left to natural regeneration.
    /// </summary>
    public int PercentNaturalRegeneration { get; set; }
}