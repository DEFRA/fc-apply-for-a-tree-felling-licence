using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.AdminHubs.Model;

/// <summary>
/// Request class containing the required data necessary to change details of
/// an Admin Hub.
/// </summary>
public class UpdateAdminHubDetailsRequestModel : AdminHubUserOperationRequestBase
{
    public string NewAdminHubName { get; set; }

    public string NewAdminHubAddress { get; set; }

    public UpdateAdminHubDetailsRequestModel(
        Guid adminHubId, 
        Guid selectedOfficerId, 
        string newAdminHubName, 
        string newAdminHubAddress,
        Guid userId, 
        AccountTypeInternal accountType)
        :base(userId, accountType, adminHubId, selectedOfficerId)
    {
        NewAdminHubName = newAdminHubName;
        NewAdminHubAddress = newAdminHubAddress;
    }
}