using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model class for the PW14 Checks page of the woodland officer review.
/// </summary>
public class Pw14ChecksViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="Pw14ChecksModel"/>.
    /// </summary>
    public required Pw14ChecksModel Pw14Checks { get; set; }

    /// <summary>
    /// Gets and sets the activity feed model for the page.
    /// </summary>
    public required ActivityFeedModel ActivityFeed { get; set; }

    /// <summary>
    /// Gets and sets the optional form level case note for the page.
    /// </summary>
    public required FormLevelCaseNote FormLevelCaseNote { get; set; }
}