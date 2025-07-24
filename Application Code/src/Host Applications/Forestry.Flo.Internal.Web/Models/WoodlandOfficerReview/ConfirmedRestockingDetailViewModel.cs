using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Services.FellingLicenceApplications.Models; // Add this for TreeSpeciesFactory

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class ConfirmedRestockingDetailViewModel
{
    [HiddenInput]
    public Guid ConfirmedRestockingDetailsId { get; set; }

    public double? RestockArea { get; set; }

    public int? PercentOpenSpace { get; set; }

    public TypeOfProposal? RestockingProposal { get; set; }

    public ConfirmedRestockingSpeciesModel[] ConfirmedRestockingSpecies { get; set; } =
        Array.Empty<ConfirmedRestockingSpeciesModel>();

    public double? RestockingDensity { get; set; }

    public int? NumberOfTrees { get; set; }

    public int? PercentNaturalRegeneration { get; set; }

    public Guid RestockingCompartmentId { get; set; }

    public string? RestockingCompartmentNumber { get; set; }

    /// <summary>
    /// Returns a comma-separated list of actual species names and their percentages from ConfirmedRestockingSpecies,
    /// using TreeSpeciesFactory.SpeciesDictionary for name lookup.
    /// Ignores null, empty, or deleted species.
    /// Example: "Ash: 80%, Alder: 20%"
    /// </summary>
    public string ListRestockingSpecies()
    {
        return string.Join(
            ", ",
            ConfirmedRestockingSpecies
                .Where(s => !s.Deleted && !string.IsNullOrWhiteSpace(s.Species))
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