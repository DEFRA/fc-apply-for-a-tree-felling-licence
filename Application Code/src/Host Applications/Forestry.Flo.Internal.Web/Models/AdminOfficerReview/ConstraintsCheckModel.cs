using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

public class ConstraintsCheckModel : FellingLicenceApplicationPageViewModel
{
    /// <summary>
    /// Gets and sets the application ID.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the check is complete.
    /// </summary>
    public bool IsComplete { get; set; }

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