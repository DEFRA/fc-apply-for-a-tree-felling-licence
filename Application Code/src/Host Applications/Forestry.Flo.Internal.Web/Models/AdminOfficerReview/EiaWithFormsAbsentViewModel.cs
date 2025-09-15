using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

/// <summary>
/// ViewModel for the Admin Officer Review check regarding EIA with forms absent.
/// </summary>
public class EiaWithFormsAbsentViewModel : AdminOfficerReviewCheckModelBase
{
    /// <summary>
    /// Gets or sets a value indicating whether the EIA form has been marked as received.
    /// </summary>
    public bool? HaveTheFormsBeenReceived { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the EIA process is in line with the code.
    /// </summary>
    public bool EiaProcessInLineWithCode { get; set; }

    /// <summary>
    /// Gets or sets the EIA tracker reference number.
    /// </summary>
    public string? EiaTrackerReferenceNumber { get; set; }
}