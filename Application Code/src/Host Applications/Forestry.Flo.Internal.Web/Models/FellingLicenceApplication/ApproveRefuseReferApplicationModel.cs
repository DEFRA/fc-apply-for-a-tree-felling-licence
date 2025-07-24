using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ApproveRefuseReferApplicationModel: FellingLicenceApplicationPageViewModel
{
    /// <summary>
    /// Gets and inits the requested status.
    /// </summary>
    public FellingLicenceStatus RequestedStatus { get; init; }
}
