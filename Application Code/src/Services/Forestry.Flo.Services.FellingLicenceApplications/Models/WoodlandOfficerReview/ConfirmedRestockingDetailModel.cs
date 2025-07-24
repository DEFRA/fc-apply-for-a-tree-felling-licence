using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the confirmed restocking details of an application
/// </summary>
public class ConfirmedRestockingDetailModel
{
    /// <summary>
    /// Gets and Sets the confirmed restocking detail ID.
    /// </summary>
    public Guid ConfirmedRestockingDetailsId { get; set; }

    /// <summary>
    /// Gets and sets the type of the operation.
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

    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets and sets the confirmed restocking species.
    /// </summary>
    public IList<ConfirmedRestockingSpecies>? ConfirmedRestockingSpecies { get; set; }

    public Guid CompartmentId { get; set; }

    public string? CompartmentNumber { get; set; }

    public string? SubCompartmentName { get; set; }
}