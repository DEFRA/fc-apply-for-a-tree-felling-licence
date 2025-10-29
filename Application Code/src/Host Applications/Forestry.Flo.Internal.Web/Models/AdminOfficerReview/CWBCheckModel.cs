using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

public class CBWCheckModel : AdminOfficerReviewCheckModelBase
{
    /// <summary>
    /// Gets and inits a collection of felling a restocking details for a given application.
    /// </summary>
    public IList<FellingAndRestockingDetail> FellingAndRestockingDetails { get; init; } = Array.Empty<FellingAndRestockingDetail>();

    /// <summary>
    /// Gets and sets a model for the form-level case note for the CBW check.
    /// </summary>
    public required FormLevelCaseNote FormLevelCaseNote { get; set; }
}