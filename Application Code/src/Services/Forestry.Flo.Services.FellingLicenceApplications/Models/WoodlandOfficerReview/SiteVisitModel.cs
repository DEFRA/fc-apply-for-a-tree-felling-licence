
namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the site visit details of the woodland officer review of an application.
/// </summary>
public class SiteVisitModel
{
    /// <summary>
    /// Gets and sets a flag indicating whether the woodland officer has decided that a site visit is not needed.
    /// </summary>
    public bool SiteVisitNotNeeded { get; set; }

    /// <summary>
    /// Gets and sets the date and time that site visit artefacts were created, effectively starting the site visit process.
    /// </summary>
    public DateTime? SiteVisitArtefactsCreated { get; set; }

    /// <summary>
    /// Gets and sets the date and time that site visit notes (from the mobile apps) were retrieved, effectively ending the site visit process.
    /// </summary>
    public DateTime? SiteVisitNotesRetrieved { get; set; }

    /// <summary>
    /// Gets and sets a list of <see cref="CaseNoteModel"/> containing the site visit comments.
    /// </summary>
    public IList<CaseNoteModel> SiteVisitComments { get; set; }
}