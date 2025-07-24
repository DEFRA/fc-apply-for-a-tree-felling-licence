using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
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
    /// Gets and sets a flag indicating whether the woodland officer has decided that a site visit is not needed.
    /// </summary>
    public bool SiteVisitNotNeeded { get; set; }

    /// <summary>
    /// Gets and sets the date and time that site visit artefacts were created, effectively starting the site visit process.
    /// </summary>
    public DateTime? SiteVisitArtefactsCreated { get; set; }

    /// <summary>
    /// Gets and sets the ID of the application PDF supporting document entry, if it has been generated yet.
    /// </summary>
    [HiddenInput]
    public bool ApplicationDocumentHasBeenGenerated { get; set; }

    /// <summary>
    /// Gets and sets the date and time that site visit notes (from the mobile apps) were retrieved, effectively ending the site visit process.
    /// </summary>
    public DateTime? SiteVisitNotesRetrieved { get; set; }

    /// <summary>
    /// Gets and sets a reason entered by the woodland officer why a site visit is not needed.
    /// </summary>
    /// <remarks>This is only populated when the woodland officer enters it into the page -
    /// the text is then stored as a <see cref="CaseNote"/> with type SiteVisitComment so
    /// on subsequent page views will appear in the <see cref="SiteVisitComments"/>.</remarks>
    public string SiteVisitNotNeededReason { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="ActivityFeedModel"/> configured for site visit comments only.
    /// </summary>
    public ActivityFeedModel SiteVisitComments { get; set; }
}