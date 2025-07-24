using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Internal.Web.Models.UserAccount;

/// <summary>
/// Model class representing an external user's account.
/// </summary>
public class ExternalUserModel
{
    public ExternalUserAccountModel ExternalUser { get; set; }

    public AgencyModel? AgencyModel { get; set; }
}