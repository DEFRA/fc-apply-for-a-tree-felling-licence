using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.Reports;

public enum DateRangeTypeForReporting
{
    [Display(Name = "Date Submitted")]
    SubmittedDate,
    [Display(Name = "Date Approved")]
    ApprovedDate,
    [Display(Name = "Referred To Local Authority")]
    ReferredToLocalAuthority,
    [Display(Name = "Final Action")]
    FinalAction,
    [Display(Name = "On Public Register")]
    OnPublicRegister,
    [Display(Name = "Off Public Register")]
    OffPublicRegister,
    [Display(Name = "Public Register Expiry")]
    PublicRegisterExpiry,
    [Display(Name = "Citizens Charter")]
    CitizensCharter,
    //Todo re-instate when FLA is populating the fields which would be used by this option.
    //[Display(Name = "Completed Felling")]
    //CompletedFelling
    //[Display(Name = "Completed Restocking")]
    //CompletedRestocking
}