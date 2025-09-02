
namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the site visit details of the woodland officer review of an application.
/// </summary>
public class SiteVisitModel
{
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
    /// Gets and sets a list of <see cref="CaseNoteModel"/> containing the site visit comments.
    /// </summary>
    public IList<CaseNoteModel> SiteVisitComments { get; set; }
}