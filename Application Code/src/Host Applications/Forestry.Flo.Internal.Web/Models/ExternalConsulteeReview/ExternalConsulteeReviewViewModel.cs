using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;

/// <summary>
/// Viewmodel class for the external consultee review page.
/// </summary>
public class ExternalConsulteeReviewViewModel
{
    /// <summary>
    /// Gets and sets a <see cref="FellingLicenceApplicationSummaryModel"/> class representing the application.
    /// </summary>
    public FellingLicenceApplicationSummaryModel ApplicationSummary { get; init; } = new();

    /// <summary>
    /// Gets and sets a list of <see cref="DocumentModel"/> representing the supporting documents available to view by consultees.
    /// </summary>
    public List<DocumentModel> ConsulteeDocuments { get; init; }

    /// <summary>
    /// Gets and sets a populated <see cref="AddConsulteeCommentModel"/> for adding consultee comments to the application.
    /// </summary>
    [Required]
    public AddConsulteeCommentModel AddConsulteeComment { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="ActivityFeedModel"/> configured for consultee comments only.
    /// </summary>
    public ActivityFeedModel ActivityFeed { get; init; }
}