using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

/// <summary>
/// ViewModel for the Admin Officer Review check regarding EIA with forms present.
/// Contains properties to capture the correctness of forms, EIA process compliance,
/// and the EIA tracker reference number.
/// </summary>
public class EiaWithFormsPresentViewModel : AdminOfficerReviewCheckModelBase
{
    /// <summary>
    /// Gets or sets a value indicating whether the forms are correct.
    /// </summary>
    public bool? AreTheFormsCorrect { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the EIA process is in line with the code.
    /// </summary>
    public bool EiaProcessInLineWithCode { get; set; }

    /// <summary>
    /// Gets or sets the EIA tracker reference number.
    /// </summary>
    public string? EiaTrackerReferenceNumber { get; set; }

    /// <summary>
    /// Gets and sets the collection of document models associated with the EIA.
    /// </summary>
    public required IEnumerable<DocumentModel> EiaDocumentModels { get; set; }
}