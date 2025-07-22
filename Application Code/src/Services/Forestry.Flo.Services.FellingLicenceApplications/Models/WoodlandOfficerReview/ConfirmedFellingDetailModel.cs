using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the confirmed felling details of an application.
/// </summary>
public class NewConfirmedFellingDetailModel
{

    /// <summary>
    /// Gets and sets the type of the operation.
    /// </summary>
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets and sets the area to be felled.
    /// </summary>
    public double AreaToBeFelled { get; set; }

    /// <summary>
    /// Gets and sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is restocking for this felling.
    /// </summary>
    public bool? IsTreeMarkingUsed { get; set; }
    /// <summary>
    /// Gets and sets the tree marking.
    /// </summary>
    public string? TreeMarking { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether this instance is part of tree preservation order.
    /// </summary>
    public bool IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets or sets the tree preservation order reference.
    /// </summary>
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether this instance is within conservation area.
    /// </summary>
    public bool IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets or sets the conservation area reference.
    /// </summary>
    public string? ConservationAreaReference { get; set; }

    /// <summary>
    /// Gets or sets the estimated total felling volume.
    /// </summary>
    public double EstimatedTotalFellingVolume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is restocking for this felling.
    /// </summary>
    public bool? IsRestocking { get; set; }

    /// <summary>
    /// Gets or sets the reason for not having restocking.
    /// </summary>
    public string? NoRestockingReason { get; set; }

    /// <summary>
    /// Gets and sets the confirmed felling species.
    /// </summary>
    public IEnumerable<ConfirmedFellingSpecies> ConfirmedFellingSpecies { get; set; } = [];
}

/// <summary>
/// A model class representing an existing confirmed felling detail within a compartment.
/// </summary>
public class ConfirmedFellingDetailModel : NewConfirmedFellingDetailModel
{
    /// <summary>
    /// Gets and Sets the confirmed felling detail ID.
    /// </summary>
    public Guid ConfirmedFellingDetailsId { get; set; }

    /// <summary>
    /// Gets and sets a collection of confirmed restocking detail models for the felling detail.
    /// </summary>
    public IEnumerable<ConfirmedRestockingDetailModel> ConfirmedRestockingDetailModels { get; set; } = [];
    public Dictionary<string, string> AmendedProperties { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Record representing a new confirmed felling detail model with an associated compartment ID.
/// </summary>
/// <param name="CompartmentId">The unique identifier of the compartment.</param>
/// <param name="Model">The new confirmed felling detail model.</param>
public record NewConfirmedFellingDetailWithCompartmentId(Guid CompartmentId, NewConfirmedFellingDetailModel Model);