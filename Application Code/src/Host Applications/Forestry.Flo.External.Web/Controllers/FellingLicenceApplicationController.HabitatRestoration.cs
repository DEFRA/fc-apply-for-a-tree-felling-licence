using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Forestry.Flo.External.Web.Controllers;

public partial class FellingLicenceApplicationController : Controller
{
    [EditingAllowed]
    public async Task<IActionResult> HabitatRestoration(
        Guid applicationId,
        bool? returnToApplicationSummary,
        [FromServices] CreateFellingLicenceApplicationUseCase createUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var appResult = await createUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (appResult.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var model = appResult.Value.HabitatRestoration;
        model.ApplicationSummary = appResult.Value.ApplicationSummary;
        model.ReturnToApplicationSummary = returnToApplicationSummary ?? false;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> HabitatRestoration(
        PriorityOpenHabitatsViewModel model,
        [FromServices] HabitatRestorationUseCase useCase,
        [FromServices] CreateFellingLicenceApplicationUseCase createUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (!ModelState.IsValid)
        {
            var invalidAppResult = await createUseCase.RetrieveFellingLicenceApplication(user, model.ApplicationId, cancellationToken);
            if (invalidAppResult.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.ApplicationSummary = invalidAppResult.Value.ApplicationSummary;
            return View(model);
        }

        var save = await useCase.SetHabitatRestorationStatus(user, model.ApplicationId, model.IsPriorityOpenHabitat, cancellationToken);
        if (save.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var appResult = await createUseCase.RetrieveFellingLicenceApplication(user, model.ApplicationId, cancellationToken);
        if (appResult.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (model.ReturnToApplicationSummary && appResult.Value.IsComplete)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId = model.ApplicationId });
        }

        if (model.IsPriorityOpenHabitat.HasValue && model.IsPriorityOpenHabitat.Value)
        {
            return RedirectToAction(nameof(HabitatCompartments), new { applicationId = model.ApplicationId });
        }

        // No habitat compartments to process - go to next required step
        if (appResult.Value.EnvironmentalImpactAssessment.StepRequiredForApplication)
        {
            return RedirectToAction(nameof(EnvironmentalImpactAssessment), new { model.ApplicationId });
        }

        if (appResult.Value.PawsAndIawp.StepRequiredForApplication)
        {
            return RedirectToAction(nameof(PawsCheck), new { model.ApplicationId });
        }

        return RedirectToAction(nameof(SupportingDocumentation), new { model.ApplicationId });
    }

    // GET action to select compartments for habitat restoration
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> HabitatCompartments(
        Guid applicationId,
        bool? returnToApplicationSummary,
        [FromServices] CreateFellingLicenceApplicationUseCase createUseCase,
        [FromServices] HabitatRestorationUseCase habitatUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var viewModel = await createUseCase.GetSelectCompartmentViewModel(
            applicationId, user, cancellationToken, true);
        if (viewModel.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = viewModel.Value.Application.WoodlandOwnerId;
        ViewBag.ApplicationSummary = viewModel.Value.Application.ApplicationSummary;

        // Use habitat restorations to determine initially selected compartments
        var selectedFromRestorations = await habitatUseCase.GetHabitatCompartmentIds(applicationId, user, cancellationToken);
        if (selectedFromRestorations.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var filteredCompartments = viewModel.Value.Compartments
            .Where(x => selectedFromRestorations.Value.ContainsKey(x.Id));

        var model = new HabitatCompartmentsModel
        {
            ApplicationId = viewModel.Value.Application.ApplicationSummary.Id,
            ApplicationReference = viewModel.Value.Application.ApplicationSummary.ApplicationReference,
            FellingLicenceStatus = viewModel.Value.Application.ApplicationSummary.Status,
            SelectedCompartmentIds = selectedFromRestorations.Value.Where(x => x.Value).Select(x => x.Key).ToList(),
            ReturnToApplicationSummary = returnToApplicationSummary ?? false
        };

        // Order compartments as per existing behavior
        ViewBag.Compartments = filteredCompartments.OrderByNameNumericOrAlpha().ToList();

        return View(model);
    }

    // POST from HabitatCompartments to proceed to habitat type selection
    [HttpPost]
    public async Task<IActionResult> HabitatCompartments(
        HabitatCompartmentsModel model,
        [FromServices] HabitatRestorationUseCase useCase,
        [FromServices] CreateFellingLicenceApplicationUseCase createUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var viewModel = await createUseCase.GetSelectCompartmentViewModel(
            model.ApplicationId, user, cancellationToken, true);
        if (viewModel.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (!ModelState.IsValid)
        {
            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = viewModel.Value.Application.WoodlandOwnerId;
            ViewBag.ApplicationSummary = viewModel.Value.Application.ApplicationSummary;
            ViewBag.Compartments = viewModel.Value.Compartments.OrderByNameNumericOrAlpha().ToList();

            // Rehydrate initial model from viewModel
            var vm = new HabitatCompartmentsModel
            {
                ApplicationId = viewModel.Value.Application.ApplicationSummary.Id,
                ApplicationReference = viewModel.Value.Application.ApplicationSummary.ApplicationReference,
                SelectedCompartmentIds = model.SelectedCompartmentIds ?? new List<Guid>(),
                ReturnToApplicationSummary = model.ReturnToApplicationSummary
            };

            return View(vm);
        }

        // Capture existing selected habitat compartments before update
        var areAnyNewResult = await useCase.AreAnyNewHabitats(model.ApplicationId, model.SelectedCompartmentIds, user, cancellationToken);
        if (areAnyNewResult.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var update = await useCase.UpdateHabitatCompartments(model.ApplicationId, model.SelectedCompartmentIds, cancellationToken);
        if (update.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        // If any new compartments were added in this update, mark the habitat restoration task as started/completed
        if (areAnyNewResult.Value)
        {
            var save = await useCase.SetHabitatRestorationStatus(user, model.ApplicationId, true, cancellationToken);
            if (save.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }
            model.ReturnToApplicationSummary = false;
        }

        var next = await useCase.GetHabitatNextCompartment(model.ApplicationId, cancellationToken);
        if (next.HasNoValue)
        {
            if (model.ReturnToApplicationSummary)
            {
                return RedirectToAction(nameof(ApplicationSummary), new { applicationId = model.ApplicationId });
            }
            else
            {
                var nextCompartmentId = model.SelectedCompartmentIds?.FirstOrDefault();
                if (nextCompartmentId == Guid.Empty)
                {
                    return RedirectToAction(nameof(HabitatCompartments), new { applicationId = model.ApplicationId });
                }
                return RedirectToAction(nameof(HabitatType), new { applicationId = model.ApplicationId, compartmentId = nextCompartmentId });
            }
        }
        return RedirectToAction(nameof(HabitatType), new { applicationId = model.ApplicationId, compartmentId = next.Value });
    }

    // GET: HabitatType for a selected compartment
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> HabitatType(
        Guid applicationId,
        Guid compartmentId,
        bool? returnToApplicationSummary,
        [FromServices] HabitatRestorationUseCase habitatUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await habitatUseCase.GetHabitatTypeModel(applicationId, compartmentId, user, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Error);
            return View(new HabitatTypeModel { ApplicationId = applicationId, CompartmentId = compartmentId });
        }

        var model = result.Value;
        model.ReturnToApplicationSummary = returnToApplicationSummary ?? false;

        return View(model);
    }

    // POST: HabitatType should redirect to HabitatWoodlandSpecies
    [HttpPost]
    public async Task<IActionResult> HabitatType(
        HabitatTypeModel model,
        [FromServices] HabitatRestorationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var user = new ExternalApplicant(User);
            var result = await useCase.GetHabitatTypeModel(model.ApplicationId, model.CompartmentId, user, cancellationToken);
            if (result.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var viewModel = result.Value;
            viewModel.SelectedHabitatType = model.SelectedHabitatType;
            viewModel.OtherHabitatDescription = model.OtherHabitatDescription;
            viewModel.ReturnToApplicationSummary = model.ReturnToApplicationSummary;

            return View(viewModel);
        }

        var update = await useCase.UpdateHabitatType(model.ApplicationId, model.CompartmentId, model.SelectedHabitatType!.Value, model.OtherHabitatDescription, cancellationToken);
        if (update.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (model.ReturnToApplicationSummary)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId = model.ApplicationId });
        }

        return RedirectToAction(nameof(HabitatWoodlandSpecies), new { applicationId = model.ApplicationId, compartmentId = model.CompartmentId });
    }

    // GET: HabitatWoodlandSpecies for a selected compartment (renamed from WoodlandSpeciesType)
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> HabitatWoodlandSpecies(
        Guid applicationId,
        Guid compartmentId,
        bool? returnToApplicationSummary,
        [FromServices] HabitatRestorationUseCase habitatUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await habitatUseCase.GetWoodlandSpeciesTypeModel(applicationId, compartmentId, user, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Error);
            return View(new WoodlandSpeciesTypeModel { ApplicationId = applicationId, CompartmentId = compartmentId });
        }

        var model = result.Value;
        model.ReturnToApplicationSummary = returnToApplicationSummary ?? false;

        return View(model);
    }

    // POST: HabitatWoodlandSpecies -> HabitatNativeBroadleaf 
    [HttpPost]
    public async Task<IActionResult> HabitatWoodlandSpecies(
        WoodlandSpeciesTypeModel model,
        [FromServices] HabitatRestorationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var user = new ExternalApplicant(User);
            var result = await useCase.GetWoodlandSpeciesTypeModel(model.ApplicationId, model.CompartmentId, user, cancellationToken);
            if (result.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var viewModel = result.Value;
            viewModel.SelectedSpeciesType = model.SelectedSpeciesType;
            viewModel.ReturnToApplicationSummary = model.ReturnToApplicationSummary;

            return View(viewModel);
        }

        var update = await useCase.UpdateWoodlandSpecies(model.ApplicationId, model.CompartmentId, model.SelectedSpeciesType!.Value, cancellationToken);
        if (update.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (model.SelectedSpeciesType.Value == WoodlandSpeciesType.BroadleafWoodland)
        {
            return RedirectToAction(nameof(HabitatNativeBroadleaf), new { applicationId = model.ApplicationId, compartmentId = model.CompartmentId, model.ReturnToApplicationSummary });
        }

        if (model.ReturnToApplicationSummary)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId = model.ApplicationId });
        }

        return RedirectToAction(nameof(HabitatProductiveWoodland), new { applicationId = model.ApplicationId, compartmentId = model.CompartmentId });
    }

    // GET: HabitatNativeBroadleaf
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> HabitatNativeBroadleaf(
        Guid applicationId,
        Guid compartmentId,
        bool? returnToApplicationSummary,
        [FromServices] HabitatRestorationUseCase habitatUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await habitatUseCase.GetHabitatNativeBroadleafModel(applicationId, compartmentId, user, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Error);
            return View(new HabitatNativeBroadleafModel { ApplicationId = applicationId, CompartmentId = compartmentId });
        }

        var model = result.Value;
        model.ReturnToApplicationSummary = returnToApplicationSummary ?? false;

        return View(model);
    }

