using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model for amending confirmed felling and restocking details during woodland officer review.
/// </summary>
public class AmendConfirmedFellingDetailsViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    [HiddenInput]
    public required Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the confirmed felling and restocking details for the compartment.
    /// </summary>
    public required IndividualConfirmedFellingRestockingDetailModel ConfirmedFellingRestockingDetails { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the confirmed felling and restocking is complete.
    /// </summary>
    public required bool ConfirmedFellingAndRestockingComplete { get; set; }

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