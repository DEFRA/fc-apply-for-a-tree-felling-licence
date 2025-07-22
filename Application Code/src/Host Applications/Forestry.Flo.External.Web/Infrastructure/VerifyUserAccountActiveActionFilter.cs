using Forestry.Flo.External.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Forestry.Flo.External.Web.Infrastructure;

public class VerifyUserAccountActiveActionFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        var user = new ExternalApplicant(context.HttpContext.User);

        if (context.HttpContext.Request.Path == "/Home/AccountError" || context.HttpContext.Request.Path == "/Home/Logout")
        {
            return;
        }

        if (user.IsLoggedIn && user.IsDeactivatedAccount)
        {
            context.Result = new RedirectToActionResult("AccountError", "Home", new object());
        }
    }
}