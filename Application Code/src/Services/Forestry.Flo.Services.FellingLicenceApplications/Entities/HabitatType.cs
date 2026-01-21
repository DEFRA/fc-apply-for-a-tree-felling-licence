using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

public enum HabitatType
{
    [Display(Name = "Blanket bog (moor)")]
    BlanketBog,

    [Display(Name = "Fen")]
    Fen,

    [Display(Name = "Limestone pavement")]
    LimestonePavement,

    [Display(Name = "Lowland chalk grassland")]
    LowlandChalkGrassland,

    [Display(Name = "Lowland dry-acid grassland")]
    LowlandDryAcidGrassland,

    [Display(Name = "Lowland heathland")]
    LowlandHeathland,

    [Display(Name = "Lowland meadow")]
    LowlandMeadow,

    [Display(Name = "Lowland raised bog")]
    LowlandRaisedBog,

    [Display(Name = "Purple moor-grass and rush pasture")]
    PurpleMoorGrassAndRushPasture,

    [Display(Name = "Reedbed")]
    Reedbed,

    [Display(Name = "Upland hay meadow")]
    UplandHayMeadow,

    [Display(Name = "Upland heathland")]
    UplandHeathland,

    [Display(Name = "Other")]
    Other
}