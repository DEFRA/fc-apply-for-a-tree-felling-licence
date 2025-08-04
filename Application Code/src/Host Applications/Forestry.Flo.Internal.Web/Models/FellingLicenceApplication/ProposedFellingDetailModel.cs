using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ProposedFellingDetailModel
{

    public ProposedFellingDetailModel()
    {
        Species = new Dictionary<string, FellingSpeciesModel>();
    }
    
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the operation.
    /// </summary>
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the area to be felled.
    /// </summary>
    public double AreaToBeFelled { get; set; }

    /// <summary>
    /// Gets or sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets the tree marking.
    /// </summary>
    public string? TreeMarking { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is part of tree preservation order.
    /// </summary>
    public bool IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets and Sets the Tree Preservation Order reference.
    /// </summary>
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is within conservation area.
    /// </summary>
    public bool IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets and Sets the conservation area reference.
    /// </summary>
    public string? ConservationAreaReference { get; set; }

    public Dictionary<string,FellingSpeciesModel> Species { get; set; } 

    // TODO: Step complete applies at the compartment level

    public bool? StepComplete { get; set; }


    public double EstimatedTotalFellingVolume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is restocking for this felling.
    /// </summary>
    public bool? IsRestocking { get; set; }

    /// <summary>
    /// Gets or sets the reason for not having restocking.
    /// </summary>
    public string? NoRestockingReason { get; set; }
}