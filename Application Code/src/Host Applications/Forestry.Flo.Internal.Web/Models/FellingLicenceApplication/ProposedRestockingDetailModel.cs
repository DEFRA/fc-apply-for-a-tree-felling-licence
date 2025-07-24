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
    /// Gets or sets the percentage of restock area.
    /// </summary>
    public int? PercentageOfRestockArea { get; set; }

    /// <summary>
    /// Gets or sets the restocking density.
    /// </summary>
    public double RestockingDensity { get; set; }

    /// <summary>
    /// Gets or sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets the restocking species.
    /// </summary>
    public Dictionary<string,RestockingSpeciesModel> Species { get; set; } 

    // TODO: Step complete applies at the compartment level

    public bool? StepComplete { get; set; }
}