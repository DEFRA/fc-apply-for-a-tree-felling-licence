using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Enumeration representing reasons that a felling licence application may be withdrawn. This is intended to
/// be used for reporting and analytics purposes, and to inform the content of notifications sent to users
/// when an application is withdrawn.
/// </summary>
public enum WithdrawalReason
{
    [Display(Name="Exceeded the 21 day deadline to resubmit the application")]
    [Description("The application has not been resubmitted within the required 21 day timeframe.")]
    ExceededResubmitDeadline,

    [Display(Name = "Exceeded the 28 day deadline to respond to felling and restocking amendments")]
    [Description("The amendments made by the woodland officer have not been responded to within the required 28 day timeframe.")]
    ExceededAmendmentsResponseDeadline,

    [ApplicantOption]
    [Display(Name="Application is no longer needed")]
    [Description("The need for a felling licence is no longer necessary due to a change in circumstances or change of mind.")]
    ApplicationNoLongerNeeded,

    [ApplicantOption]
    [Display(Name="Application does not meet UK Forest Standards")]
    [Description("The application does not comply with the required UK Forest Standards.")]
    ApplicationDoesNotMeetStandards,

    [ApplicantOption]
    [Display(Name="The felling licence is not required")]
    [Description("The work meets exception criteria, so it does not need a felling licence.")]
    FellingLicenceNotRequired,

    [ApplicantOption]
    [Display(Name="There is a duplicate or existing licence for the proposed work")]
    [Description("A valid licence already covers the planned works.")]
    DuplicateOrExistingLicenceExists,

    [ApplicantOption]
    [Display(Name="The works are covered by a Statutory Plant Health Notice (SPHN)")]
    [Description("The works are already authorised under the SPHN, so it does not need a separate felling licence.")]
    WorksCoveredBySPHN,

    [ApplicantOption]
    [Display(Name="A breach of grant conditions has occurred")]
    [Description("The application no longer complies with grant conditions.")]
    BreachOfGrantConditions,

    [ApplicantOption]
    [Display(Name="Combining this application with another one")]
    [Description("The proposed works will be included in a different application.")]
    CombineWithAnotherApplication,

    [ApplicantOption]
    [Display(Name="Divide this application into smaller parts")]
    [Description("The application will be split into more manageable sections.")]
    DivideTheApplication,

    [ApplicantOption]
    [Display(Name="Other")]
    [Description("Please specify.")]
    Other
}