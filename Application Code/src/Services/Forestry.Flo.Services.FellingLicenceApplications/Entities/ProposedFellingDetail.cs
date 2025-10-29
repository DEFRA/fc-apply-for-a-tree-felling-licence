using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// ProposedFellingDetail entity class
/// </summary>
public class ProposedFellingDetail
{
    /// <summary>
    /// Gets and Sets the proposed felling detail ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the linked property profile identifier.
    /// </summary>
    public Guid LinkedPropertyProfileId { get; set; }

    /// <summary>
    /// Gets or sets the linked property profile.
    /// </summary>
    public LinkedPropertyProfile LinkedPropertyProfile { get; set; }

    /// <summary>
    /// Gets or sets the property profile compartment identifier.
    /// </summary>
    public Guid PropertyProfileCompartmentId { get; set; }

    /// <summary>
    /// Gets or sets the type of the operation.
    /// </summary>
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the area to be felled.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
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
    [Required]
    public bool IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets or sets the tree preservation order reference.
    /// </summary>
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is within conservation area.
    /// </summary>
    [Required]
    public bool IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets or sets the conservation area reference.
    /// </summary>
    public string? ConservationAreaReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is restocking for this felling.
    /// </summary>
    public bool? IsRestocking { get; set; }

    /// <summary>
    /// Gets or sets the reason for not having restocking.
    /// </summary>
    public string? NoRestockingReason { get; set; }

    /// <summary>
    /// Gets or sets the felling species.
    /// </summary>
    public IList<FellingSpecies>? FellingSpecies { get; set; }

    /// <summary>
    /// Gets or sets the felling outcomes.
    /// </summary>
    public IList<FellingOutcome>? FellingOutcomes { get; set; }

    /// <summary>
    /// Gets or sets the proposed restocking details.
    /// </summary>
    public IList<ProposedRestockingDetail>? ProposedRestockingDetails { get; set; }

    [Required]
    public double EstimatedTotalFellingVolume { get; set; }
}