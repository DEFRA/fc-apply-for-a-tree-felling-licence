using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.UserAccount;

/// <summary>
/// Model class for amending an external user's account.
/// </summary>
public class AmendExternalUserAccountModel : PageWithBreadcrumbsViewModel
{
    public AccountPersonNameModel? PersonName { get; set; }

    public AccountPersonContactModel? PersonContactsDetails { get; set; }

    [HiddenInput]
    public Guid UserId { get; set; }

    [HiddenInput]
    public bool IsFcAgent { get; set; }
}