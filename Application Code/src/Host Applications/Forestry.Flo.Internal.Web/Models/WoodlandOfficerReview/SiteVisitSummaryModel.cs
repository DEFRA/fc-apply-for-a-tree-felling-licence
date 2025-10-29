using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class SiteVisitSummaryModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets a populated <see cref="ActivityFeedModel"/> configured for site visit comments only.
    /// </summary>
    public ActivityFeedModel SiteVisitComments { get; set; }

    /// <summary>
    /// Gets and sets the felling and restocking details for the application.
    /// </summary>
    public FellingAndRestockingDetails FellingAndRestockingDetail { get; set; }

    /// <summary>
    /// Gets and sets an image of the felling operations converted to a base64 string.
    /// </summary>
    public string? FellingMapBase64 { get; set; }

    /// <summary>
    /// Gets and sets an image of the restocking operations converted to a base64 string.
    /// </summary>
    public string? RestockingMapBase64 { get; set; }

    /// <summary>
    /// Gets and sets the application owner details for the application.
    /// </summary>
    public ApplicationOwnerModel ApplicationOwner { get; set; }
}