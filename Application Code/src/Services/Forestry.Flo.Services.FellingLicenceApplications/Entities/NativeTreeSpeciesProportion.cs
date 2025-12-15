using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Enumeration of proportion values for native tree species in a PAWS zone.
/// </summary>
public enum NativeTreeSpeciesProportion
{
    [Display(Name="<50%")]
    LessThan50Percent,

    [Display(Name="50-80%")]
    Between50And80Percent,

    [Display(Name = ">80%")]
    GreaterThan80Percent
}