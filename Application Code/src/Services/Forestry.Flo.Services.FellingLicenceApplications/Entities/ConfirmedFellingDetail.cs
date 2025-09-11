using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Confirmed felling details entity class.
/// </summary>
public class ConfirmedFellingDetail
{
    /// <summary>
    /// Gets and Sets the confirmed felling detail ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the id of the submitted FLA property compartment these felling details are for.
    /// </summary>
    public Guid SubmittedFlaPropertyCompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the submitted FLA property compartment these felling details are for.
    /// </summary>
    public SubmittedFlaPropertyCompartment? SubmittedFlaPropertyCompartment { get; set; }

    /// <summary>
    /// Gets and sets the type of the operation.
    /// </summary>
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets and sets the area to be felled.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public double AreaToBeFelled { get; set; }

    /// <summary>
    /// Gets and sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets and sets the tree marking.
    /// </summary>
    public string? TreeMarking { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether this instance is part of tree preservation order.
    /// </summary>
    [Required]
    public bool IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets or sets the tree preservation order reference.
    /// </summary>
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether this instance is within conservation area.
    /// </summary>
    [Required]
    public bool IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets or sets the conservation area reference.
    /// </summary>
    public string? ConservationAreaReference { get; set; }

    /// <summary>
    /// Gets and sets the confirmed felling species.
    /// </summary>
    public List<ConfirmedFellingSpecies> ConfirmedFellingSpecies { get; set; } = new();

    /// <summary>
    /// Gets or sets the confirmed restocking details.
    /// </summary>
    public List<ConfirmedRestockingDetail> ConfirmedRestockingDetails { get; set; } = new();

    [Required]
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
    /// Gets and sets the identifier for the proposed felling detail linked to this confirmed felling detail.
    /// </summary>
    /// <remarks>
    /// This directly links a confirmed felling detail to a proposed felling detail.
    /// </remarks>
    public Guid? ProposedFellingDetailId { get; set; }
}