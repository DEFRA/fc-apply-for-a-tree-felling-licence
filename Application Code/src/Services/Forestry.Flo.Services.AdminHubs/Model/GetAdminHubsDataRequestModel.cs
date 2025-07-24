using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.AdminHubs.Model;

public class GetAdminHubsDataRequestModel: AdminHubRequestBase
{
    public GetAdminHubsDataRequestModel(
        Guid performingUserId, 
        AccountTypeInternal performingUserAccountType)
        : base(performingUserId, performingUserAccountType)
    {
        
    }

    public static GetAdminHubsDataRequestModel CreateSystemRequest => new GetAdminHubsDataRequestModel(
        Guid.Empty,
        AccountTypeInternal.AdminHubManager);
}