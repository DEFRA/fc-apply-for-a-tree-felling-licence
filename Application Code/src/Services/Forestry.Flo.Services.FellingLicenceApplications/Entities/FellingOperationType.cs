using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

public enum FellingOperationType
{
    [Display(Name = "Select a value")]
    None = 0,
    [Display(Name = "Clear felling")]
    [Description("Remove all or most trees in an identified area of woodland in one operation, sometimes leaving small clumps or individual trees intact. Typically, restocking of the felled area will be required.")]
    ClearFelling,
    [Display(Name = "Coppicing")]
    [Description("Remove some or all woody stems growing from an existing coppice stool, that are over 15cm in diameter when measured 1.3 meters from the ground. This is to stimulate new woody growth from that stool. Typically, restocking of the felled area using coppice regrowth will be required.")]
    FellingOfCoppice,
    [Display(Name = "Individual trees (including trees within hedgerows)")]
    [Description("Remove specifically identified individual trees, for example parkland trees, trees within hedgerows or trees otherwise specifically identifed. Typically, restocking of the felled tree/s by planting or regeneration is required.")]
    FellingIndividualTrees,
    [Display(Name = "Regeneration felling")]
    [Description("Remove selected dominant trees to promote natural regeneration of lower woodland layers. Typically, restocking is required, primarily through natural regeneration.")]
    RegenerationFelling,
    [Display(Name = "Thinning")]
    [Description("Remove up to a third of trees evenly across the area to promote the growth of the remaining trees. Restocking is not required.")]
    Thinning
}