using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

public class CBWCheckModel : AdminOfficerReviewCheckModelBase
{
    /// <summary>
    /// Gets and inits a collection of felling a restocking details for a given application.
    /// </summary>
    public IList<FellingAndRestockingDetail> FellingAndRestockingDetails { get; init; } = Array.Empty<FellingAndRestockingDetail>();

    public string? CaseNote { get; set; }
    public bool VisibleToApplicant { get; set; }
    public bool VisibleToConsultee { get; set; }

}