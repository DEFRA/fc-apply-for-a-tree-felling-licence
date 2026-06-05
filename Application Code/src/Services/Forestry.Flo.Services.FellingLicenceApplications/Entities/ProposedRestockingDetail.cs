using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// ProposedRestockingDetail entity class
/// </summary>
public class ProposedRestockingDetail
{
    /// <summary>
    /// Gets and Sets the property document ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the proposed felling details ID.
    /// </summary>
    [Required]
    public Guid ProposedFellingDetailsId { get; set; }

    /// <summary>
    /// Gets or sets the proposed felling detail.
    /// </summary>
    [Required]
    public ProposedFellingDetail ProposedFellingDetail { get; set; }

    /// <summary>
    /// Gets or sets the property profile compartment identifier.
    /// </summary>
    [Required]
    public Guid PropertyProfileCompartmentId { get; set; }

    /// <summary>
    /// Gets or sets the restocking proposal.
    /// </summary>
    public TypeOfProposal RestockingProposal { get; set; }

    /// <summary>
    /// Gets or sets the area.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public double? Area { get; set; }

    /// <summary>
    /// Gets and sets the percentage of open space.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public double? PercentOpenSpace { get; set; }

    /// <summary>
    /// Gets or sets the restock area as a percentage of compartment size.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public double? PercentageOfRestockArea { get; set; }

    /// <summary>
    /// Gets and sets the restock area as a percentage of felling operation area.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
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
    /// Gets or sets the restocking species.
    /// </summary>
    public IList<RestockingSpecies>? RestockingSpecies{ get; set; }

    /// <summary>
    /// Gets or sets the percentage established by coppice regrowth or natural regeneration.
    /// </summary>
    /// <remarks>
    /// Only applicable if the restocking proposal is either coppice regrowth or natural regeneration.
    /// This field should be null for other restocking proposals.
    /// </remarks>
    public double? PercentageEstablishedByCoppiceOrNaturalRegen { get; set; }

    /// <summary>
    /// Gets or sets the restocking outcomes.
    /// </summary>
    public IList<RestockingOutcome>? RestockingOutcomes { get; set; }
}