    // POST: HabitatNativeBroadleaf -> HabitatProductiveWoodland
    [HttpPost]
    public async Task<IActionResult> HabitatNativeBroadleaf(
        HabitatNativeBroadleafModel model,
        [FromServices] HabitatRestorationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var user = new ExternalApplicant(User);
            var result = await useCase.GetHabitatNativeBroadleafModel(model.ApplicationId, model.CompartmentId, user, cancellationToken);
            if (result.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var viewModel = result.Value;
            viewModel.IsNativeBroadleaf = model.IsNativeBroadleaf;
            viewModel.ReturnToApplicationSummary = model.ReturnToApplicationSummary;

            return View(viewModel);
        }

        var update = await useCase.UpdateNativeBroadleaf(model.ApplicationId, model.CompartmentId, model.IsNativeBroadleaf, cancellationToken);
        if (update.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (model.ReturnToApplicationSummary)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId = model.ApplicationId });
        }

        return RedirectToAction(nameof(HabitatProductiveWoodland), new { applicationId = model.ApplicationId, compartmentId = model.CompartmentId });
    }

    // GET: HabitatProductiveWoodland
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> HabitatProductiveWoodland(
        Guid applicationId,
        Guid compartmentId,
        bool? returnToApplicationSummary,
        [FromServices] HabitatRestorationUseCase habitatUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await habitatUseCase.GetHabitatProductiveWoodlandModel(applicationId, compartmentId, user, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Error);
            return View(new HabitatProductiveWoodlandModel { ApplicationId = applicationId, CompartmentId = compartmentId });
        }

        var model = result.Value;
        model.ReturnToApplicationSummary = returnToApplicationSummary ?? false;

        return View(model);
    }

    // POST: HabitatProductiveWoodland -> next step based on selection
    [HttpPost]
    public async Task<IActionResult> HabitatProductiveWoodland(
        HabitatProductiveWoodlandModel model,
        [FromServices] HabitatRestorationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var user = new ExternalApplicant(User);
            var result = await useCase.GetHabitatProductiveWoodlandModel(model.ApplicationId, model.CompartmentId, user, cancellationToken);
            if (result.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var viewModel = result.Value;
            viewModel.IsProductiveWoodland = model.IsProductiveWoodland;
            viewModel.ReturnToApplicationSummary = model.ReturnToApplicationSummary;

            return View(viewModel);
        }

        var update = await useCase.UpdateProductiveWoodland(model.ApplicationId, model.CompartmentId, model.IsProductiveWoodland, cancellationToken);
        if (update.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (model.IsProductiveWoodland.HasValue && model.IsProductiveWoodland.Value)
        {
            return RedirectToAction(nameof(HabitatFelledEarly), new { applicationId = model.ApplicationId, compartmentId = model.CompartmentId, model.ReturnToApplicationSummary });
        }

        if (model.ReturnToApplicationSummary)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId = model.ApplicationId });
        }

        return RedirectToAction(nameof(HabitatFinalCheck), new { applicationId = model.ApplicationId, compartmentId = model.CompartmentId });
    }

    // GET: HabitatFelledEarly
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> HabitatFelledEarly(
        Guid applicationId,
        Guid compartmentId,
        bool? returnToApplicationSummary,
        [FromServices] HabitatRestorationUseCase habitatUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await habitatUseCase.GetHabitatFelledEarlyModel(applicationId, compartmentId, user, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Error);
            return View(new HabitatFelledEarlyModel { ApplicationId = applicationId, CompartmentId = compartmentId });
        }

        var model = result.Value;
        model.ReturnToApplicationSummary = returnToApplicationSummary ?? false;

        return View(model);
    }

    // POST: HabitatFelledEarly -> ApplicationTaskList
    [HttpPost]
    public async Task<IActionResult> HabitatFelledEarly(
        HabitatFelledEarlyModel model,
        [FromServices] HabitatRestorationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var user = new ExternalApplicant(User);
            var result = await useCase.GetHabitatFelledEarlyModel(model.ApplicationId, model.CompartmentId, user, cancellationToken);
            if (result.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var viewModel = result.Value;
            viewModel.IsFelledEarly = model.IsFelledEarly;
            viewModel.ReturnToApplicationSummary = model.ReturnToApplicationSummary;

            return View(viewModel);
        }

        var update = await useCase.UpdateFelledEarly(model.ApplicationId, model.CompartmentId, model.IsFelledEarly, cancellationToken);
        if (update.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (model.ReturnToApplicationSummary)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId = model.ApplicationId });
        }

        return RedirectToAction(nameof(HabitatFinalCheck), new { applicationId = model.ApplicationId, compartmentId = model.CompartmentId });
    }

    // GET: HabitatFinalCheck - mark current compartment complete and move to next incomplete or task list
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> HabitatFinalCheck(
        Guid applicationId,
        Guid compartmentId,
        [FromServices] HabitatRestorationUseCase useCase,
        [FromServices] CreateFellingLicenceApplicationUseCase createUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var update = await useCase.UpdateCompleted(applicationId, compartmentId, true, cancellationToken);
        if (update.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var next = await useCase.GetHabitatNextCompartment(applicationId, cancellationToken);
        if (next.HasNoValue)
        {
            // Set task completed
            var save = await useCase.SetHabitatRestorationStatus(user, applicationId, true, cancellationToken);
            if (save.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }
            // No more habitat compartments to process - go to next required step
            var appResult = await createUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
            if (appResult.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            if (appResult.Value.EnvironmentalImpactAssessment.StepRequiredForApplication)
            {
                return RedirectToAction(nameof(EnvironmentalImpactAssessment), new { applicationId });
            }

            if (appResult.Value.PawsAndIawp.StepRequiredForApplication)
            {
                return RedirectToAction(nameof(PawsCheck), new { applicationId });
            }

            return RedirectToAction(nameof(SupportingDocumentation), new { applicationId });
        }

        return RedirectToAction(nameof(HabitatType), new { applicationId, compartmentId = next.Value });
    }
}
