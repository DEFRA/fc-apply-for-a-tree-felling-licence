using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Common.Models;

/// <summary>
/// Model class representing a user's account for use in activity feed items.
/// </summary>
public class ActivityFeedItemUserModel
{
    public Guid Id { get; set; }

    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }

    public bool IsActiveUser { get; set; }

    public AccountTypeInternal AccountType { get; set; }

    public string FullName => $"{FirstName} {LastName} {(IsActiveUser == false ? "(deactivated)" : string.Empty)}".Trim().Replace("  ", " ");
}