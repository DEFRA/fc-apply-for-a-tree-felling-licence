using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Mandates that the user account has a certain role before a user can complete actions
/// </summary>
public class UserIsInRoleAttribute : ActionFilterAttribute
{
    public AccountTypeExternal roleName { get; set; }
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var externalUser = new ExternalApplicant(context.HttpContext.User);

        if (externalUser.AccountType != roleName)
        {
            context.Result = new RedirectToActionResult("Index", "Home", new object());
        }
    }
}