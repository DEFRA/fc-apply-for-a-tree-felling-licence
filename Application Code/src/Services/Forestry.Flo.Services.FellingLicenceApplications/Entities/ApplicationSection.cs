using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

public enum ApplicationSection
{
    [Display(Name = "Compartments")]
    Compartments = 1,
    [Display(Name = "Proposed felling details")]
    ProposedFellingDetails,
    [Display(Name = "Proposed restocking details")]
    ProposedRestockingDetails,
    [Display(Name = "Operations")]
    Operations,
    [Display(Name = "Documents")]
    Documents
}