using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public partial class WoodlandOfficerReviewController
{
    [HttpGet]
    public async Task<IActionResult> ConfirmTreeHealth(
        Guid id,
        [FromServices] IWoodlandOfficerTreeHealthCheckUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, model) = await useCase.GetTreeHealthCheckWoodlandOfficerViewModelAsync(
            id,
            cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        SetTreeHealthBreadcrumbs(model);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmTreeHealth(
        ConfirmTreeHealthIssuesViewModel model,
        [FromServices] IWoodlandOfficerTreeHealthCheckUseCase useCase,
        [FromServices] IValidator<ConfirmTreeHealthIssuesViewModel> validator,
        CancellationToken cancellationToken)
    {
        ValidateModel(model, validator);
        
        if (!ModelState.IsValid)
        {
            var (_, isReloadFailure, viewModel) = await useCase
                .GetTreeHealthCheckWoodlandOfficerViewModelAsync(model.ApplicationId, cancellationToken);

            if (isReloadFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            SetTreeHealthBreadcrumbs(viewModel);

            return View(viewModel);
        }

        var user = new InternalUser(User);

        var (_, isFailure) = await useCase
            .ConfirmTreeHealthIssuesAsync(model.ApplicationId, user, model.Confirmed!.Value, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return RedirectToAction(nameof(Index), "WoodlandOfficerReview", new { id = model.ApplicationId });
    }

    private void SetTreeHealthBreadcrumbs(FellingLicenceApplicationPageViewModel model)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Open applications", "Home", "Index", null),
            new BreadCrumb(model.FellingLicenceApplicationSummary.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary.Id.ToString()),
            new BreadCrumb("Woodland officer review", "WoodlandOfficerReview", "Index", model.FellingLicenceApplicationSummary.Id.ToString())
        };
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = "Confirm tree health"
        };
    }
}