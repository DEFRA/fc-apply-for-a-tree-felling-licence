using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.AdminHubs.Model;

/// <summary>
/// Request class containing the required data necessary to remove an Admin Hub Officer from
/// an Admin Hub.
/// </summary>
public class RemoveAdminOfficerFromAdminHubRequestModel: AdminHubUserOperationRequestBase
{
    public RemoveAdminOfficerFromAdminHubRequestModel(
        Guid adminHubId, 
        Guid userId, 
        AccountTypeInternal userAccountType, 
        Guid adminManagerId)
        :base(adminManagerId, userAccountType, adminHubId, userId)
    {
    }
}
