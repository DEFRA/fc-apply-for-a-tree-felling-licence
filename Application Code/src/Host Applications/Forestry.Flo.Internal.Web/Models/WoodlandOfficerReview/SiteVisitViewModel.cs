using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model class for the Site Visit page of the woodland officer review.
/// </summary>
public class SiteVisitViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    [HiddenInput]
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the woodland officer has decided that a site visit is needed.
    /// </summary>
    public bool? SiteVisitNeeded { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating that the site visit arrangements have been made.
    /// </summary>
    public bool? SiteVisitArrangementsMade { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating that the site visit has been completed and resulting notes/attachments have been uploaded.
    /// </summary>
    public bool SiteVisitComplete { get; set; }

    /// <summary>
    /// Gets and sets a reason entered by the woodland officer why a site visit is not needed.
    /// </summary>
    /// <remarks>This is only populated when the woodland officer enters it into the page -
    /// the text is then stored as a <see cref="CaseNote"/> with type SiteVisitComment so
    /// on subsequent page views will appear in the <see cref="SiteVisitComments"/>.</remarks>
    public FormLevelCaseNote SiteVisitNotNeededReason { get; set; }

    /// <summary>
    /// Gets and sets any notes entered by the woodland officer about arrangements for the site visit.
    /// </summary>
    /// <remarks>This is only populated when the woodland officer enters it into the page -
    /// the text is then stored as a <see cref="CaseNote"/> with type SiteVisitComment so
    /// on subsequent page views will appear in the <see cref="SiteVisitComments"/>.</remarks>
    public FormLevelCaseNote SiteVisitArrangementNotes { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="ActivityFeedModel"/> configured for site visit comments only.
    /// </summary>
    public ActivityFeedModel SiteVisitComments { get; set; }
}