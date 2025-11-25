using Forestry.Flo.External.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Forestry.Flo.External.Web.Infrastructure;

public sealed class EditingAllowedAttribute : ActionFilterAttribute
{
    private const string DefaultApplicationIdKey = "applicationId";

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var useCase = services.GetRequiredService<CreateFellingLicenceApplicationUseCase>();

        if (!TryGetApplicationId(context, out var applicationId))
        {
            await base.OnActionExecutionAsync(context, next);
            return;
        }
        var user = new ExternalApplicant(context.HttpContext.User);

        var isApplicationEditable = await useCase.EnsureApplicationIsEditable(applicationId, user, context.HttpContext.RequestAborted);
        if (isApplicationEditable.IsFailure)
        {
            context.Result = new RedirectToActionResult(
            nameof(Controllers.FellingLicenceApplicationController.ApplicationSummary),
            "FellingLicenceApplication",
            new { applicationId });
            return;
        }

        await base.OnActionExecutionAsync(context, next);
    }

    private static bool TryGetApplicationId(ActionExecutingContext context, out Guid applicationId)
    {
        applicationId = Guid.Empty;
        if (context.ActionArguments.TryGetValue(DefaultApplicationIdKey, out var arg) && arg is Guid g)
        {
            applicationId = g;
            return true;
        }
        if (context.RouteData.Values.TryGetValue(DefaultApplicationIdKey, out var routeVal)
        && routeVal is string routeStr
        && Guid.TryParse(routeStr, out var fromRoute))
        {
            applicationId = fromRoute;
            return true;
        }
        return false;
    }
}
