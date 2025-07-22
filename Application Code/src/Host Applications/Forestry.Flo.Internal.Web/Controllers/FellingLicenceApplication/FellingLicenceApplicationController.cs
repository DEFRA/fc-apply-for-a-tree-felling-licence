using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
public class FellingLicenceApplicationController() : Controller
{
    private readonly List<BreadCrumb> _breadCrumbsRoot = [new("Open applications", "Home", "Index", null)];

    [HttpGet]
    public async Task<IActionResult> ApplicationSummary(
        Guid id,
        [FromServices] FellingLicenceApplicationUseCase applicationUseCase,
        CancellationToken cancellationToken)
    {
        var model = await applicationUseCase.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            id,
            new InternalUser(User),
            cancellationToken);

        if (model.HasNoValue)
        {
            return RedirectToAction("Error", "Home");
        }
        model.Value.Breadcrumbs = new BreadcrumbsModel
        {
            CurrentPage = model.Value.FellingLicenceApplicationSummary!.ApplicationReference,
            Breadcrumbs = new List<BreadCrumb>() { new("Open applications", "Home", "Index", null) }
        };

        return View(model.Value);
    }

    [HttpGet]
    public async Task<IActionResult> ReopenWithdrawnApplication(
        Guid id,
        [FromServices] FellingLicenceApplicationUseCase applicationUseCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (user.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        var (_, isFailure, model) = await applicationUseCase.RetrieveReopenWithdrawnApplicationModelAsync(
            id, 
            Url.Action("ReopenWithdrawnApplication", "FellingLicenceApplication", new {id})!,
            cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        SetBreadcrumbs(model, model.FellingLicenceApplicationSummary!.ApplicationReference);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReopenWithdrawnApplication(
        ReopenWithdrawnApplicationModel model,
        [FromServices] RevertApplicationFromWithdrawnUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (user.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        var result =
            await useCase.RevertApplicationFromWithdrawnAsync(
                user,
                model.ApplicationId,
                cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        this.AddConfirmationMessage("Application reopened successfully");
        return RedirectToAction("Index", "Home");
    }

    private void SetBreadcrumbs(PageWithBreadcrumbsViewModel model, string currentPage)
    {
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = currentPage
        };
    }
}

