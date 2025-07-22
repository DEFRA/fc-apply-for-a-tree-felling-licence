using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[TypeFilter(typeof(ApplicationExceptionFilter))]
public class LinkedUsersController : Controller
{
    private readonly List<BreadCrumb> _breadCrumbsRoot = new List<BreadCrumb>
    {
        new("Home", "Home", "Index", null),
    };

    [HttpGet]
    [UserIsInRoleMultiple(new[]
    {
        AccountTypeExternal.WoodlandOwnerAdministrator, 
        AccountTypeExternal.AgentAdministrator,
        AccountTypeExternal.FcUser
    })]
    public async Task<IActionResult> WoodlandOwnerUsers(
        [FromServices] ListWoodlandOwnerUsersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (!user.HasSelectedAgentWoodlandOwner)
        {
            this.AddErrorMessage("No woodland owner selected");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var woodlandOwnerId = Guid.Parse(user.WoodlandOwnerId!);
        var model = await useCase.RetrieveListOfWoodlandOwnerUsersAsync(user, woodlandOwnerId,  cancellationToken);
        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not load the list of users for woodland owner.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        SetBreadcrumbs(model.Value, user, "Users");
        return View(model.Value);
    }
    
    private void SetBreadcrumbs(PageWithBreadcrumbsViewModel model, ExternalApplicant user, string currentPage)
    {
        var breadCrumbs = _breadCrumbsRoot;

        if (user.IsFcUser || user.AccountType == AccountTypeExternal.AgentAdministrator)
        {
            breadCrumbs.Add(new BreadCrumb(user.WoodlandOwnerName, "Home", "WoodlandOwner", null));
        }

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = currentPage
        };
    }
}