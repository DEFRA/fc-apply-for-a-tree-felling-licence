using FluentValidation;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.PawsDesignations;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

public partial class FellingLicenceApplicationController
{
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> PawsCheck(
        Guid applicationId,
        Guid? compartmentDesignationsId,
        bool? returnToApplicationSummary,
        [FromServices] ICollectPawsDataUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var viewModel = await useCase.GetPawsDesignationsViewModelAsync(
            user, applicationId, compartmentDesignationsId, cancellationToken);

        if (viewModel.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = viewModel.Value.ApplicationSummary.WoodlandOwnerId;
        ViewBag.ApplicationSummary = viewModel.Value.ApplicationSummary;

        SetTaskBreadcrumbs(viewModel.Value);

        viewModel.Value.ReturnToApplicationSummary = returnToApplicationSummary ?? false;

        return View(viewModel.Value);
    }

    [HttpPost]
    [EditingAllowed]
    public async Task<IActionResult> PawsCheck(
        Guid applicationId,
        bool? returnToApplicationSummary,
        PawsDesignationsViewModel model,
        [FromServices] ICollectPawsDataUseCase useCase,
        [FromServices] IValidator<PawsDesignationsViewModel> validator,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        ValidateModel(model, validator);

        if (!ModelState.IsValid)
        {
            var reloadModel = await useCase.GetPawsDesignationsViewModelAsync(
                user, applicationId, model.CompartmentDesignation.Id, cancellationToken);

            if (reloadModel.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            reloadModel.Value.CompartmentDesignation.ProportionBeforeFelling = model.CompartmentDesignation.ProportionBeforeFelling;
            reloadModel.Value.CompartmentDesignation.ProportionAfterFelling = model.CompartmentDesignation.ProportionAfterFelling;
            reloadModel.Value.CompartmentDesignation.IsRestoringCompartment = model.CompartmentDesignation.IsRestoringCompartment;
            reloadModel.Value.CompartmentDesignation.RestorationDetails = model.CompartmentDesignation.RestorationDetails;

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = reloadModel.Value.ApplicationSummary.WoodlandOwnerId;
            ViewBag.ApplicationSummary = reloadModel.Value.ApplicationSummary;

            SetTaskBreadcrumbs(reloadModel.Value);

            return View(reloadModel.Value);
        }

        var updateResult = await useCase.UpdatePawsDesignationsForCompartmentAsync(
            user, applicationId, model.CompartmentDesignation, cancellationToken);

        if (updateResult.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        if (returnToApplicationSummary is true)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId });
        }

        var nextCompartmentResult = await useCase.GetNextCompartmentDesignationsIdAsync(
            user, applicationId, model.CompartmentDesignation.Id, cancellationToken);

        if (nextCompartmentResult.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        if (nextCompartmentResult.Value.NextCompartmentDesignationsId != null)
        {
            return RedirectToAction(nameof(PawsCheck), new { applicationId, compartmentDesignationsId = nextCompartmentResult.Value.NextCompartmentDesignationsId });
        }

        return nextCompartmentResult.Value.RequiresEia is true
            ? RedirectToAction(nameof(FellingLicenceApplicationController.EnvironmentalImpactAssessment), new { applicationId })
            : RedirectToAction(nameof(FellingLicenceApplicationController.ConstraintsCheck), new { applicationId });
    }
}