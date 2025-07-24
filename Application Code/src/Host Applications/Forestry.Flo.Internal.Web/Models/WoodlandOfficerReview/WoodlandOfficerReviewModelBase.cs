using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// Base class for view models for the woodland officer review pages.
/// </summary>
public class WoodlandOfficerReviewModelBase : FellingLicenceApplicationPageViewModel
{
    /// <summary>
    /// Gets a flag indicating whether the application state and current user should be able
    /// to edit the woodland officer review details for the application.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public bool Editable(InternalUser user) =>
        FellingLicenceApplicationSummary.AssigneeHistories.Any(x =>
            x.Role == AssignedUserRole.WoodlandOfficer
            && x.UserAccount?.Id == user.UserAccountId
            && x.TimestampUnassigned.HasValue == false)
        && FellingLicenceApplicationSummary.Status == FellingLicenceStatus.WoodlandOfficerReview;
}