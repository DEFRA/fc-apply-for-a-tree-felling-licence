using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.AccountAdministration;

public class ExternalUserListModel : PageWithUserListModel
{
    public IEnumerable<ExternalUserModel>? ExternalUserList { get; set; }
}