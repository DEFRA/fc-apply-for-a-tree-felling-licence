using Forestry.Flo.Internal.Web.Models.UserAccount;

namespace Forestry.Flo.Internal.Web.Models.AccountAdministration;

public class FcStaffListModel : PageWithUserListModel
{
    public IEnumerable<UserAccountModel>? FcStaffList { get; set; }
}