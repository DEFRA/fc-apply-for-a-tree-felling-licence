using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

public enum RecommendedLicenceDuration
{
    [Display(Name = "No recommendation")]
    None,

    [Display(Name = "2 year licence")]
    TwoYear = 2,

    [Display(Name = "3 year licence")]
    ThreeYear = 3,

    [Display(Name = "4 year licence")]
    FourYear = 4,

    [Display(Name = "5 year licence")]
    FiveYear = 5,

    [Display(Name = "6 year licence")]
    SixYear = 6,

    [Display(Name = "7 year licence")]
    SevenYear = 7,

    [Display(Name = "8 year licence")]
    EightYear = 8,

    [Display(Name = "9 year licence")]
    NineYear = 9,

    [Display(Name = "10 year licence")]
    TenYear = 10
}