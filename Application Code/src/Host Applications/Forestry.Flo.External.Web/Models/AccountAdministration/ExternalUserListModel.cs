using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Applicants.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.AccountAdministration;

public class ExternalUserListModel : PageWithBreadcrumbsViewModel
{
    public IEnumerable<UserAccountModel>? ExternalUserList { get; set; }

    [HiddenInput]
    [Required(ErrorMessage = "You must select a user")]
    public Guid? SelectedUserAccountId { get; set; }

    [HiddenInput]
    public string ReturnUrl { get; set; }
}