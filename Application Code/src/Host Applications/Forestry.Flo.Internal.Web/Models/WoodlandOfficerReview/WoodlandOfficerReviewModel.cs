using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model class for the summary page of the woodland officer review.
/// </summary>
public class WoodlandOfficerReviewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the application ID.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.WoodlandOfficerReviewTaskListStates"/> indicating the status of each
    /// step of the woodland officer review process.
    /// </summary>
    public WoodlandOfficerReviewTaskListStates WoodlandOfficerReviewTaskListStates { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="ActivityFeedModel"/> configured for woodland officer review comments only.
    /// </summary>
    public ActivityFeedModel WoodlandOfficerReviewCommentsFeed { get; set; }

    /// <summary>
    /// Gets and sets the recommended licence duration selected by the woodland officer.
    /// </summary>
    public RecommendedLicenceDuration? RecommendedLicenceDuration { get; set; }

    /// <summary>
    /// Gets and sets a bool to indicate whether the Woodland Officer has recommended that the application
    /// is published to the decision public register.
    /// </summary>
    public bool? RecommendationForDecisionPublicRegister { get; set; }

    /// <summary>
    /// Gets and sets the reason provided by the Woodland Officer for their recommendation
    /// that the application is or is not published to the decision public register.
    /// </summary>
    public string? RecommendationForDecisionPublicRegisterReason { get; set; }

    /// <summary>
    /// Gets and sets the name of the field manager assigned to the application
    /// </summary>
    public string? AssignedFieldManager { get; set; }
}