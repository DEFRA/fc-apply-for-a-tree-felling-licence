using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model for adding a new confirmed felling detail during woodland officer review.
/// </summary>
public class AddNewConfirmedFellingDetailsViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    [HiddenInput]
    public required Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the confirmed felling and restocking details for the compartment.
    /// </summary>
    public required NewConfirmedFellingDetailModel ConfirmedFellingRestockingDetails { get; set; }

    /// <summary>
    /// Gets a dictionary of species models keyed by species code, representing the species included in the confirmed felling details.
    /// </summary>
    public Dictionary<string, SpeciesModel> Species => _species ??= BuildSpecies();
    private Dictionary<string, SpeciesModel>? _species;

    private Dictionary<string, SpeciesModel> BuildSpecies()
    {
        var result = new Dictionary<string, SpeciesModel>();
        var dictionary = TreeSpeciesFactory.SpeciesDictionary;

        foreach (var species in ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConfirmedFellingSpecies)
        {
            if (species.Species is null || species.Id is null)
                continue;

            result[species.Species] = new SpeciesModel
            {
                Id = species.Id.Value,
                Species = species.Species,
                SpeciesName = dictionary[species.Species].Name,
            };
        }

        return result;
    }
}