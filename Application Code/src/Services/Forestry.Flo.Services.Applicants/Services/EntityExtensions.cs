using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Applicants.Services;

public static class EntityExtensions
{
    public static bool IsWoodlandOwner(this UserAccount account)
    {
        return account.AccountType == AccountTypeExternal.WoodlandOwner ||
               account.AccountType == AccountTypeExternal.WoodlandOwnerAdministrator;
    }

    public static bool IsAgent(this UserAccount account)
    {
        return account.AccountType == AccountTypeExternal.Agent || 
               account.AccountType == AccountTypeExternal.AgentAdministrator;
    }

    public static bool IsFcUser(this UserAccount account)
    {
        return account.AccountType == AccountTypeExternal.FcUser;
    }

    public static string FullName(this UserAccount account, bool includeTitle = true)
    {
        if(account.Title == "Other" || account.Title == "No Title" || !includeTitle)
        {
            return $"{account.FirstName} {account.LastName}".Trim().Replace("  ", " ");
        }

        return $"{account.Title} {account.FirstName} {account.LastName}".Trim().Replace("  ", " ");
    }

    public static bool IsBlank(this Address? address)
    {
        if (address == null) return true;

        return string.IsNullOrWhiteSpace(address.Line1)
               && string.IsNullOrWhiteSpace(address.Line2)
               && string.IsNullOrWhiteSpace(address.Line3)
               && string.IsNullOrWhiteSpace(address.Line4)
               && string.IsNullOrWhiteSpace(address.PostalCode);
    }
}