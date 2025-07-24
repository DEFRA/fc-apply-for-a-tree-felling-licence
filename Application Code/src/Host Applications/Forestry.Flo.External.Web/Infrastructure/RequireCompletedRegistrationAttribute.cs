using Forestry.Flo.External.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Mandates that the user account is in a completed state before a user can complete actions
/// pertinent to a felling licence application.
/// </summary>
public class RequireCompletedRegistrationAttribute : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var externalUser = new ExternalApplicant(context.HttpContext.User);

        if (externalUser.HasCompletedAccountRegistration == false)
        {
            // registered account information incomplete to allow us to continue, so we redirect the user to ask them politely to fill in the required data
            context.Result = new RedirectToActionResult("RegisterAccountType", "Account", new object());
            return;
        }
    }
}