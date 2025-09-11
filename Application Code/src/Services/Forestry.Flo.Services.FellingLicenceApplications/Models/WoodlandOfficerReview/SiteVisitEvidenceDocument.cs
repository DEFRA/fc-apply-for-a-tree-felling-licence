namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class representing a site visit evidence type supporting document.
/// </summary>
public class SiteVisitEvidenceDocument
{
    /// <summary>
    /// Gets and sets the id of the supporting document that this metadata relates to.
    /// </summary>
    public required Guid DocumentId { get; set; }

    /// <summary>
    /// Gets and sets the name of the file as it was uploaded.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// An optional label entered by the woodland officer for the uploaded evidence file.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// An optional comment entered by the woodland officer about the uploaded evidence file.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets and sets the visibility of the document to the applicant.
    /// </summary>
    public bool VisibleToApplicant { get; set; }

    /// <summary>
    /// Gets and sets the visibility of the document to consultees.
    /// </summary>
    public bool VisibleToConsultee { get; set; }
}