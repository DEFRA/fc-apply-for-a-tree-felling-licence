using Forestry.Flo.Services.Applicants.Models;
using WoodlandOwnerModel = Forestry.Flo.Internal.Web.Models.UserAccount.WoodlandOwnerModel;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ApplicationOwnerModel
{
    public WoodlandOwnerModel WoodlandOwner { get; set; }

    public AgencyModel? Agency { get; set; }

    public AgentAuthorityFormViewModel? AgentAuthorityForm { get; set; }
}