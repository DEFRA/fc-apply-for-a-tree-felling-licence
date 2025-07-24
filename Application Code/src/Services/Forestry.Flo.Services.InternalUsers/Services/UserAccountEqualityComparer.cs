using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Services.InternalUsers.Services;

public class UserAccountEqualityComparer : IEqualityComparer<UserAccount>
{
    public bool Equals(UserAccount x, UserAccount y)
    {
        return x.Id.Equals(y.Id);
    }

    public int GetHashCode(UserAccount obj)
    {
        return obj.Id.GetHashCode();
    }
}