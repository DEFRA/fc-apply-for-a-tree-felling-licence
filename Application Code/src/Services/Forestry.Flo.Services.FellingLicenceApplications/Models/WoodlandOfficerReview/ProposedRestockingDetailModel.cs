using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// A model class representing proposed restocking details.
/// </summary>
public class ProposedRestockingDetailModel
{
    /// <summary>
    /// Gets or sets the proposed restocking detail ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the restocking proposal.
    /// </summary>
    public TypeOfProposal RestockingProposal { get; set; }

    /// <summary>
    /// Gets or sets the type of the operation.
    /// </summary>
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the area.
    /// </summary>
    public double? Area { get; set; }
    
    /// <summary>
    /// Gets or sets compartment total hectares.
    /// </summary>
    public double? CompartmentTotalHectares { get; set; }

    /// <summary>
    /// Gets or sets the percentage of restock area.
    /// </summary>
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
    public Dictionary<string, SpeciesModel> Species { get; set; } = [];

    /// <summary>
    /// Gets or sets the unique identifier for the compartment.
    /// </summary>
    public Guid CompartmentId { get; set; }

    /// <summary>
    /// Gets or sets the compartment number.
    /// </summary>
    public string? CompartmentNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the sub-compartment.
    /// </summary>
    public string? SubCompartmentName { get; set; }

    /// <summary>
    /// Returns a comma-separated list of actual species names and their percentages
    /// using TreeSpeciesFactory.SpeciesDictionary for name lookup.
    /// Ignores null, empty, or deleted species.
    /// Example: "Ash: 80%, Alder: 20%"
    /// </summary>
    public string ListRestockingSpecies()
    {
        return string.Join(
            ", ",
            Species.Values
                .Select(s =>
                {
                    var name = TreeSpeciesFactory.SpeciesDictionary.TryGetValue(s.Species!, out var speciesModel)
                        ? speciesModel.Name
                        : s.Species;
                    var percent = s.Percentage.HasValue ? $"{s.Percentage.Value}%" : "";
                    return string.IsNullOrEmpty(percent) ? name : $"{name}: {percent}";
                })
        );
    }
}