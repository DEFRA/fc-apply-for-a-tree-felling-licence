using Forestry.Flo.External.Web.Exceptions;
using Forestry.Flo.External.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Forestry.Flo.External.Web.Infrastructure;

public class ApplicationExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApplicationExceptionFilter> _logger;

    public ApplicationExceptionFilter(ILogger<ApplicationExceptionFilter> logger)
    {
        _logger = logger;
    }
    
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            
            case NotExistingUserAccountException:
            {
                _logger.LogError("User account was not found in the database, error: {Error}", context.Exception.Message);
                context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter()
                    .GetResult();
                context.HttpContext.Response.Headers.Add("Clear-Site-Data", "\"cookies\", \"storage\", \"cache\"");

                var user = new ExternalApplicant(context.HttpContext.User);
                if (!user.IsAnInvitedUser)
                {
                    context.Result = new RedirectToRouteResult(
                        new RouteValueDictionary
                            (new { action = "RegisterAccountType", controller = "Account" }));
                    context.ExceptionHandled = true;
                }
                break;
            }
        }
    }
}