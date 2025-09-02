using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class SiteVisitSummaryModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets a populated <see cref="ActivityFeedModel"/> configured for site visit comments only.
    /// </summary>
    public ActivityFeedModel SiteVisitComments { get; set; }
}