using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

public enum AssignedUserRole
{
    [Display(Name = "Author")]
    Author,
    [Display(Name= "Operations Admin Officer")]
    AdminOfficer,
    [Display(Name="Woodland Officer")]
    WoodlandOfficer,
    [Display(Name="Approver")]
    FieldManager,
    [Display(Name = "Applicant")]
    Applicant
}