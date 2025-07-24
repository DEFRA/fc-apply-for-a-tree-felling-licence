using Forestry.Flo.Services.Applicants.Entities.UserAccount;

namespace Forestry.Flo.Services.Applicants.Services
{
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
}
