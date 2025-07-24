using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

public class AgentAuthorityFormCheckModel : AdminOfficerReviewCheckModelBase
{
    /// <summary>
    /// Gets and inits a populated <see cref="ApplicationOwnerModel"/> representing the application owner.
    /// </summary>
    public ApplicationOwnerModel? ApplicationOwner { get; init; }
}