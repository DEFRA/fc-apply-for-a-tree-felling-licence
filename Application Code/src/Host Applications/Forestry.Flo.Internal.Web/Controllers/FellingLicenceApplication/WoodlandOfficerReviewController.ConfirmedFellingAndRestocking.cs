using CSharpFunctionalExtensions;
using FileSignatures.Formats;
using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public partial class WoodlandOfficerReviewController
{
    private const string AmendFellingPageName = "Amend confirmed felling details";
    private const string AddFellingPageName = "Add confirmed felling details";
    private const string CombinedPageName = "Confirmed felling and restocking";

    [HttpGet]
    public async Task<IActionResult> AmendConfirmedFellingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid confirmedFellingDetailsId,
        [FromServices] ConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, confirmedFellingRestockingDetailsModel) = await useCase.GetConfirmedFellingAndRestockingDetailsAsync(
            applicationId,
            new InternalUser(User),
            cancellationToken, 
            AmendFellingPageName);

        const string retrievalErrorMessage = "Could not retrieve confirmed felling details";

        if (isFailure)
        {
            this.AddErrorMessage(retrievalErrorMessage);
            return RedirectToAction("Index", new { applicationId });
        }

        var specificFellingDetail = 
            confirmedFellingRestockingDetailsModel.Compartments.FirstOrDefault(x => 
                x.ConfirmedFellingDetails.Any(y => y.ConfirmedFellingDetailsId == confirmedFellingDetailsId));

        if (specificFellingDetail is null)
        {
            this.AddErrorMessage(retrievalErrorMessage);
            return RedirectToAction("Index", new { applicationId });
        }

        var model = new AmendConfirmedFellingDetailsViewModel
        {
            FellingLicenceApplicationSummary = confirmedFellingRestockingDetailsModel.FellingLicenceApplicationSummary,
            ApplicationId = confirmedFellingRestockingDetailsModel.ApplicationId,
            ConfirmedFellingRestockingDetails =
                new IndividualConfirmedFellingRestockingDetailModel
                {
                    CompartmentId = specificFellingDetail.CompartmentId,
                    CompartmentNumber = specificFellingDetail.CompartmentNumber,
                    SubCompartmentName = specificFellingDetail.SubCompartmentName,
                    TotalHectares = specificFellingDetail.TotalHectares,
                    Designation = specificFellingDetail.Designation,
                    ConfirmedFellingDetails =
                        specificFellingDetail.ConfirmedFellingDetails.First(x =>
                            x.ConfirmedFellingDetailsId == confirmedFellingDetailsId),
                },
            ConfirmedFellingAndRestockingComplete =
                confirmedFellingRestockingDetailsModel.ConfirmedFellingAndRestockingComplete,
            Breadcrumbs = confirmedFellingRestockingDetailsModel.Breadcrumbs
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AmendConfirmedFellingDetails(
        AmendConfirmedFellingDetailsViewModel model,
        [FromServices] ConfirmedFellingAndRestockingDetailsUseCase useCase,
        [FromServices] IValidator<AmendConfirmedFellingDetailsViewModel> validator,
        CancellationToken cancellationToken)
    {
        var speciesList = model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConfirmedFellingSpecies.ToList();
        foreach (var species in model.Species.Where(species => speciesList.All(x => x.Species != species.Key)))
        {
            speciesList.Add(new ConfirmedFellingSpeciesModel
            {
                Species = species.Key,
                Deleted = false
            });
        }

        foreach (var deletedSpecies in model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConfirmedFellingSpecies
                     .Where(x => model.Species.ContainsKey(x.Species ?? "") is false))
        {
            deletedSpecies.Deleted = true;
        }

        ValidateModel(model, validator);

        if (ModelState.IsValid is false)
        {

            var (_, retrievalFailure, confirmedFellingRestockingDetailsModel) = await useCase.GetConfirmedFellingAndRestockingDetailsAsync(
                model.ApplicationId,
                new InternalUser(User),
                cancellationToken,
                AmendFellingPageName);

            if (retrievalFailure)
            {
                this.AddErrorMessage("Could not retrieve felling details");
                return RedirectToAction("Index", new { id = model.ApplicationId });
            }

            model.FellingLicenceApplicationSummary = confirmedFellingRestockingDetailsModel.FellingLicenceApplicationSummary;
            model.Breadcrumbs = confirmedFellingRestockingDetailsModel.Breadcrumbs;

            return View(model);
        }

        var (_, isFailure) = await useCase.SaveConfirmedFellingDetailsAsync(
            model, 
            new InternalUser(User),
            cancellationToken);

        if (isFailure)
        {
            this.AddErrorMessage("Could not save confirmed felling details");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        // todo: return to the summary page when implemented
        return RedirectToAction("Index", new { id = model.ApplicationId });
    }


    [HttpGet]
    public async Task<IActionResult> AddConfirmedFellingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid compartmentId,
        [FromServices] ConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, confirmedFellingRestockingDetailsModel) = await useCase.GetConfirmedFellingAndRestockingDetailsAsync(
            applicationId,
            new InternalUser(User),
            cancellationToken,
            AddFellingPageName);

        const string retrievalErrorMessage = "Could not retrieve confirmed felling details";

        if (isFailure)
        {
            this.AddErrorMessage(retrievalErrorMessage);
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { applicationId });
        }

        var specificCompartment =
            confirmedFellingRestockingDetailsModel.Compartments.FirstOrDefault(x => x.CompartmentId == compartmentId);

        if (specificCompartment is null)
        {
            this.AddErrorMessage(retrievalErrorMessage);
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { applicationId });
        }

        var model = new AddNewConfirmedFellingDetailsViewModel
        {
            FellingLicenceApplicationSummary = confirmedFellingRestockingDetailsModel.FellingLicenceApplicationSummary,
            ApplicationId = confirmedFellingRestockingDetailsModel.ApplicationId,
            ConfirmedFellingRestockingDetails = 
                new NewConfirmedFellingDetailModel
                {
                    CompartmentId = specificCompartment.CompartmentId,
                    CompartmentNumber = specificCompartment.CompartmentNumber,
                    SubCompartmentName = specificCompartment.SubCompartmentName,
                    TotalHectares = specificCompartment.TotalHectares,
                    Designation = specificCompartment.Designation,
                    ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel()
                },
            Breadcrumbs = confirmedFellingRestockingDetailsModel.Breadcrumbs
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddConfirmedFellingDetails(
        AddNewConfirmedFellingDetailsViewModel model,
        [FromServices] ConfirmedFellingAndRestockingDetailsUseCase useCase,
        [FromServices] IValidator<AddNewConfirmedFellingDetailsViewModel> validator,
        CancellationToken cancellationToken)
    {
        var speciesList = model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConfirmedFellingSpecies.ToList();
        foreach (var species in model.Species.Where(species => speciesList.All(x => x.Species != species.Key)))
        {
            speciesList.Add(new ConfirmedFellingSpeciesModel
            {
                Species = species.Key,
                Deleted = false
            });
        }

        foreach (var deletedSpecies in model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConfirmedFellingSpecies
                     .Where(x => model.Species.ContainsKey(x.Species ?? "") is false))
        {
            deletedSpecies.Deleted = true;
        }

        ValidateModel(model, validator);

        if (ModelState.IsValid is false)
        {

            var (_, retrievalFailure, confirmedFellingRestockingDetailsModel) = await useCase.GetConfirmedFellingAndRestockingDetailsAsync(
                model.ApplicationId, 
                new InternalUser(User),
                cancellationToken,
                AddFellingPageName);

            if (retrievalFailure)
            {
                this.AddErrorMessage("Could not retrieve felling details");
                return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = model.ApplicationId });
            }

            model.FellingLicenceApplicationSummary = confirmedFellingRestockingDetailsModel.FellingLicenceApplicationSummary; 
            model.Breadcrumbs = confirmedFellingRestockingDetailsModel.Breadcrumbs;

            return View(model);
        }

        var (_, isFailure) = await useCase.SaveConfirmedFellingDetailsAsync(
            model,
            new InternalUser(User),
            cancellationToken);

        if (isFailure)
        {
            this.AddErrorMessage("Could not save confirmed felling details");
        }
        else
        {
            this.AddConfirmationMessage("Confirmed felling details amended");
        }

        return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> SelectFellingCompartment(
        [FromQuery] Guid applicationId,
        [FromServices] ConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, viewModel) = await useCase.GetSelectableFellingCompartmentsAsync(
            applicationId,
            cancellationToken);

        if (isFailure)
        {
            this.AddErrorMessage("Could not retrieve selectable compartments");
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
        }

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SelectFellingCompartment(
        SelectFellingCompartmentModel model,
        [FromServices] ConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            return RedirectToAction(nameof(AddConfirmedFellingDetails),
                new { applicationId = model.ApplicationId, compartmentId = model.SelectedCompartmentId });
        }

        var (_, isFailure, viewModel) = await useCase.GetSelectableFellingCompartmentsAsync(
            model.ApplicationId,
            cancellationToken);

        if (isFailure)
        {
            this.AddErrorMessage("Could not retrieve selectable compartments");
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = model.ApplicationId });
        }

        model.Breadcrumbs = viewModel.Breadcrumbs;
        model.SelectableCompartments = viewModel.SelectableCompartments;
        model.FellingLicenceApplicationSummary = viewModel.FellingLicenceApplicationSummary;

        return View(model);
    }


    [HttpGet]
    public async Task<IActionResult> ConfirmedFellingAndRestocking(
        Guid id,
        [FromServices] ConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var model = await useCase.GetConfirmedFellingAndRestockingDetailsAsync(
            id,
            user,
            cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model.Value);
    }

    public async Task<IActionResult> DeleteConfirmedFellingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid confirmedFellingDetailId,
        [FromServices] ConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure) = await useCase.DeleteConfirmedFellingDetailAsync(
            applicationId,
            confirmedFellingDetailId,
            new InternalUser(User),
            cancellationToken);

        if (isFailure)
        {
            this.AddErrorMessage("Could not delete confirmed felling details");
        }
        else
        {
            this.AddConfirmationMessage("Felling details deleted");
        }

        return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
    }
}