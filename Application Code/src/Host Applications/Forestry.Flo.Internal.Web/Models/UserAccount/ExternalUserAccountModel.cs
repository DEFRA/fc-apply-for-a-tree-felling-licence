using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Internal.Web.Models.UserAccount;

/// <summary>
/// Model class representing an external user's account.
/// </summary>
public class ExternalUserAccountModel
{
    public Guid Id { get; set; }

    public string? Title { get; set; }
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }

    public string? Email { get; set; }

    public AccountTypeExternal AccountType { get; set; }

    public Guid? WoodlandOwnerId { get; set; }

    public string FullName => $"{Title} {FirstName} {LastName}".Trim().Replace("  ", " ");
}