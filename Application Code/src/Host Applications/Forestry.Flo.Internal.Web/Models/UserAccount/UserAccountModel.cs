using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Internal.Web.Models.UserAccount;

/// <summary>
/// Model class representing a user's account of the system.
/// </summary>
public class UserAccountModel
{
    public Guid Id { get; set; }
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }

    public string? Email { get; set; }

    public AccountTypeInternal AccountType { get; set; }

    public AccountTypeInternalOther? AccountTypeOther { get; set; }

    public Status Status { get; set; }

    public string FullName => $"{FirstName} {LastName} {(IsActiveAccount is false ? "(deactivated)" : string.Empty)}".Trim().Replace("  ", " ");

    public bool CanApproveApplications { get; set; }

    public bool IsActiveAccount => Status is Status.Confirmed;

    public bool CanBeApproved => Status is Status.Requested or Status.Denied;

    public bool CanBeDenied => Status is Status.Requested or Status.Confirmed;
}