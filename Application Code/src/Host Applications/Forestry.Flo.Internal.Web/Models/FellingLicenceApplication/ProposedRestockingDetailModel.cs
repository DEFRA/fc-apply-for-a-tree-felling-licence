using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ProposedRestockingDetailModel
{

    public ProposedRestockingDetailModel()
    {
        Species = new Dictionary<string, RestockingSpeciesModel>();
    }
    
    /// <summary>
    /// Gets and Sets the property document ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the restocking proposal.
    /// </summary>
    public TypeOfProposal RestockingProposal { get; set; }

    /// <summary>
    /// Gets or sets the area.
    /// </summary>
    public double? Area { get; set; }

    /// <summary>
    /// Gets and sets the percentage of open space.
    /// </summary>
    public double? PercentOpenSpace { get; set; }

    /// <summary>
    /// Gets or sets the restock area as a percentage of compartment size.
    /// </summary>
    public double? PercentageOfRestockArea { get; set; }

    /// <summary>
    /// Gets and sets the restock area as a percentage of felling operation area.
    /// </summary>
    public double? PercentageOfFellingArea { get; set; }

    /// <summary>
    /// Gets or sets the restocking density.
    /// </summary>
    public double? RestockingDensity { get; set; }

    /// <summary>
    /// Gets or sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets the percentage established by coppice regrowth or natural regeneration.
    /// </summary>
    /// <remarks>
    /// Only applicable if the restocking proposal is either coppice regrowth or natural regeneration.
    /// This field should be null for other restocking proposals.
    /// </remarks>
    public double? PercentageEstablishedByCoppiceOrNaturalRegen { get; set; }

    /// <summary>
    /// Gets or sets the restocking species.
    /// </summary>
    public Dictionary<string,RestockingSpeciesModel> Species { get; set; } 

    // TODO: Step complete applies at the compartment level

    public bool? StepComplete { get; set; }

    public Guid RestockingCompartmentId { get; set; }

    public string? RestockingCompartmentName { get; set; }

    public string? GISData { get; set; }
}