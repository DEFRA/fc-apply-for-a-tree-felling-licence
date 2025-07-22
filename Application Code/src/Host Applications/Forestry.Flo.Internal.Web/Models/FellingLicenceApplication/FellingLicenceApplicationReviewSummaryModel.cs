using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class FellingLicenceApplicationReviewSummaryModel : FellingLicenceApplicationPageViewModel
{
    [HiddenInput]
    public Guid Id { get; set; }

    public InternalUser? ViewingUser { get; set; }
    
    public Document? ApplicationDocument { get; set; }

    public IList<ActivityFeedItemModel> ActivityFeedItems { get; set; }

    public FellingAndRestockingDetails? FellingAndRestockingDetail { get; set; }

    public IEnumerable<DocumentModel> Documents { get; set; } = Enumerable.Empty<DocumentModel>();

    public ApplicationOwnerModel? ApplicationOwner { get; set; }

    public OperationDetailsModel? OperationDetailsModel { get; set; }

    /// <summary>
    /// Determines whether an application is editable based on its status.
    /// </summary>
    public bool IsEditable =>
        FellingLicenceApplicationSummary?.StatusHistories.MaxBy(x => x.Created)?.Status
            is not (
            FellingLicenceStatus.Approved 
            or FellingLicenceStatus.Refused 
            or FellingLicenceStatus.Withdrawn 
            or FellingLicenceStatus.ReferredToLocalAuthority);

    public bool UserCanApproveRefuseReferApplication { get; set; }
}