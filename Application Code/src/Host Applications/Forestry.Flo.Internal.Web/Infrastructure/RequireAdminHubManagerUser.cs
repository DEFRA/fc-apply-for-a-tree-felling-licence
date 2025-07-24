using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Forestry.Flo.Internal.Web.Infrastructure;

/// <summary>
/// Mandates that user is an Admin Hub Manager
/// </summary>
public class RequireAdminHubManagerUser : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var internalUser = new InternalUser(context.HttpContext.User);

        if (internalUser.AccountType != AccountTypeInternal.AdminHubManager)
        {
            // user is not an Admin Hub Manager, so we redirect the user to the home page to select functionality relevant to their access
            context.Result = new RedirectToActionResult("Index", "Home", new object());
            return;
        }
    }
}