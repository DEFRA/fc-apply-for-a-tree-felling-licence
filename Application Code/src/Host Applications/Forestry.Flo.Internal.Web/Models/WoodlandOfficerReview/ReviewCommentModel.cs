using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.Notifications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model class for the Public Register page of the woodland officer review.
/// </summary>
public class ReviewCommentModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the id of the application.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }
    

    /// <summary>
    /// Gets and sets a collection of <see cref="NotificationHistoryModel"/> representing received public register comments.
    /// </summary>
    public NotificationHistoryModel? Comment { get; set; }

    public string? CommentDeepLink { get; set; } = string.Empty;

}