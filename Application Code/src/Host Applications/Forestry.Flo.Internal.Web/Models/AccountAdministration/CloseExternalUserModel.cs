using Forestry.Flo.Internal.Web.Models.UserAccount;

namespace Forestry.Flo.Internal.Web.Models.AccountAdministration;

public class CloseExternalUserModel : PageWithBreadcrumbsViewModel
{
    public ExternalUserModel AccountToClose { get; set; }
}