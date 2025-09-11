using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model for site visit evidence on an application.
/// </summary>
public class AddSiteVisitEvidenceModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the id of the application for the site visit evidence.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the metadata entered by the woodland officer for the evidence files
    /// being uploaded.
    /// </summary>
    public SiteVisitEvidenceMetadataModel[] SiteVisitEvidenceMetadata { get; set; } = [];

    /// <summary>
    /// Gets and sets a flag indicating that the site visit has been completed.
    /// </summary>
    public bool SiteVisitComplete { get; set; }

    /// <summary>
    /// Gets and sets a comment entered by the woodland officer regarding the overall site visit.
    /// </summary>
    /// <remarks>This is only populated when the woodland officer enters it into the page -
    /// the text is then stored as a <see cref="CaseNote"/> with type SiteVisitComment so
    /// on subsequent page views will appear in the <see cref="SiteVisitComments"/>.</remarks>
    public FormLevelCaseNote SiteVisitComment { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="ActivityFeedModel"/> configured for site visit comments only.
    /// </summary>
    public ActivityFeedModel SiteVisitComments { get; set; }
}

/// <summary>
/// Model representing the metadata against a single site visit evidence file.
/// </summary>
public class SiteVisitEvidenceMetadataModel
{
    /// <summary>
    /// Gets and sets the filename of the uploaded evidence file.
    /// </summary>
    /// <remarks>
    /// For new files, this name is used to link the metadata to the actual file in the form file
    /// collection that is posted back to the server along with this model.
    /// </remarks>
    public required string FileName { get; set; }

    /// <summary>
    /// Gets and sets the id of the uploaded evidence file supporting document entity.
    /// </summary>
    /// <remarks>
    /// This field will only be populated for files that have already been uploaded, not new ones.
    /// </remarks>
    public Guid? SupportingDocumentId { get; set; }

    /// <summary>
    /// Gets and sets the label entered by the woodland officer for the uploaded evidence file.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets and sets the comment entered by the woodland officer for the uploaded evidence file.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the evidence file, when added as a supporting document,
    /// should be visible to the applicant.
    /// </summary>
    public bool VisibleToApplicants { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the evidence file, when added as a supporting document,
    /// should be visible to consultees.
    /// </summary>
    public bool VisibleToConsultees { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the woodland officer has marked this evidence file
    /// for deletion/removal.
    /// </summary>
    public bool MarkedForDeletion { get; set; }
}