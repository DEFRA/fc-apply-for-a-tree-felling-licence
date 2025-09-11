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
    /// Gets or sets the percentage of restock area.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public double? PercentageOfRestockArea { get; set; }

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
    /// Gets or sets the restocking outcomes.
    /// </summary>
    public IList<RestockingOutcome>? RestockingOutcomes { get; set; }
}