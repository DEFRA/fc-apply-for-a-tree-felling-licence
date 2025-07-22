using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Services.InternalUsers.Services;

public static class EntityExtensions
{
    public static string FullName(this UserAccount account, bool includeTitle = true)
    {
        if (account.Title == "Other" || account.Title == "No Title" || !includeTitle)
        {
            return $"{account.FirstName} {account.LastName}".Trim().Replace("  ", " ");
        }

        return $"{account.Title} {account.FirstName} {account.LastName}".Trim().Replace("  ", " ");
    }

    public static string FullNameNoTitle(this UserAccount account)
    {
        return $"{account.FirstName} {account.LastName}".Trim().Replace("  ", " ");
    }
}