using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Internal.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public partial class WoodlandOfficerReviewController
{
    private const string AmendFellingPageName = "Amend confirmed felling details";
    private const string AddFellingPageName = "Add confirmed felling details";
    private const string CombinedPageName = "Confirmed felling and restocking";
    private const string AmendRestockingPageName = "Amend confirmed restocking details";

    [HttpGet]
    public async Task<IActionResult> AmendConfirmedFellingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid confirmedFellingDetailsId,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
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
                    SubmittedFlaPropertyCompartmentId = specificFellingDetail.SubmittedFlaPropertyCompartmentId,
                    TotalHectares = specificFellingDetail.TotalHectares,
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
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
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

        return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = model.ApplicationId });
    }


    [HttpGet]
    public async Task<IActionResult> AddConfirmedFellingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid compartmentId,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
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
                    ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel()
                },
            Breadcrumbs = confirmedFellingRestockingDetailsModel.Breadcrumbs
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddConfirmedFellingDetails(
        AddNewConfirmedFellingDetailsViewModel model,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
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
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
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
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
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
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
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

        var validationResult = await new ConfirmedFellingAndRestockingCrossValidator().ValidateAsync(model.Value, cancellationToken);
        foreach (var error in validationResult.Errors.DistinctBy(x => x.ErrorMessage))
        {
            ModelState.AddModelError(
                error.FormattedMessagePlaceholderValues["PropertyName"]?.ToString() ?? error.PropertyName, 
                error.ErrorMessage);
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SendAmendmentsToApplicant(
        Guid applicationId,
        string? amendmentReason,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase cfrUseCase,
        [FromServices] IWoodlandOfficerReviewUseCase woReviewUseCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (string.IsNullOrWhiteSpace(amendmentReason))
        {
            this.AddErrorMessage("Enter a reason for the amendments");
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
        }

        var (_, isFailure, error) = await cfrUseCase.SendAmendmentsToApplicant(
            applicationId,
            user,
            amendmentReason,
            cancellationToken);

        if (isFailure)
        {
            this.AddErrorMessage("Could not send felling and restocking amendments");
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
        }
        this.AddConfirmationMessage("Amendments are sent successfully to the applicant");
        return RedirectToAction("ConfirmedFellingAndRestocking", "WoodlandOfficerReview", new { id = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> MakeFurtherAmendments(
        Guid applicationId,
        Guid amendmentReviewId,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase cfrUseCase,
        [FromServices] IWoodlandOfficerReviewUseCase woReviewUseCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var result = await cfrUseCase.MakeFurtherAmendments(
            user,
            amendmentReviewId,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Could not close current felling and restocking amendment review");
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
        }

        this.AddConfirmationMessage("The current amendment review was closed. A new one can be sent to the applicant.");
        return RedirectToAction("ConfirmedFellingAndRestocking", "WoodlandOfficerReview", new { id = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmFellingAndRestocking(
        Guid applicationId,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase cfrUseCase,
        [FromServices] IWoodlandOfficerReviewUseCase woReviewUseCase,
        [FromServices] IValidator<ConfirmedFellingRestockingDetailsModel> validator,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var model = await cfrUseCase.GetConfirmedFellingAndRestockingDetailsAsync(
            applicationId,
            user,
            cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        var validationResult = await validator.ValidateAsync(model.Value, cancellationToken);
        if (!validationResult.IsValid)
        {
            return RedirectToAction("ConfirmedFellingAndRestocking", new { id = applicationId });
        }

        var (_, isFailure, error) = await woReviewUseCase.CompleteConfirmedFellingAndRestockingDetailsAsync(
            applicationId,
            user,
            cancellationToken);

        if (isFailure)
        {
            this.AddErrorMessage("Could not confirm felling and restocking details");
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
        }

        return RedirectToAction("Index", "WoodlandOfficerReview", new { id = applicationId });
    }

    public async Task<IActionResult> DeleteConfirmedFellingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid confirmedFellingDetailId,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
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

    public async Task<IActionResult> DeleteConfirmedRestockingDetails(
    [FromQuery] Guid applicationId,
    [FromQuery] Guid confirmedRestockingDetailId,
    [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
    CancellationToken cancellationToken)
    {
        var (_, isFailure) = await useCase.DeleteConfirmedRestockingDetailAsync(
            applicationId,
            confirmedRestockingDetailId,
            new InternalUser(User),
            cancellationToken);

        if (isFailure)
        {
            this.AddErrorMessage("Could not delete confirmed restocking details");
        }
        else
        {
            this.AddConfirmationMessage("Restocking details deleted");
        }

        return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
    }

    public async Task<IActionResult> RevertAmendedConfirmedFellingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid proposedFellingDetailsId,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        ArgumentNullException.ThrowIfNull(user.UserAccountId);

        var result = await useCase.RevertConfirmedFellingDetailAmendmentsAsync(
            applicationId,
            proposedFellingDetailsId,
            user,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Could not revert confirmed felling details amendments");
            return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
        }

        this.AddConfirmationMessage("Reverted confirmed felling details amendments successfully");
        return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = applicationId });
    }

    [HttpGet]
    public async Task<IActionResult> AmendConfirmedRestockingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid confirmedFellingDetailsId,
        [FromQuery] Guid confirmedRestockingDetailsId,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await HandleGetConfirmedRestockingDetails(applicationId, confirmedFellingDetailsId, confirmedRestockingDetailsId, useCase, cancellationToken, isAdd: false);
    }

    [HttpGet]
    public async Task<IActionResult> AddConfirmedRestockingDetails(
        [FromQuery] Guid applicationId,
        [FromQuery] Guid compartmentId,
        [FromQuery] Guid fellingDetailsId,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        return await HandleGetConfirmedRestockingDetails(applicationId, fellingDetailsId, null, useCase, cancellationToken, isAdd: true);
    }

    private async Task<IActionResult> HandleGetConfirmedRestockingDetails(
        Guid applicationId,
        Guid fellingDetailsId,
        Guid? restockingDetailsId,
        IConfirmedFellingAndRestockingDetailsUseCase useCase,
        CancellationToken cancellationToken,
        bool isAdd)
    {
        var user = new InternalUser(User);
        var (_, isFailure, confirmedFellingRestockingDetailsModel) = await useCase.GetConfirmedFellingAndRestockingDetailsAsync(applicationId, user, cancellationToken, AmendRestockingPageName);

        const string retrievalErrorMessage = "Could not retrieve confirmed restocking details";

        if (isFailure)
        {
            this.AddErrorMessage(retrievalErrorMessage);
            return RedirectToAction("ConfirmedFellingAndRestocking", new { applicationId });
        }


        if (isAdd)
        {
            var specificFellingDetail = confirmedFellingRestockingDetailsModel.Compartments.FirstOrDefault(x => x.ConfirmedFellingDetails.Any(y => y.ConfirmedFellingDetailsId == fellingDetailsId));
            if (specificFellingDetail is null)
            {
                this.AddErrorMessage(retrievalErrorMessage);
                return RedirectToAction("ConfirmedFellingAndRestocking", new { applicationId });
            }
            var model = new AmendConfirmedRestockingDetailsViewModel
            {
                FellingLicenceApplicationSummary = confirmedFellingRestockingDetailsModel.FellingLicenceApplicationSummary,
                ApplicationId = confirmedFellingRestockingDetailsModel.ApplicationId,
                SubmittedFlaPropertyCompartments = confirmedFellingRestockingDetailsModel.SubmittedFlaPropertyCompartments,
                ConfirmedFellingRestockingDetails =
                    new IndividualConfirmedRestockingDetailModel
                    {
                        CompartmentId = specificFellingDetail.CompartmentId,
                        CompartmentNumber = specificFellingDetail.CompartmentNumber,
                        SubCompartmentName = specificFellingDetail.SubCompartmentName,
                        SubmittedFlaPropertyCompartmentId = specificFellingDetail.SubmittedFlaPropertyCompartmentId,
                        TotalHectares = specificFellingDetail.TotalHectares,
                        ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel()
                        {
                            ConfirmedFellingDetailsId = fellingDetailsId,
                            OperationType = specificFellingDetail.ConfirmedFellingDetails.First(x =>
                                x.ConfirmedFellingDetailsId == fellingDetailsId).OperationType??FellingOperationType.None
                        },
                    },
                Breadcrumbs = confirmedFellingRestockingDetailsModel.Breadcrumbs
            };
            return View(model);
        }
        else
        {
            var specificFellingDetail = confirmedFellingRestockingDetailsModel.Compartments.FirstOrDefault(x => x.ConfirmedFellingDetails.Any(y => y.ConfirmedFellingDetailsId == fellingDetailsId));
            if (specificFellingDetail is null)
            {
                this.AddErrorMessage(retrievalErrorMessage);
                return RedirectToAction("ConfirmedFellingAndRestocking", new { applicationId });
            }
            var model = new AmendConfirmedRestockingDetailsViewModel
            {
                FellingLicenceApplicationSummary = confirmedFellingRestockingDetailsModel.FellingLicenceApplicationSummary,
                ApplicationId = confirmedFellingRestockingDetailsModel.ApplicationId,
                SubmittedFlaPropertyCompartments = confirmedFellingRestockingDetailsModel.SubmittedFlaPropertyCompartments,
                ConfirmedFellingRestockingDetails =
                    new IndividualConfirmedRestockingDetailModel
                    {
                        CompartmentId = specificFellingDetail.CompartmentId,
                        CompartmentNumber = specificFellingDetail.CompartmentNumber,
                        SubCompartmentName = specificFellingDetail.SubCompartmentName,
                        SubmittedFlaPropertyCompartmentId = specificFellingDetail.SubmittedFlaPropertyCompartmentId,
                        ConfirmedRestockingDetails =
                            specificFellingDetail.ConfirmedFellingDetails.First(x =>
                                x.ConfirmedFellingDetailsId == fellingDetailsId).ConfirmedRestockingDetails.First(x =>
                                x.ConfirmedRestockingDetailsId == restockingDetailsId),
                    },
                Breadcrumbs = confirmedFellingRestockingDetailsModel.Breadcrumbs
            };
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AmendConfirmedRestockingDetails(
        AmendConfirmedRestockingDetailsViewModel model,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
        [FromServices] IValidator<AmendConfirmedRestockingDetailsViewModel> validator,
        CancellationToken cancellationToken)
    {
        return await HandleConfirmedRestockingDetails(model, useCase, validator, cancellationToken, isAdd: false);
    }

    [HttpPost]
    public async Task<IActionResult> AddConfirmedRestockingDetails(
        AmendConfirmedRestockingDetailsViewModel model,
        [FromServices] IConfirmedFellingAndRestockingDetailsUseCase useCase,
        [FromServices] IValidator<AmendConfirmedRestockingDetailsViewModel> validator,
        CancellationToken cancellationToken)
    {
        return await HandleConfirmedRestockingDetails(model, useCase, validator, cancellationToken, isAdd: true);
    }

    private async Task<IActionResult> HandleConfirmedRestockingDetails(
        AmendConfirmedRestockingDetailsViewModel model,
        IConfirmedFellingAndRestockingDetailsUseCase useCase,
        IValidator<AmendConfirmedRestockingDetailsViewModel> validator,
        CancellationToken cancellationToken,
        bool isAdd)
    {
        var rd = model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails;
        var restockingProposal = rd.RestockingProposal;
        if (restockingProposal == TypeOfProposal.CreateDesignedOpenGround)
        {
            model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.ConfirmedRestockingSpecies = Array.Empty<ConfirmedRestockingSpeciesModel>();
            model.Species.Clear();
        }

        if (rd.RestockingProposal == TypeOfProposal.RestockWithIndividualTrees || rd.RestockingProposal == TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees)
        {
            rd.RestockingDensity = null;
        }
        else
        {
            rd.NumberOfTrees = null;
        }

        var speciesList = model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.ConfirmedRestockingSpecies.ToList();
        foreach (var species in model.Species.Where(species => speciesList.All(x => x.Species != species.Key)))
        {
            speciesList.Add(new ConfirmedRestockingSpeciesModel
            {
                Species = species.Key,
                Deleted = false
            });
        }

        foreach (var deletedSpecies in model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.ConfirmedRestockingSpecies
                     .Where(x => model.Species.ContainsKey(x.Species ?? "") is false))
        {
            deletedSpecies.Deleted = true;
        }

        ValidateModel(model, validator);

        if (ModelState.IsValid is false)
        {
            var user = new InternalUser(User);
            var (_, retrievalFailure, confirmedFellingRestockingDetailsModel) = await useCase.GetConfirmedFellingAndRestockingDetailsAsync(model.ApplicationId, user, cancellationToken, AmendRestockingPageName);

            if (retrievalFailure)
            {
                this.AddErrorMessage("Could not retrieve felling details");
                return RedirectToAction("ConfirmedFellingAndRestocking", new { id = model.ApplicationId });
            }

            var specificFellingDetail = confirmedFellingRestockingDetailsModel.Compartments
                .FirstOrDefault(x => x.ConfirmedFellingDetails.Any(y => y.ConfirmedFellingDetailsId == model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.ConfirmedFellingDetailsId));

            if (specificFellingDetail is null)
            {
                this.AddErrorMessage("Could not retrieve felling details");
                return RedirectToAction("ConfirmedFellingAndRestocking", new { id = model.ApplicationId });
            }

            model.FellingLicenceApplicationSummary = confirmedFellingRestockingDetailsModel.FellingLicenceApplicationSummary;
            model.Breadcrumbs = confirmedFellingRestockingDetailsModel.Breadcrumbs;
            model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.OperationType = specificFellingDetail.ConfirmedFellingDetails.First(x =>
                                                x.ConfirmedFellingDetailsId == model.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.ConfirmedFellingDetailsId).OperationType ?? FellingOperationType.None;
            model.SubmittedFlaPropertyCompartments = confirmedFellingRestockingDetailsModel.SubmittedFlaPropertyCompartments;

            return View(model);
        }

        var userToSave = new InternalUser(User);
        var result = await useCase.SaveConfirmedRestockingDetailsAsync(
            model,
            userToSave,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage(isAdd ? "Could not add confirmed restocking details" : "Could not save confirmed restocking details");
            return RedirectToAction("ConfirmedFellingAndRestocking", new { id = model.ApplicationId });
        }

        if (isAdd)
        {
            this.AddConfirmationMessage("Confirmed restocking details added");
        }

        return RedirectToAction(nameof(ConfirmedFellingAndRestocking), new { id = model.ApplicationId });
    }
}