using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Confirmed restocking details entity class.
/// </summary>
public class ConfirmedRestockingDetail
{
    /// <summary>
    /// Gets and Sets the confirmed restocking detail ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the id of the submitted FLA property compartment these restocking details are for.
    /// </summary>
    public Guid SubmittedFlaPropertyCompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the confirmed felling detail id.
    /// </summary>
    [Required]
    public Guid ConfirmedFellingDetailId { get; set; }

    /// <summary>
    /// Gets and sets the confirmed felling detail.
    /// </summary>
    [Required]
    public ConfirmedFellingDetail ConfirmedFellingDetail { get; set; }

    /// <summary>
    /// Gets and sets the restocking proposal.
    /// </summary>
    public TypeOfProposal RestockingProposal { get; set; }

    /// <summary>
    /// Gets and sets the area.
    /// </summary>
    public double? Area { get; set; }

    /// <summary>
    /// Gets and sets the percentage of open space.
    /// </summary>
    public int? PercentOpenSpace { get; set; }

    /// <summary>
    /// Gets and sets the percentage of natural regeneration.
    /// </summary>
    public int? PercentNaturalRegeneration { get; set; }

    /// <summary>
    /// Gets and sets the percentage of restock area.
    /// </summary>
    public double? PercentageOfRestockArea { get; set; }

    /// <summary>
    /// Gets and sets the restocking density.
    /// </summary>
    public double? RestockingDensity { get; set; }

    /// <summary>
    /// Gets and sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets and sets the confirmed restocking species.
    /// </summary>
    public IList<ConfirmedRestockingSpecies> ConfirmedRestockingSpecies { get; set; } = new List<ConfirmedRestockingSpecies>();

    /// <summary>
    /// Gets and sets the identifier for the proposed restocking detail linked to this confirmed restocking detail.
    /// </summary>
    /// <remarks>
    /// This directly links a confirmed restocking detail to a proposed restocking detail.
    /// </remarks>
    public Guid? ProposedRestockingDetailId { get; set; }

}