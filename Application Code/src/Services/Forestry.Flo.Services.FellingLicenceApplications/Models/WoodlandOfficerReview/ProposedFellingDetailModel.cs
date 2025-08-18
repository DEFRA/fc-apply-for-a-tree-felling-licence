using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// A model class representing the proposed felling details of a felling licence application.
/// </summary>
public class ProposedFellingDetailModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the proposed felling detail.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the operation.
    /// </summary>
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the restocking details associated with the proposed felling.
    /// </summary>
    public List<ProposedRestockingDetailModel> ProposedRestockingDetails { get; set; } = null!;

    /// <summary>
    /// Gets or sets compartment total hectares.
    /// </summary>
    public double? CompartmentTotalHectares { get; set; }

    /// <summary>
    /// Gets or sets the area to be felled.
    /// </summary>
    public double AreaToBeFelled { get; set; }

    /// <summary>
    /// Gets or sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether tree marking is used.
    /// </summary>
    public bool? IsTreeMarkingUsed { get; set; }

    /// <summary>
    /// Gets or sets the tree marking.
    /// </summary>
    public string? TreeMarking { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is part of tree preservation order.
    /// </summary>
    public bool? IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets and Sets the Tree Preservation Order reference.
    /// </summary>
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is within conservation area.
    /// </summary>
    public bool? IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets and Sets the conservation area reference.
    /// </summary>
    public string? ConservationAreaReference { get; set; }

    /// <summary>
    /// Gets or sets the species present in the proposed felling area, keyed by species code.
    /// </summary>
    public Dictionary<string, SpeciesModel> Species { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether restocking is planned for this compartment.
    /// </summary>
    public bool? IsRestocking { get; set; }

    /// <summary>
    /// Gets or sets the reason for not restocking, if applicable.
    /// </summary>
    public string? NoRestockingReason { get; set; }

    /// <summary>
    /// Gets or sets the estimated total volume of timber to be felled (in cubic meters).
    /// </summary>
    public double EstimatedTotalFellingVolume { get; set; }

    /// <summary>
    /// Gets or sets the GIS data associated with the proposed felling area.
    /// </summary>s
    public string? GisData { get; set; }

    /// <summary>
    /// Returns a comma-separated list of actual species names
    /// </summary>
    public string ListFellingSpecies()
    {
        return string.Join(
            ", ",
            Species.Values.Select(x => x.SpeciesName)
        );
    }
}