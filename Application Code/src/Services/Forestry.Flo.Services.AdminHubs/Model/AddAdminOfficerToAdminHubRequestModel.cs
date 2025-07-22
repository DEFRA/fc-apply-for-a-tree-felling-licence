using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.AdminHubs.Model;

/// <summary>
/// Request class containing the required data necessary to add an Admin Hub Officer to
/// an Admin Hub.
/// </summary>
public class AddAdminOfficerToAdminHubRequestModel :AdminHubUserOperationRequestBase
{
    public AddAdminOfficerToAdminHubRequestModel(
        Guid adminHubId, 
        Guid userId, 
        AccountTypeInternal performingUserAccountType, 
        Guid adminManagerId)
        :base(adminManagerId, performingUserAccountType, adminHubId, userId)
    {
    }
}

