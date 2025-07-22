using Forestry.Flo.External.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Mandates that a user is an agent user or agent administrator
/// </summary>
public class UserIsAgentOrAgentAdministratorAttribute : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var externalUser = new ExternalApplicant(context.HttpContext.User);

        if(externalUser.AccountType != AccountTypeExternal.Agent 
           && externalUser.AccountType != AccountTypeExternal.AgentAdministrator
           && externalUser.AccountType != AccountTypeExternal.FcUser)
        {
            // user is not an agent user or administrator, so we redirect the user to the home page.
            context.Result = new RedirectToActionResult("Index", "Home", new object());
            return;
        }
    }
}