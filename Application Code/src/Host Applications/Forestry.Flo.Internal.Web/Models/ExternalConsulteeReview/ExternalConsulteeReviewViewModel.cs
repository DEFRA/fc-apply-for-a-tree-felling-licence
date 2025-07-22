using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;

public class ExternalConsulteeReviewViewModel
{
    //TODO - whatever other information FC decide external consultees should see

    public string ApplicationReference { get; set; }

    public string? PropertyName { get; set; }

    [Required]
    public AddConsulteeCommentModel AddConsulteeComment { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="ActivityFeedModel"/> configured for site visit comments only.
    /// </summary>
    public ActivityFeedModel ActivityFeed { get; set; }
}