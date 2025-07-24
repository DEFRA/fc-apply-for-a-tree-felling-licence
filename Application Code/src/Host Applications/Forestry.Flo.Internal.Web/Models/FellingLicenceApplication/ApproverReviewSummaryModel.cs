using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ApproverReviewSummaryModel : FellingLicenceApplicationPageViewModel
{
    public List<SelectListItem> RecommendedLicenceDurations = [];

    [HiddenInput]
    public Guid Id { get; set; }

    public InternalUser? ViewingUser { get; set; }
    
    public Document? ApplicationDocument { get; set; }

    public IList<ActivityFeedItemModel> ActivityFeedItems { get; set; } = [];

    public IEnumerable<DocumentModel> Documents { get; set; } = [];

    public ApplicationOwnerModel? ApplicationOwner { get; set; }

    public OperationDetailsModel? OperationDetailsModel { get; set; }

    public bool IsReadonly { get; set; }

    /// <summary>
    /// Gets and sets the recommended licence duration selected by the woodland officer.
    /// </summary>
    public RecommendedLicenceDuration? RecommendedLicenceDuration { get; set; }

    public bool IsWOReviewed { get; set; }

    public required ApproverReviewModel ApproverReview { get; set; }

    /// <summary>
    /// Return application descision
    /// </summary>
    public bool? Decision { get; set; }
}