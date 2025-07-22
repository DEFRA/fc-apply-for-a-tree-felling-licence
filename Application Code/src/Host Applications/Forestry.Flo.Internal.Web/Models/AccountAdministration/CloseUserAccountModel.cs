using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.AccountAdministration;

public class CloseUserAccountModel : PageWithBreadcrumbsViewModel
{
    public UserAccountModel AccountToClose { get; set; }
}