using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.AccountAdministration;

public class PageWithUserListModel : PageWithBreadcrumbsViewModel
{
    [HiddenInput]
    [Required(ErrorMessage = "You must select a user")]
    public Guid? SelectedUserAccountId { get; set; }

    [HiddenInput]
    public string ReturnUrl { get; set; }
}