using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

public class AdminOfficerReviewCheckModelBase : FellingLicenceApplicationPageViewModel
{
    /// <summary>
    /// Gets and inits the application ID.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; init; }

    /// <summary>
    /// Gets and sets a flag indicating whether the check has passed.
    /// </summary>
    [Required(ErrorMessage = "Check must either pass or fail")]
    public bool? CheckPassed { get; set; }

    /// <summary>
    /// Gets and sets the reason for the check failure.
    /// </summary>
    [DisplayName("Reason for check failed")]
    public string? CheckFailedReason { get; set; }

    /// <summary>
    /// Gets a flag indicating whether the application state and current user should be able
    /// to edit the admin officer review details for the application.
    /// </summary>
    public bool Editable(InternalUser user) =>
        FellingLicenceApplicationSummary.AssigneeHistories.Any(x =>
            x.Role == AssignedUserRole.AdminOfficer
            && x.UserAccount?.Id == user.UserAccountId
            && x.TimestampUnassigned.HasValue == false)
        && FellingLicenceApplicationSummary.Status == FellingLicenceStatus.AdminOfficerReview;
}