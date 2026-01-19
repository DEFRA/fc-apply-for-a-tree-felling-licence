using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public enum WoodlandSpeciesType
{
    [Display(Name = "Mixed woodland")]
    MixedWoodland,
    [Display(Name = "Broadleaf woodland")]
    BroadleafWoodland,
    [Display(Name = "Conifer woodland")]
    ConiferWoodland
}