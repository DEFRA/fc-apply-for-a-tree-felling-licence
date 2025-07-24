using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.Reports;

public enum FellingOperationTypeForReporting
{
    [Display(Name = "Clear Felling")]
    ClearFelling,

    [Display(Name = "Felling Of Coppice")]
    FellingOfCoppice,
    
    [Display(Name = "Felling Individual Trees")]
    FellingIndividualTrees,
    
    [Display(Name = "Regeneration Felling")]
    RegenerationFelling,
    
    [Display(Name = "Thinning")]
    Thinning,

    [Display(Name = "None")]
    None,
}
