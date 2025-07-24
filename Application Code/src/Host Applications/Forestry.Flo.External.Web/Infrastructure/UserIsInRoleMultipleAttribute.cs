using Forestry.Flo.External.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Mandates that the user account has a one of multiple roles before a user can complete actions, otherwise it will return them to the homepage
/// </summary>
public class UserIsInRoleMultipleAttribute : ActionFilterAttribute
{
    public UserIsInRoleMultipleAttribute(AccountTypeExternal[] roleNames)
    {
        RoleNames = roleNames;
    }

    public AccountTypeExternal[] RoleNames { get; set; }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var externalUser = new ExternalApplicant(context.HttpContext.User);
        if (RoleNames != null)
        {
            var roleNames = RoleNames.ToList();
            if (!roleNames.Contains((AccountTypeExternal)externalUser.AccountType))
            {
                context.Result = new RedirectToActionResult("Index", "Home", new object());
            }
        }
    }
}