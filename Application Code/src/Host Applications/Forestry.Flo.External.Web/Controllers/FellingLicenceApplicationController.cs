using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Infrastructure.Display;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using FellingLicenceStatusConstants = Forestry.Flo.External.Web.Models.FellingLicenceStatusConstants;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize, RequireCompletedRegistration, AutoValidateAntiforgeryToken]
public partial class FellingLicenceApplicationController(
    ILogger<FellingLicenceApplicationController> logger,
    CreateFellingLicenceApplicationUseCase createFellingLicenceApplicationUseCase,
    IBus busControl,
    IValidator<OperationDetailsModel> operationsValidator,
    IValidator<ProposedFellingDetailModel> fellingDetailsValidator,
    IValidator<ProposedRestockingDetailModel> restockingDetailsValidator,
    IValidator<DecisionToRestockViewModel> decisionToRestockValidator,
    IValidator<SelectFellingOperationTypesViewModel> fellingOperationTypesValidator,
    IValidator<SelectRestockingOptionsViewModel> restockingOptionsValidator,
    IValidator<FlaTermsAndConditionsViewModel> flaTermsAndConditionsViewModelValidator)
    : Controller
{
    public IActionResult Index(
        Guid woodlandOwnerId)
    {
        return RedirectToAction(nameof(SelectWoodland), new {woodlandOwnerId});
    }

    [HttpGet]
    public async Task<IActionResult> SelectWoodland(
        Guid woodlandOwnerId,
        Guid? agencyId,
        Guid? propertyId,
        CreateApplicationAgencySourcePage? agencySourcePage,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var getPropertyProfilesResult =
            await createFellingLicenceApplicationUseCase.RetrievePropertyProfilesForWoodlandOwnerAsync(
                woodlandOwnerId,
                user,
                cancellationToken);

        if (getPropertyProfilesResult.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (!getPropertyProfilesResult.Value.Any())
        {
            return RedirectToAction(nameof(PropertyProfileController.Create), "PropertyProfile", new {isApplication = true, woodlandOwnerId});
        }

        var model = new SelectWoodlandModel
        {
            ApplicationId = Guid.Empty,
            WoodlandOwnerId = woodlandOwnerId,
            AgencyId = agencyId,
            PropertyProfileId = propertyId.HasValue ? propertyId.Value : Guid.Empty,
            AgencySourcePage = agencySourcePage
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = model.WoodlandOwnerId;

        ViewBag.PropertyProfiles = getPropertyProfilesResult.Value.OrderBy(p=>p.Name);
        SetTaskBreadcrumbs(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SelectWoodland(
        SelectWoodlandModel selectWoodlandModel, 
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        if (!ModelState.IsValid)
        {
            var getPropertyProfilesResult =
                await createFellingLicenceApplicationUseCase.RetrievePropertyProfilesForWoodlandOwnerAsync(
                    selectWoodlandModel.WoodlandOwnerId,
                    user,
                    cancellationToken);

            if (getPropertyProfilesResult.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            SetTaskBreadcrumbs(selectWoodlandModel);
            ViewBag.PropertyProfiles = getPropertyProfilesResult.Value;

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = selectWoodlandModel.WoodlandOwnerId;

            return View(selectWoodlandModel);
        }

        var result = await createFellingLicenceApplicationUseCase.CreateFellingLicenceApplication(
            user,
            selectWoodlandModel.PropertyProfileId, 
            selectWoodlandModel.WoodlandOwnerId,
            true, cancellationToken);

        return result.IsFailure
            ? RedirectToAction(nameof(HomeController.Error), "Home")
            : RedirectToAction(nameof(Operations),
                new { applicationId = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> UpdateSelectWoodland(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var application =
            await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (application.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (application.Value.CompletedStepsCount != 0)
        {
            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId = application.Value.ApplicationId });
        }

        var getPropertyProfilesResult =
            await createFellingLicenceApplicationUseCase.RetrievePropertyProfilesForWoodlandOwnerAsync(
                application.Value.WoodlandOwnerId,
                user,
                cancellationToken);

        if (getPropertyProfilesResult.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (!getPropertyProfilesResult.Value.Any())
        {
            return RedirectToAction(nameof(PropertyProfileController.Create), "PropertyProfile", new { isApplication = true });
        }

        var model = new SelectWoodlandModel
        {
            ApplicationId = application.Value.ApplicationId,
            PropertyProfileId = application.Value.ApplicationSummary.PropertyProfileId,
            ApplicationReference = application.Value.ApplicationSummary.ApplicationReference,
            WoodlandOwnerId = application.Value.WoodlandOwnerId,
            FellingLicenceStatus = application.Value.ApplicationSummary.Status
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.Value.WoodlandOwnerId;

        ViewBag.PropertyProfiles = getPropertyProfilesResult.Value.OrderBy(p => p.Name);
        ViewBag.ApplicationSummary = application.Value.ApplicationSummary;
        SetTaskBreadcrumbs(model);
        return View("SelectWoodland", model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSelectWoodland(
        SelectWoodlandModel selectWoodlandModel, 
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        if (!ModelState.IsValid)
        {
            var getPropertyProfilesResult =
                await createFellingLicenceApplicationUseCase.RetrievePropertyProfilesForWoodlandOwnerAsync(
                    selectWoodlandModel.WoodlandOwnerId,
                    user,
                    cancellationToken);

            if (getPropertyProfilesResult.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = selectWoodlandModel.WoodlandOwnerId;

            ViewBag.PropertyProfiles = getPropertyProfilesResult.Value;
            return View("SelectWoodland", selectWoodlandModel);
        }

        selectWoodlandModel.StepComplete = true;

        var result = await createFellingLicenceApplicationUseCase.UpdateWoodland(user,
            selectWoodlandModel, cancellationToken);

        return result.IsFailure
            ? RedirectToAction(nameof(HomeController.Error), "Home")
            : RedirectToAction(nameof(SelectCompartments),
                new { applicationId = result.Value });
    }

    /// <summary>
    /// Sets a compartment as selected for an application before loading the page to allow user to select any more compartments for their application.
    /// </summary>
    /// <param name="applicationId"></param> Id of the application to add the compartment as selected.
    /// <param name="newCompartmentId">THe identifier for the <see cref="Compartment"/> to select for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// Id of the compartment that is to be added to the application.
    /// <returns></returns>
    public async Task<IActionResult> SelectNewCompartment(Guid applicationId, Guid newCompartmentId, Guid? proposedFellingDetailsId, Guid? fellingCompartmentId, bool isForRestockingCompartmentSelection, FellingOperationType? fellingOperationType, string? fellingCompartmentName,  CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var viewModel = await createFellingLicenceApplicationUseCase.GetSelectCompartmentViewModel(applicationId, user, cancellationToken);
        if (viewModel.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var model = new SelectedCompartmentsModel
        {
            ApplicationId = viewModel.Value.Application.ApplicationSummary.Id,
            SelectedCompartmentIds = viewModel.Value.Application.SelectedCompartments.SelectedCompartmentIds,
            StepComplete = viewModel.Value.Application.SelectedCompartments.StepComplete,
            ConstraintCheckStepComplete = viewModel.Value.Application.ConstraintCheck.StepComplete,
            WoodlandOwnerId = viewModel.Value.Application.WoodlandOwnerId
        };
        model.SelectedCompartmentIds.Add(newCompartmentId);

        if (!isForRestockingCompartmentSelection)
        {
            var result = await createFellingLicenceApplicationUseCase.SelectApplicationCompartmentsAsync(user,
                model.ApplicationId, model.SelectedCompartmentIds!, model.StepComplete, cancellationToken);

            if (result.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }    
        }

        return RedirectToAction(nameof(SelectCompartments), new
            {
                applicationId = model.ApplicationId,
                returnToApplicationSummary = model.ReturnToApplicationSummary,
                isForRestockingCompartmentSelection,
                fellingOperationType,
                fellingCompartmentName,
                fellingCompartmentId,
                proposedFellingDetailsId
            });
    }

    [HttpGet]
    public async Task<IActionResult> SelectCompartments(Guid applicationId, bool returnToApplicationSummary, bool isForRestockingCompartmentSelection, FellingOperationType fellingOperationType, string? fellingCompartmentName, Guid? fellingCompartmentId, Guid? proposedFellingDetailsId, bool? returnToPlayback, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var viewModel = await createFellingLicenceApplicationUseCase.GetSelectCompartmentViewModel(applicationId, user, cancellationToken);

        if (viewModel.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }
        var gisCompartment = viewModel.Value.Compartments.Select(c => new {
            c.Id,
            c.GISData,
            c.DisplayName,
            Selected = viewModel.Value.Application.SelectedCompartments.SelectedCompartmentIds?.Contains(c.Id)
        });

        if (viewModel.Value.Compartments.Count == 0)
        {
            return RedirectToAction(nameof(CompartmentController.CreateDetails), "Compartment",
                new
                {
                    propertyProfileId = viewModel.Value.Application.ApplicationSummary.PropertyProfileId,
                    applicationId = viewModel.Value.Application.ApplicationSummary.Id,
                    woodlandOwnerId = viewModel.Value.Application.WoodlandOwnerId
                });
        }

        var selectedRestockingCompartmentIds = new List<Guid>();

        if (isForRestockingCompartmentSelection)
        {
            var fellingCompartment = viewModel.Value.Application.FellingAndRestockingDetails.DetailsList.Find(dl => dl.CompartmentId == fellingCompartmentId);

            if (fellingCompartment != null)
            {
                var felling = fellingCompartment.FellingDetails.Find(fd => fd.Id == proposedFellingDetailsId);

                if (felling != null)
                {
                    selectedRestockingCompartmentIds = felling.ProposedRestockingDetails.Select(prd => prd.RestockingCompartmentId).Distinct().ToList();
                }
            }
        }

        var model = new SelectedCompartmentsModel
        {
            ApplicationId = viewModel.Value.Application.ApplicationSummary.Id,
            ApplicationReference = viewModel.Value.Application.ApplicationSummary.ApplicationReference,
            PropertyProfileId = viewModel.Value.Application.ApplicationSummary.PropertyProfileId,
            SelectedCompartmentIds = isForRestockingCompartmentSelection ? selectedRestockingCompartmentIds : viewModel.Value.Application.SelectedCompartments.SelectedCompartmentIds,
            GIS = JsonConvert.SerializeObject(gisCompartment),
            StepComplete = viewModel.Value.Application.SelectedCompartments.StepComplete,
            FellingLicenceStatus = viewModel.Value.Application.ApplicationSummary.Status,
            DetailsList = viewModel.Value.Application.FellingAndRestockingDetails.DetailsList,
            ConstraintCheckStepComplete = viewModel.Value.Application.ConstraintCheck.StepComplete,
            IsForRestockingCompartmentSelection = isForRestockingCompartmentSelection,
            FellingOperationType = fellingOperationType,
            FellingCompartmentName = fellingCompartmentName,
            FellingCompartmentId = fellingCompartmentId,
            ProposedFellingDetailsId = proposedFellingDetailsId,
            ReturnToPlayback = returnToPlayback.HasValue && returnToPlayback.Value,
            WoodlandOwnerId = viewModel.Value.Application.WoodlandOwnerId
        };

        //check if the application is currently read-only, if so redirect to the summary page
        if (model.AllowEditing is false)
        {
            return RedirectToAction(nameof(FellingAndRestockingPlayback), new { applicationId });
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = viewModel.Value.Application.WoodlandOwnerId;

        var compartmentsToShow = new List<Models.Compartment.CompartmentModel>();

        if (isForRestockingCompartmentSelection && !fellingOperationType.SupportsAlternativeCompartmentRestocking())
        {
            compartmentsToShow.Add(viewModel.Value.Compartments.First(c => c.Id == fellingCompartmentId));
        }
        else
        {
            compartmentsToShow.AddRange(viewModel.Value.Compartments);
        }

        ViewBag.Compartments = compartmentsToShow.OrderByNameNumericOrAlpha().ToList();
        ViewBag.ApplicationSummary = viewModel.Value.Application.ApplicationSummary;
        model.ReturnToApplicationSummary = returnToApplicationSummary;
        SetTaskBreadcrumbs(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SelectCompartments(
        SelectedCompartmentsModel compartmentsModel, 
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var viewModel = await createFellingLicenceApplicationUseCase.GetSelectCompartmentViewModel(compartmentsModel.ApplicationId, user, cancellationToken);
        if (viewModel.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        compartmentsModel.StepComplete = true;

        if ((compartmentsModel.SelectedCompartmentIds?.Count ?? 0) == 0)
        {
            // this should not be possible as we should have been pushed into creating a compartment on first loading this screen
            if (viewModel.Value.Compartments.Count == 0)
            {
                return RedirectToAction(nameof(CompartmentController.CreateDetails), "Compartment",
                    new
                    {
                        propertyProfileId = viewModel.Value.Application.ApplicationSummary.PropertyProfileId,
                        applicationId = viewModel.Value.Application.ApplicationSummary.Id,
                        woodlandOwnerId = viewModel.Value.Application.WoodlandOwnerId
                    } );
            }

            var mode = compartmentsModel.IsForRestockingCompartmentSelection ? "restock" : "fell";
            this.AddErrorMessage($"Select at least one compartment to {mode} in");

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = viewModel.Value.Application.WoodlandOwnerId;

            var compartmentsToShow = new List<Models.Compartment.CompartmentModel>();

            if (compartmentsModel.IsForRestockingCompartmentSelection 
                && !compartmentsModel.FellingOperationType!.Value.SupportsAlternativeCompartmentRestocking())
            {
                compartmentsToShow.Add(viewModel.Value.Compartments.First(c => c.Id == compartmentsModel.FellingCompartmentId));
            }
            else
            {
                compartmentsToShow.AddRange(viewModel.Value.Compartments);
            }

            ViewBag.Compartments = compartmentsToShow;
            ViewBag.ApplicationSummary = viewModel.Value.Application.ApplicationSummary;
            SetTaskBreadcrumbs(compartmentsModel);
            return View(compartmentsModel);
        }

        if (compartmentsModel.IsForRestockingCompartmentSelection)
        {
            var result = await createFellingLicenceApplicationUseCase.UpdateRestockingCompartmentsForFellingAsync
                (user,
                compartmentsModel.ApplicationId, 
                compartmentsModel.ProposedFellingDetailsId ?? Guid.Empty, 
                compartmentsModel.SelectedCompartmentIds!, 
                compartmentsModel.FellingCompartmentId ?? Guid.Empty, 
                cancellationToken);
            
            if (result.IsFailure)
            {
                RedirectToAction(nameof(HomeController.Error), "Home");
            }

            result = await createFellingLicenceApplicationUseCase.CreateMissingRestockingStatuses
                (user,
                compartmentsModel.ApplicationId,
                compartmentsModel.FellingCompartmentId!.Value,
                compartmentsModel.ProposedFellingDetailsId.HasValue ? compartmentsModel.ProposedFellingDetailsId.Value : Guid.Empty,
                cancellationToken);

            if (result.IsFailure)
            {
                RedirectToAction(nameof(HomeController.Error), "Home");
            }
        }
        else
        {
            var result = await createFellingLicenceApplicationUseCase.SelectApplicationCompartmentsAsync(user,
                compartmentsModel.ApplicationId, compartmentsModel.SelectedCompartmentIds!, compartmentsModel.StepComplete, cancellationToken);
            if (result.IsFailure)
            {
                RedirectToAction(nameof(HomeController.Error), "Home");
            }
        }

        if (compartmentsModel.StepComplete.Value 
            && !(viewModel.Value.Compartments.Where(x => compartmentsModel.SelectedCompartmentIds!.Contains(x.Id)).Any(x => x.GISData == null)))
        {
            // enqueue the asynchronous calculation of centre point, OS grid reference and set Area Code ready for when the FLA is submitted

            await busControl.Publish(
                new CentrePointCalculationMessage(
                    viewModel.Value.Application.WoodlandOwnerId,
                    user.UserAccountId!.Value,
                    compartmentsModel.ApplicationId,
                    user.IsFcUser,
                    string.IsNullOrEmpty(user.AgencyId) ? null : Guid.Parse(user.AgencyId)),
                cancellationToken);
        }

        if (compartmentsModel.ReturnToApplicationSummary)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId = compartmentsModel.ApplicationId });
        }

        if (compartmentsModel.IsForRestockingCompartmentSelection)
        {
            return await IterateRestockingCompartmentsForFellingOperationType(compartmentsModel.ApplicationId, compartmentsModel.FellingCompartmentId.Value, compartmentsModel.ProposedFellingDetailsId.Value);
        }

        return await IterateCompartments(compartmentsModel.ApplicationId);
    }

    [HttpGet]
    public async Task<IActionResult> SelectFellingOperationTypes(Guid applicationId, Guid fellingCompartmentId, bool? returnToPlayback, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var model = await createFellingLicenceApplicationUseCase.GetSelectFellingOperationTypesViewModel(applicationId, fellingCompartmentId, user, cancellationToken);

        if (model.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        model.Value.FellingCompartmentId = fellingCompartmentId;
        model.Value.OperationTypes = new List<FellingOperationType>();
        var compartment = model.Value.Compartments.FirstOrDefault(c => c.Id == fellingCompartmentId);

        model.Value.CompartmentName = compartment?.DisplayName ?? string.Empty;

        ViewBag.OperationTypes = model.Value.OperationTypes;
        ViewBag.ApplicationSummary = model.Value.Application.ApplicationSummary;
        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = model.Value.Application.WoodlandOwnerId;

        model.Value.ReturnToPlayback = returnToPlayback.HasValue && returnToPlayback.Value;

        //check if the application is currently read-only, if so redirect to the summary page
        if (model.Value.AllowEditing is false)
        {
            return RedirectToAction(nameof(FellingAndRestockingPlayback), new { applicationId });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SelectFellingOperationTypes(SelectFellingOperationTypesViewModel model, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var operationTypes = new List<FellingOperationType>();

        if (model.IsClearFellingSelected)
            operationTypes.Add(FellingOperationType.ClearFelling);
        if (model.IsFellingIndividualTreesSelected)
            operationTypes.Add(FellingOperationType.FellingIndividualTrees);
        if (model.IsFellingOfCoppiceSelected)
            operationTypes.Add(FellingOperationType.FellingOfCoppice);
        if (model.IsRegenerationFellingSelected)
            operationTypes.Add(FellingOperationType.RegenerationFelling);
        if (model.IsThinningSelected)
            operationTypes.Add(FellingOperationType.Thinning);

        model.OperationTypes = operationTypes;

        ValidateModel(model, fellingOperationTypesValidator);

        if (!ModelState.IsValid)
        {
            var remodel = await createFellingLicenceApplicationUseCase.GetSelectFellingOperationTypesViewModel(model.ApplicationId, model.FellingCompartmentId, user, cancellationToken);

            remodel.Value.FellingCompartmentId = model.FellingCompartmentId;
            remodel.Value.OperationTypes = new List<FellingOperationType>();
            var compartment = remodel.Value.Compartments.FirstOrDefault(c => c.Id == model.FellingCompartmentId);

            remodel.Value.CompartmentName = compartment?.DisplayName ?? string.Empty;

            ViewBag.OperationTypes = remodel.Value.OperationTypes;
            ViewBag.ApplicationSummary = remodel.Value.Application.ApplicationSummary;

            return View(remodel.Value);
        }

        var result = await createFellingLicenceApplicationUseCase.CreateEmptyProposedFellingDetails
            (user,
            model,
            cancellationToken);
        
        if (result.IsFailure)
        {
            RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var statusUpdateResult = await createFellingLicenceApplicationUseCase.CreateMissingFellingStatuses(
            user,
            model.ApplicationId,
            model.FellingCompartmentId,
            cancellationToken);

        if (statusUpdateResult.IsFailure)
        {
            RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return await IterateFellingOperationTypesInCompartment(model.ApplicationId, model.FellingCompartmentId);
    }


    [HttpGet]
    public async Task<IActionResult> ConstraintsCheck(Guid applicationId, bool returnToApplicationSummary, string? tab,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var viewModel = await createFellingLicenceApplicationUseCase.GetSelectCompartmentViewModel(applicationId, user, cancellationToken);
        if (viewModel.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = viewModel.Value.Application.WoodlandOwnerId;

        var model = result.Value.ConstraintCheck;
        model.ReturnToApplicationSummary = returnToApplicationSummary;
        ViewBag.ApplicationSummary = result.Value.ApplicationSummary;
        SetTaskBreadcrumbs(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ConstraintsCheck(
        [FromForm] Guid applicationId,
        [FromForm] bool? notRunningExternalLisReport,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        
        var resultModel = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        var model = resultModel.Value.ConstraintCheck;
        model.StepComplete = null;
        model.NotRunningExternalLisReport = notRunningExternalLisReport;

        if (model.MostRecentExternalLisReport.HasValue || notRunningExternalLisReport.Value)
        {
            model.StepComplete = true;
        }
        else if (model.ExternalLisAccessedTimestamp.HasValue)
        {
            model.StepComplete = false;
        }

        var result = await createFellingLicenceApplicationUseCase.SetApplicationConstraintCheckAsync(user,
            model, cancellationToken);

        return result.IsFailure
            ? RedirectToAction(nameof(HomeController.Error), "Home")
            : RedirectToAction(nameof(SupportingDocumentation), new { applicationId = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> RunConstraintsCheck(
        [FromForm] Guid applicationId,
        [FromServices] RunApplicantConstraintCheckUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.ExecuteConstraintsCheckAsync(user, applicationId, cancellationToken);

        if (result.IsSuccess)
        {
            var resultModel = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
            if (resultModel.HasValue)
            {
                var model = resultModel.Value.ConstraintCheck;
                model.NotRunningExternalLisReport = false;
                model.ExternalLisReportRun = true;

                await createFellingLicenceApplicationUseCase.SetApplicationConstraintCheckAsync(user,
                    model, cancellationToken);
            }

            return result.Value;
        }

        return RedirectToAction(nameof(HomeController.Error), "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Operations(Guid applicationId, bool returnToApplicationSummary, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var application = result.Value;
        var model = application.OperationDetails;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.WoodlandOwnerId;

        ViewBag.ApplicationSummary = application.ApplicationSummary;
        model.ReturnToApplicationSummary = returnToApplicationSummary;
        SetTaskBreadcrumbs(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Operations(OperationDetailsModel operationDetailsModel, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        ValidateModel(operationDetailsModel, operationsValidator);
        if (!ModelState.IsValid)
        {
            var applicationResult =
                await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(
                    user,
                    operationDetailsModel.ApplicationId,
                    cancellationToken);

            if (applicationResult.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var application = applicationResult.Value;

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;

            ViewBag.ApplicationSummary = application.ApplicationSummary;
            SetTaskBreadcrumbs(operationDetailsModel);
            return View(operationDetailsModel);
        }

        var result = await createFellingLicenceApplicationUseCase.SetApplicationOperationsAsync(
            user,
            operationDetailsModel, 
            cancellationToken);

        return result.IsFailure
            ? RedirectToAction(nameof(HomeController.Error), "Home")
            : operationDetailsModel.ReturnToApplicationSummary 
                ? RedirectToAction(nameof(ApplicationSummary), new { applicationId = operationDetailsModel.ApplicationId })
                : RedirectToAction(nameof(SelectCompartments), new { applicationId = operationDetailsModel.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> FellingDetail(Guid applicationId, Guid fellingCompartmentId, Guid proposedFellingDetailsId, bool? returnToPlayback, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplicationCompartmentDetail(user, applicationId, fellingCompartmentId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var fellingDetail = result.Value.FellingAndRestockingDetail;
        ViewBag.ApplicationSummary = result.Value.ApplicationSummary;

        var model = fellingDetail.FellingDetails.Find(fd => fd.Id == proposedFellingDetailsId);

        if (model is null)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        model.ApplicationReference = fellingDetail.ApplicationReference;
        model.ApplicationId = applicationId;
        model.FellingCompartmentId = fellingCompartmentId;
        model.FellingCompartmentName = result.Value.FellingAndRestockingDetail.Compartments.Find(c => c.Id == fellingCompartmentId)?.DisplayName ?? string.Empty;
        model.ReturnToPlayback = returnToPlayback.HasValue && returnToPlayback.Value;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.ApplicationSummary.WoodlandOwnerId;

        //check if the application is currently read-only, if so redirect to the summary page
        if (model.AllowEditing is false)
        {
            return RedirectToAction(nameof(FellingAndRestockingPlayback), new { applicationId });
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> FellingDetail(ProposedFellingDetailModel proposedFellingDetail, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        ValidateModel(proposedFellingDetail, fellingDetailsValidator);

        if (ModelState.IsValid)
        {
            proposedFellingDetail.StepComplete = true;
            var updateFellingRestockingResult = await createFellingLicenceApplicationUseCase.UpdateApplicationFellingDetailsAsync(
                user,
                proposedFellingDetail,
                cancellationToken);
            if (updateFellingRestockingResult.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var isLarch = proposedFellingDetail.Species
                    .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Key))
                    .Where(specie => specie?.IsLarch ?? false).Any();
            if(isLarch)
                return RedirectToAction(nameof(LarchSpeciesInformation), new 
                { 
                    id = proposedFellingDetail.ApplicationId,
                    applicationId = proposedFellingDetail.ApplicationId,
                    fellingCompartmentId = proposedFellingDetail.FellingCompartmentId,
                    proposedFellingDetailsId = proposedFellingDetail.Id,
                    ReturnToPlayback = proposedFellingDetail.ReturnToPlayback
                });

            return await DecideOnPostFellingDetailRedirect(proposedFellingDetail);
        }
        else
        {
            var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplicationCompartmentDetail(user, proposedFellingDetail.ApplicationId, proposedFellingDetail.FellingCompartmentId, cancellationToken);
            if (result.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            ViewBag.ApplicationSummary = result.Value.ApplicationSummary;
            return View(proposedFellingDetail);
        }
    }

    [HttpGet]
    public IActionResult LarchSpeciesInformation(Guid applicationId, Guid fellingCompartmentId, Guid proposedFellingDetailsId, bool? returnToPlayback, CancellationToken cancellationToken)
    {
        var model = new LarchSpeciesInformationModel()
        {
            ApplicationId = applicationId,
            FellingCompartmentId = fellingCompartmentId,
            ProposedFellingDetailsId = proposedFellingDetailsId,
            ReturnToPlayback = returnToPlayback.HasValue && returnToPlayback.Value
        };
        return View(model);
    }

    [HttpPost]
    [ActionName("LarchSpeciesInformation")]
    public async Task<IActionResult> LarchSpeciesInformationContinue(LarchSpeciesInformationModel informationModel, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplicationCompartmentDetail(user, informationModel.ApplicationId, informationModel.FellingCompartmentId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }
        var fellingDetail = result.Value.FellingAndRestockingDetail;
        ViewBag.ApplicationSummary = result.Value.ApplicationSummary;

        var model = fellingDetail.FellingDetails.Find(fd => fd.Id == informationModel.ProposedFellingDetailsId);

        if (model is null)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }
        model.ApplicationReference = fellingDetail.ApplicationReference;
        model.ApplicationId = informationModel.ApplicationId;
        model.FellingCompartmentId = informationModel.FellingCompartmentId;
        model.FellingCompartmentName = result.Value.FellingAndRestockingDetail.Compartments.Find(c => c.Id == informationModel.FellingCompartmentId)?.DisplayName ?? string.Empty;
        model.ReturnToPlayback = informationModel.ReturnToPlayback;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.ApplicationSummary.WoodlandOwnerId;

        return await DecideOnPostFellingDetailRedirect(model);
    }

    [HttpGet]
    public async Task<IActionResult> DecisionToRestock(Guid applicationId, Guid fellingCompartmentId, Guid proposedFellingDetailsId, FellingOperationType fellingOperationType, bool? returnToPlayback, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.GetSelectCompartmentViewModel(applicationId, user, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewBag.ApplicationSummary = result.Value.Application.ApplicationSummary;

        var compartmentName = result.Value.Compartments.FirstOrDefault(c => c.Id == fellingCompartmentId)?.DisplayName ?? string.Empty;

        var compartment = result.Value.Application.FellingAndRestockingDetails.DetailsList.Find(dl => dl.CompartmentId == fellingCompartmentId);

        var restockSelected = true;
        var noRestockReason = string.Empty;

        if (compartment != null)
        {
            var felling = compartment.FellingDetails.Find(fd => fd.Id == proposedFellingDetailsId);

            if (felling != null)
            {
                restockSelected = !felling.IsRestocking.HasValue || felling.IsRestocking.Value;
                noRestockReason = !string.IsNullOrEmpty(felling.NoRestockingReason) ? felling.NoRestockingReason : string.Empty;
            }
        }

        var model = new DecisionToRestockViewModel()
        {
            ApplicationId = applicationId,
            FellingCompartmentId = fellingCompartmentId,
            ProposedFellingDetailsId = proposedFellingDetailsId,
            FellingCompartmentName = compartmentName,
            OperationType = fellingOperationType,
            IsRestockSelected = restockSelected,
            Reason = noRestockReason,
            FellingLicenceStatus = result.Value.Application.ApplicationSummary.Status
        };

        model.ReturnToPlayback = returnToPlayback.HasValue && returnToPlayback.Value;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.Application.ApplicationSummary.WoodlandOwnerId;

        //check if the application is currently read-only, if so redirect to the summary page
        if (model.AllowEditing is false)
        {
            return RedirectToAction(nameof(FellingAndRestockingPlayback), new { applicationId });
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DecisionToRestock(DecisionToRestockViewModel model, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        ValidateModel(model, decisionToRestockValidator);

        if (!ModelState.IsValid)
        {
            var compartmentResult = await createFellingLicenceApplicationUseCase.GetSelectCompartmentViewModel(model.ApplicationId, user, cancellationToken);
            if (compartmentResult.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            ViewBag.ApplicationSummary = compartmentResult.Value.Application.ApplicationSummary;

            return View(model);
        }

        var result = await createFellingLicenceApplicationUseCase.UpdateApplicationFellingDetailsWithRestockDecisionAsync
            (user, model, cancellationToken);

        if (result.IsFailure)
        {
            RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return model.IsRestockSelected ?
            RedirectToAction(nameof(SelectCompartments), "FellingLicenceApplication", new 
            {
                applicationId = model.ApplicationId, 
                isForRestockingCompartmentSelection = true,
                fellingOperationType = model.OperationType,
                fellingCompartmentId = model.FellingCompartmentId,
                fellingCompartmentName = model.FellingCompartmentName,
                proposedFellingDetailsId = model.ProposedFellingDetailsId
            })
            : await IterateFellingOperationTypesInCompartment(model.ApplicationId, model.FellingCompartmentId);
    }

    [HttpGet]
    public async Task<IActionResult> SelectRestockingOptions(Guid applicationId, Guid fellingCompartmentId, Guid restockingCompartmentId, Guid proposedFellingDetailsId, bool restockAlternativeArea, bool? returnToPlayback, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.GetSelectCompartmentViewModel(applicationId, user, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewBag.ApplicationSummary = result.Value.Application.ApplicationSummary;

        var model = await createFellingLicenceApplicationUseCase.GetSelectRestockingOptionsViewModel
                (applicationId, 
                fellingCompartmentId, 
                restockingCompartmentId, 
                proposedFellingDetailsId, 
                restockAlternativeArea, 
                user, 
                cancellationToken);

        if (model.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        model.Value.ReturnToPlayback = returnToPlayback.HasValue && returnToPlayback.Value;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.Application.ApplicationSummary.WoodlandOwnerId;

        //check if the application is currently read-only, if so redirect to the summary page
        if (model.Value.AllowEditing is false)
        {
            return RedirectToAction(nameof(FellingAndRestockingPlayback), new { applicationId });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SelectRestockingOptions(SelectRestockingOptionsViewModel model, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var restockingOptions = new List<TypeOfProposal>();

        if (model.IsCoppiceRegrowthSelected)
            restockingOptions.Add(TypeOfProposal.RestockWithCoppiceRegrowth);
        if (model.IsCreateOpenSpaceSelected)
            restockingOptions.Add(TypeOfProposal.CreateDesignedOpenGround);
        if (model.IsIndividualTreesSelected)
            restockingOptions.Add(TypeOfProposal.RestockWithIndividualTrees);
        if (model.IsNaturalRegenerationSelected)
            restockingOptions.Add(TypeOfProposal.RestockByNaturalRegeneration);
        if (model.IsReplantFelledAreaSelected)
            restockingOptions.Add(TypeOfProposal.ReplantTheFelledArea);
        if (model.IsIndividualTreesInAlternativeAreaSelected)
            restockingOptions.Add(TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees);
        if (model.IsPlantingInAlternativeAreaSelected)
            restockingOptions.Add(TypeOfProposal.PlantAnAlternativeArea);
        if (model.IsNaturalColonisationSelected)
            restockingOptions.Add(TypeOfProposal.NaturalColonisation);

        model.RestockingOptions = restockingOptions;

        ValidateModel(model, restockingOptionsValidator);

        if (!ModelState.IsValid)
        {
            var compartmentViewModel = await createFellingLicenceApplicationUseCase.GetSelectCompartmentViewModel(model.ApplicationId, user, cancellationToken);
            if (compartmentViewModel.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            ViewBag.ApplicationSummary = compartmentViewModel.Value.Application.ApplicationSummary;

            var remodel = await createFellingLicenceApplicationUseCase.GetSelectRestockingOptionsViewModel
                    (model.ApplicationId,
                    model.FellingCompartmentId,
                    model.RestockingCompartmentId,
                    model.ProposedFellingDetailsId,
                    model.RestockAlternativeArea,
                    user,
                    cancellationToken);

            if (remodel.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            return View(remodel.Value);
        }

        var result = await createFellingLicenceApplicationUseCase.CreateEmptyProposedRestockingDetails
            (user,
            model,
            cancellationToken);

        if (result.IsFailure)
        {
            RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var statusUpdateResult = await createFellingLicenceApplicationUseCase.CreateMissingRestockingStatuses(
            user,
            model.ApplicationId,
            model.FellingCompartmentId,
            model.ProposedFellingDetailsId,
            cancellationToken);

        if (statusUpdateResult.IsFailure)
        {
            RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return await IterateRestockingTypesForRestockingCompartment(model.ApplicationId,
            model.FellingCompartmentId,
            model.ProposedFellingDetailsId,
            model.RestockingCompartmentId);
    }

    [HttpGet]
    public async Task<IActionResult> RestockingDetail(Guid applicationId, Guid restockingId, Guid fellingCompartmentId, Guid restockingCompartmentId, Guid proposedFellingDetailsId, bool? returnToPlayback, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }
        
        ViewBag.ApplicationSummary = result.Value.ApplicationSummary;

        var model = await createFellingLicenceApplicationUseCase.GetRestockingDetailViewModel(user, applicationId, restockingId, result.Value, cancellationToken);

        if (model.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }
        model.Value.ReturnToPlayback = returnToPlayback.HasValue && returnToPlayback.Value;
        model.Value.FellingCompartmentId = fellingCompartmentId;
        SetTaskBreadcrumbs(model.Value);

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.ApplicationSummary.WoodlandOwnerId;

        //check if the application is currently read-only, if so redirect to the summary page
        if (model.Value.AllowEditing is false)
        {
            return RedirectToAction(nameof(FellingAndRestockingPlayback), new { applicationId });
        }


        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> RestockingDetail(ProposedRestockingDetailModel model, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        // recalculate percentage in case front-end javascript failed
        model.PercentageOfRestockArea = model.Area.HasValue && model.CompartmentTotalHectares.HasValue
            ? Math.Round(model.Area.Value / model.CompartmentTotalHectares.Value * 100, 2)
            : null;

        ValidateModel(model, restockingDetailsValidator);

        if (ModelState.IsValid)
        {
            model.StepComplete = true;
        }
        else
        {
            var res = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplicationCompartmentDetail(user, model.ApplicationId, model.FellingCompartmentId, cancellationToken);
            if (res.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            ViewBag.ApplicationSummary = res.Value.ApplicationSummary;

            return View(model);
        }

        var result = await createFellingLicenceApplicationUseCase.UpdateApplicationRestockingDetailsAsync(user, model, cancellationToken);

        if (result.IsFailure)
        {
            RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return await IterateRestockingTypesForRestockingCompartment(model.ApplicationId,
            model.FellingCompartmentId,
            model.ProposedFellingDetailsId,
            model.RestockingCompartmentId);
    }

    [HttpGet]
    public async Task<IActionResult> FellingAndRestockingPlayback(Guid applicationId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewBag.ApplicationSummary = result.Value.ApplicationSummary;

        var model = await createFellingLicenceApplicationUseCase.GetFellingAndRestockingDetailsPlaybackViewModel(applicationId, user, cancellationToken);

        if (model.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.ApplicationSummary.WoodlandOwnerId;

        return View(model.Value);
    }

    [HttpGet]
    public async Task<IActionResult> SupportingDocumentation(
        Guid applicationId,
        bool returnToApplicationSummary,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var application = result.Value;
        var model = application.SupportingDocumentation;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;

        ViewBag.ApplicationSummary = application.ApplicationSummary;
        model.ReturnToApplicationSummary = returnToApplicationSummary;
        SetTaskBreadcrumbs(model);
        return View(model);
    }

    /// <summary>
    /// Handle the Save/Continue post-back on the Supporting Documentation task screen,
    /// to persist whether the user has flagged as complete or in-complete.
    /// </summary>
    /// <param name="supportingDocumentationSaveModel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [ActionName("SupportingDocumentation")]
    public async Task<IActionResult> SupportingDocumentationSave(
        SupportingDocumentationSaveModel supportingDocumentationSaveModel,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, supportingDocumentationSaveModel.ApplicationId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (!supportingDocumentationSaveModel.StepComplete.HasValue)
        {
            ModelState.AddModelError("StepComplete", "Select if you have uploaded all relevant documents");

            var model = result.Value.SupportingDocumentation;

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.WoodlandOwnerId;

            ViewBag.ApplicationSummary = result.Value.ApplicationSummary;
            model.ReturnToApplicationSummary = model.ReturnToApplicationSummary;
            SetTaskBreadcrumbs(model);
            return View(model);
        }

        await createFellingLicenceApplicationUseCase.UpdateSupportingDocumentsStatusAsync(user, supportingDocumentationSaveModel, cancellationToken);

        var application = result.Value;

        return RedirectToAction(nameof(TermsAndConditions), new { applicationId = application.ApplicationId });
    }

    /// <summary>
    /// This is an action method to explicitly support the deleting of uploaded supporting documents.
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> RemoveSupportingDocumentation(
        Guid applicationId,
        Guid documentIdentifier,
        bool returnToApplicationSummary,
        [FromServices] RemoveSupportingDocumentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var removeResult = await useCase.RemoveSupportingDocumentAsync(user, applicationId, documentIdentifier, cancellationToken);

        if (removeResult.IsFailure)
        {
            logger.LogError("Failed to remove supporting documentation with error {Error}", removeResult.Error);
            this.AddErrorMessage("Could not remove supporting document at this time, try again");
        }
        if(returnToApplicationSummary)
            return RedirectToAction(nameof(SupportingDocumentation), new { applicationId, returnToApplicationSummary });
        else
            return RedirectToAction(nameof(SupportingDocumentation), new { applicationId });

    }

    [HttpPost]
    public async Task<IActionResult> AttachSupportingDocumentation(
        AddSupportingDocumentModel model,
        FormFileCollection supportingDocumentationFiles,
        [FromServices] AddSupportingDocumentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!supportingDocumentationFiles.Any())
            return RedirectToAction(nameof(SupportingDocumentation), new { applicationId = model.FellingLicenceApplicationId });

        var user = new ExternalApplicant(User);

        var saveDocumentsResult = await useCase.AddDocumentsToApplicationAsync(
            user,
            model,
            supportingDocumentationFiles,
            ModelState,
            cancellationToken);

        if (saveDocumentsResult.IsSuccess && ModelState.IsValid)
            if (model.ReturnToApplicationSummary)
                return RedirectToAction(nameof(SupportingDocumentation), new { applicationId = model.FellingLicenceApplicationId, model.ReturnToApplicationSummary });
            else
                return RedirectToAction(nameof(SupportingDocumentation), new { applicationId = model.FellingLicenceApplicationId });

        // Was not successful across the entire set of uploaded documents in FormFileCollection:
        var fellingLicenceApplicationModelResult =
            await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, model.FellingLicenceApplicationId, cancellationToken);

        if (fellingLicenceApplicationModelResult.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var application = fellingLicenceApplicationModelResult.Value;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;

        ViewBag.ApplicationSummary = application.ApplicationSummary;
        fellingLicenceApplicationModelResult.Value.SupportingDocumentation.ReturnToApplicationSummary = model.ReturnToApplicationSummary;
        SetTaskBreadcrumbs(fellingLicenceApplicationModelResult.Value.SupportingDocumentation);
        return View("SupportingDocumentation", fellingLicenceApplicationModelResult.Value.SupportingDocumentation);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadSupportingDocument(
        [FromServices] GetSupportingDocumentUseCase useCase,
        Guid applicationId,
        [FromQuery] Guid documentIdentifier,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.GetSupportingDocumentAsync(user, applicationId, documentIdentifier, cancellationToken);

        return result.IsSuccess ? result.Value : RedirectToAction(nameof(HomeController.Error), "Home");
    }

    [HttpGet]
    public async Task<IActionResult> TermsAndConditions(Guid applicationId, bool returnToApplicationSummary, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var application = result.Value;

        var model = application.FlaTermsAndConditionsViewModel;
        model.ReturnToApplicationSummary = returnToApplicationSummary;
        model.IsCBWapplication = application.IsCBWapplication;
        model.TotalNumberOfTreesRestocking = application.TotalNumberOfTreesRestocking;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;

        SetTaskBreadcrumbs(model);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> TermsAndConditions(FlaTermsAndConditionsViewModel flaTermsAndConditionsViewModel, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        ValidateModel(flaTermsAndConditionsViewModel, flaTermsAndConditionsViewModelValidator);

        if (!ModelState.IsValid)
        {
            var applicationResult = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, flaTermsAndConditionsViewModel.ApplicationId, cancellationToken);

            if (applicationResult.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var application = applicationResult.Value;
            ViewBag.ApplicationSummary = application.ApplicationSummary;

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;

            SetTaskBreadcrumbs(flaTermsAndConditionsViewModel);
            return View(flaTermsAndConditionsViewModel);
        }
        await createFellingLicenceApplicationUseCase.SetFlaTermsAndConditionsAccepted(user, flaTermsAndConditionsViewModel, cancellationToken);

        return RedirectToAction(nameof(ApplicationSummary), new { applicationId = flaTermsAndConditionsViewModel.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationTaskList(Guid applicationId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var applicationResult =
            await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (applicationResult.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var application = applicationResult.Value;

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;

        SetBreadcrumbs(application);
        return View(application);
    }


    [HttpGet]
    public async Task<IActionResult> ViewCaseNotes(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var fellingLicenceApplicationResult = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        var activityFeed = await createFellingLicenceApplicationUseCase.GetCaseNotesActivityFeedForApplicationAsync(
            applicationId, user, cancellationToken);
        if (activityFeed.HasNoValue)
        {
            this.AddErrorMessage("Could not retrieve case notes for application, try again");
            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId = applicationId });
        }

        var viewCaseNotesViewModel = new ViewCaseNotesViewModel
        {
            ActivityFeedViewModel = activityFeed.Value,
            ApplicationId = applicationId,
            ApplicationReference = fellingLicenceApplicationResult.Value.ApplicationSummary.ApplicationReference
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = fellingLicenceApplicationResult.Value.WoodlandOwnerId;

        SetTaskBreadcrumbs(viewCaseNotesViewModel);

        return View(viewCaseNotesViewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationSummary(Guid applicationId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.GetApplicationSummaryViewModel(user, applicationId, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (!result.Value.Application.IsComplete)
        {
            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId = result.Value.Application.ApplicationId });
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.Application.WoodlandOwnerId;

        SetApplicationSummaryBreadcrumbs(result.Value);

        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> SubmissionConfirmation(Guid applicationId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var application = result.Value;

        if (application.ApplicationSummary.Status != FellingLicenceStatus.Submitted)
        {
            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId = application.ApplicationId });
        }

        var model = new SubmissionConfirmationViewModel
        {
            ApplicationId = application.ApplicationId,
            ApplicationReference = application.ApplicationSummary.ApplicationReference,
            TaskName = "Submission"
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.WoodlandOwnerId;

        SetTaskBreadcrumbs(model);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SubmissionFailure(Guid applicationId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var model = new SubmissionFailureModel
        {
            ApplicationId = applicationId,
            TaskName = "Failure"
        };
        SetTaskBreadcrumbs(model);

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.WoodlandOwnerId;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmSubmitFellingLicenceApplication(
        Guid applicationId,
        [FromServices] ManageWoodlandOwnerDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        var enabledStatuses = new[]
        {
            FellingLicenceStatus.Draft,
            FellingLicenceStatus.WithApplicant,
            FellingLicenceStatus.ReturnedToApplicant
        };

        if (result.HasNoValue)
        {
            logger.LogInformation("Could not get application by its Id {applicationId} for the current user having Id of {userId}", applicationId, user.UserAccountId);
            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId });
        }

        if (enabledStatuses.Contains(result.Value.ApplicationSummary.Status) is false)
        {
            logger.LogInformation("Application with Id {applicationId} is not in a valid state for submission, current status is {status}", applicationId, result.Value.ApplicationSummary.Status);
            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId });
        }

        var application = result.Value;

        var model = new ConfirmSubmitFellingLicenceApplicationModel
        {
            ApplicationId = application.ApplicationId,
            ApplicationReference = application.ApplicationSummary.ApplicationReference,
            TaskName = "Submission"
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;
        ViewData["IgnoreFeedback"] = true;

        SetTaskBreadcrumbs(model);
        if (result.Value.AgencyId.HasValue)
        {
            var agentAuthorityCheck = await
                useCase.VerifyAgentCanSubmitApplicationAsync(
                    application.WoodlandOwnerId,
                    result.Value.AgencyId.Value,
                    cancellationToken);
            model.ValidAuthorityForm = agentAuthorityCheck.Value;
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitFellingLicenceApplication(
        SubmitFellingLicenceApplicationModel model,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var getFellingApplicationResult = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(
            user, 
            model.ApplicationId, 
            cancellationToken);

        if (getFellingApplicationResult.HasNoValue)
        {
            logger.LogWarning("Could not get application by it's Id {applicationId} for the current user having Id of {userId}", model.ApplicationId, user.UserAccountId);
            return RedirectToAction(nameof(SubmissionFailure), new { applicationId = model.ApplicationId });
        }

        var linkToApplication = 
            Url.AbsoluteAction(
                "Index",
                "FellingLicenceApplication", 
                new { id = model.ApplicationId })!;

        var (_, submissionFailure, isResubmission) = 
            await createFellingLicenceApplicationUseCase.SubmitFellingLicenceApplicationAsync(
                model.ApplicationId, 
                user,
                linkToApplication, 
                cancellationToken);

        if (submissionFailure)
        {
            return RedirectToAction(nameof(SubmissionFailure), new { applicationId = model.ApplicationId });
        }

        return RedirectToAction(nameof(SubmissionConfirmation), new { applicationId = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmWithdrawFellingLicenceApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (result.HasNoValue || 
            FellingLicenceStatusConstants.WithdrawalStatuses.Contains(result.Value.ApplicationSummary.Status) is false)
        {
            this.AddErrorMessage($"Application cannot be withdrawn in the current state of {result.Value.ApplicationSummary.Status.GetDisplayNameByActorType(ActorType.ExternalApplicant)}");
            return RedirectToAction(nameof(ApplicationTaskList), new {applicationId});
        }

        var application = result.Value;

        var model = new ConfirmWithdrawFellingLicenceApplicationModel
        {
            ApplicationId = application.ApplicationId,
            ApplicationReference = application.ApplicationSummary.ApplicationReference,
            TaskName = "Withdraw"
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;
        ViewData["IgnoreFeedback"] = true;

        SetTaskBreadcrumbs(model);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> WithdrawFellingLicenceApplication(WithdrawFellingLicenceApplicationModel withdrawFellingLicenceApplicationModel, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        string linkToApplication = Url.AbsoluteAction("Index", "FellingLicenceApplication", new { id = withdrawFellingLicenceApplicationModel.ApplicationId })!;

        var result = await createFellingLicenceApplicationUseCase.WithdrawFellingLicenceApplicationAsync(withdrawFellingLicenceApplicationModel.ApplicationId, user, linkToApplication!, cancellationToken);
        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong, try again.");

            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId = withdrawFellingLicenceApplicationModel.ApplicationId });
        }
        
        return RedirectToAction(nameof(WithdrawnConfirmation), new { applicationId = withdrawFellingLicenceApplicationModel.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> WithdrawnConfirmation(Guid applicationId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (result.HasNoValue || result.Value.ApplicationSummary.Status != FellingLicenceStatus.Withdrawn)
        {
            this.AddErrorMessage("Something went wrong, try again.");

            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId });
        }

        var application = result.Value;
        var model = new WithdrawnConfirmationViewModel
        {
            ApplicationId = application.ApplicationId,
            ApplicationReference = application.ApplicationSummary.ApplicationReference,
            TaskName = "Withdrawn"
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;

        SetTaskBreadcrumbs(model);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmDeleteFellingLicenceApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (result.HasNoValue || result.Value.ApplicationSummary.Status != FellingLicenceStatus.Draft)
        {
            this.AddErrorMessage($"Application must be in { FellingLicenceStatus.Draft.GetDisplayNameByActorType(ActorType.ExternalApplicant)} state to be deleted");

            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId });
        }

        var application = result.Value;

        var model = new ConfirmDeleteFellingLicenceApplicationModel
        {
            ApplicationId = application.ApplicationId,
            ApplicationReference = application.ApplicationSummary.ApplicationReference,
            TaskName = "Delete"
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = application.WoodlandOwnerId;
        ViewData["IgnoreFeedback"] = true;

        SetTaskBreadcrumbs(model);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteFellingLicenceApplication(DeleteFellingLicenceApplicationModel deleteFellingLicenceApplicationModel, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.DeleteDraftFellingLicenceApplicationAsync(deleteFellingLicenceApplicationModel.ApplicationId, user, cancellationToken);
        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong, try again.");

            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId = deleteFellingLicenceApplicationModel.ApplicationId });
        }

        return RedirectToAction(nameof(DeleteConfirmation), new
        {
            applicationId = deleteFellingLicenceApplicationModel.ApplicationId, 
            woodlandOwnerId = result.Value, 
            applicationReference = deleteFellingLicenceApplicationModel.ApplicationReference
        });
    }

    [HttpGet]
    public async Task<IActionResult> DeleteConfirmation(Guid applicationId, Guid woodlandOwnerId, string applicationReference,  CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (result.HasValue)
        {
            this.AddErrorMessage("Something went wrong, try again.");

            return RedirectToAction(nameof(ApplicationTaskList), new { applicationId });
        }

        var model = new DeleteConfirmationViewModel
        {
            ApplicationId = applicationId,
            ApplicationReference = applicationReference,
            TaskName = "Deleted"
        };

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = woodlandOwnerId;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SaveAndCompleteLater(SelectedCompartmentsModel model)
    {
        return RedirectToAction(nameof(ApplicationTaskList), new { applicationId = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> ContinueFellingAndRestocking(Guid applicationId)
    {
        return await IterateCompartments(applicationId);
    }

    [HttpGet]
    public async Task<IActionResult> CancelNewApplication(Guid woodlandOwnerId, Guid? agencyId, CreateApplicationAgencySourcePage? agencySourcePage, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var isAgent = user.AccountType == AccountTypeExternal.Agent || user.AccountType == AccountTypeExternal.AgentAdministrator || user.AccountType == AccountTypeExternal.FcUser;

        if (isAgent && agencySourcePage.HasValue)
        {
            Guid agencyIdToUse = agencyId.HasValue ? agencyId.Value : (Guid.TryParse(user.AgencyId, out Guid userAgencyId) ? userAgencyId : Guid.Empty);

            switch (agencySourcePage.Value)
            {
                case CreateApplicationAgencySourcePage.ClientSummary:
                    return RedirectToAction(nameof(HomeController.AgentUser), "Home", new { agencyId = agencyIdToUse });
                case CreateApplicationAgencySourcePage.ClientProperty:
                    return RedirectToAction(nameof(WoodlandOwnerController.ManagedClientSummary), "WoodlandOwner", new { woodlandOwnerId, agencyId = agencyIdToUse });
            }
        }
        else
        {
            return RedirectToAction(nameof(HomeController.WoodlandOwner), "Home", new { woodlandOwnerId });
        }

        return RedirectToAction(nameof(HomeController.Error), "Home");
    }

    private void SetTaskBreadcrumbs(IApplicationWithBreadcrumbsViewModel model)
    {
        var breadCrumbs = DetermineDefaultBreadCrumbs();

        if (model.ApplicationId != Guid.Empty)
        {
            breadCrumbs.Add(
                new BreadCrumb($@"Application {model.ApplicationReference}",
                    "FellingLicenceApplication", "ApplicationTaskList", model.ApplicationId.ToString()));
        }

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = model.TaskName
        };
    }

    private void SetBreadcrumbs(FellingLicenceApplicationModel model)
    {
        var breadCrumbs = DetermineDefaultBreadCrumbs();

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = $@"Application {model.ApplicationSummary.ApplicationReference}"
        };
    }

    private void SetApplicationSummaryBreadcrumbs(FellingLicenceApplicationSummaryViewModel model)
    {
        List<BreadCrumb> breadCrumbs = DetermineDefaultBreadCrumbs();

        if (model.Application.ApplicationId != Guid.Empty)
        {
            breadCrumbs.Add(
                new BreadCrumb($@"Application {model.Application.ApplicationSummary.ApplicationReference}",
                    "FellingLicenceApplication", "ApplicationTaskList", model.Application.ApplicationId.ToString()));
        }

        model.Application.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = $@"Application Summary"
        };
    }

    private List<BreadCrumb> DetermineDefaultBreadCrumbs()
    {
        List<BreadCrumb> breadCrumbs = new List<BreadCrumb>
        {
            new("Home", "Home", "Index", null),
        };

        ExternalApplicant user = new ExternalApplicant(User);

        if (user.AccountType is AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator)
        {
            var woodlandOwnerName =
                string.IsNullOrEmpty(user.WoodlandOwnerName) ? "Woodland Owner" : user.WoodlandOwnerName;

            breadCrumbs.Add(new(woodlandOwnerName, "Home", "WoodlandOwner", null));
        }

        return breadCrumbs;
    }

    private void ValidateModel<T>(T model, IValidator<T> validator, bool createErrors = true)
    {
        if (createErrors)
        {
            ModelState.Clear();
        }
        var validationErrors = validator.Validate(model).Errors;

        if (!validationErrors.Any()) return;

        foreach (var validationFailure in validationErrors)
        {
            //if it's a ProposedRestockingDetailModel and the property name is Species we need to handle it differently for species percentages
            if (model.GetType() == typeof(ProposedRestockingDetailModel))
            {
                var speciesPattern = $"{nameof(ProposedRestockingDetailModel.Species)}\\[\\d+\\]\\.Value";
                if (Regex.IsMatch(validationFailure.PropertyName, speciesPattern))
                {
                    ModelState.AddModelError(validationFailure.FormattedMessagePlaceholderValues["PropertyName"].ToString(), validationFailure.ErrorMessage);
                    continue;
                }
            }

            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }

    private async Task<IActionResult> DecideOnPostFellingDetailRedirect(ProposedFellingDetailModel proposedFellingDetail)
    {
        if (createFellingLicenceApplicationUseCase.FellingOperationRequiresStocking(proposedFellingDetail.OperationType) && !proposedFellingDetail.ReturnToPlayback)
        {
            return RedirectToAction(nameof(DecisionToRestock), "FellingLicenceApplication", new
            {
                applicationId = proposedFellingDetail.ApplicationId,
                fellingCompartmentId = proposedFellingDetail.FellingCompartmentId,
                proposedFellingDetailsId = proposedFellingDetail.Id,
                fellingOperationType = proposedFellingDetail.OperationType
            });
        }
        else
        {
            return await IterateFellingOperationTypesInCompartment(proposedFellingDetail.ApplicationId, proposedFellingDetail.FellingCompartmentId);
        }
    }

    private async Task<IActionResult> IterateRestockingTypesForRestockingCompartment(Guid applicationId, Guid fellingCompartmentId, Guid proposedFellingDetailsId, Guid restockingCompartmentId)
    {
        var applicationStepStatus = await createFellingLicenceApplicationUseCase.GetApplicationStepStatus(applicationId);

        var compartmentStatus = applicationStepStatus.CompartmentFellingRestockingStatuses.FirstOrDefault(c => c.CompartmentId == fellingCompartmentId);

        var fellingStatus = compartmentStatus.FellingStatuses.FirstOrDefault(fs => fs.Id == proposedFellingDetailsId);

        var restockingCompartmentStatus = fellingStatus.RestockingCompartmentStatuses.FirstOrDefault(rcf => rcf.CompartmentId == restockingCompartmentId);

        var unprocessedRestockingsForThisCompartment = restockingCompartmentStatus.RestockingStatuses.Where(rs => rs.Status.HasNoValue() || (rs.Status.HasValue && !rs.Status.Value)).ToList();

        if (unprocessedRestockingsForThisCompartment.Any())
        {
            var restockingToProcess = unprocessedRestockingsForThisCompartment[0];
            return RedirectToAction(nameof(RestockingDetail), new
            {
                applicationId,
                restockingId = restockingToProcess.Id,
                fellingCompartmentId,
                restockingCompartmentId,
                proposedFellingDetailsId
            });
        }
        else
        {
            return await IterateRestockingCompartmentsForFellingOperationType(applicationId, fellingCompartmentId, proposedFellingDetailsId);
        }
    }

    private async Task<IActionResult> IterateRestockingCompartmentsForFellingOperationType(Guid applicationId, Guid fellingCompartmentId, Guid proposedFellingDetailsId)
    {
        var applicationStepStatus = await createFellingLicenceApplicationUseCase.GetApplicationStepStatus(applicationId);

        var compartmentStatus = applicationStepStatus.CompartmentFellingRestockingStatuses.FirstOrDefault(c => c.CompartmentId == fellingCompartmentId);

        var fellingStatus = compartmentStatus.FellingStatuses.FirstOrDefault(fs => fs.Id == proposedFellingDetailsId);

        var unprocessedRestockings = fellingStatus.RestockingCompartmentStatuses.Where(rs => rs.Status.HasNoValue() || rs.Status.HasValue && !rs.Status.Value).ToList();

        if (unprocessedRestockings.Any())
        {
            var restockingToProcess = unprocessedRestockings[0];
            var restockingCompartmentId = restockingToProcess.CompartmentId;

            return RedirectToAction(nameof(SelectRestockingOptions), new 
            { 
                applicationId, 
                fellingCompartmentId, 
                restockingCompartmentId, 
                proposedFellingDetailsId, 
                restockAlternativeArea = restockingCompartmentId != fellingCompartmentId
            });
        }
        else
        {
            // are there restockings that need processing?

            foreach (var restockingCompartmentStatus in fellingStatus.RestockingCompartmentStatuses)
            {
                var completion = restockingCompartmentStatus.OverallCompletion();

                if (completion.HasNoValue() || (completion.HasValue && !completion.Value))
                {
                    return await IterateRestockingTypesForRestockingCompartment(applicationId, fellingCompartmentId, proposedFellingDetailsId, restockingCompartmentStatus.CompartmentId);
                }
            }

            return await IterateFellingOperationTypesInCompartment(applicationId, fellingCompartmentId);
        }
    }

    private async Task<IActionResult> IterateFellingOperationTypesInCompartment(Guid applicationId, Guid fellingCompartmentId)
    {
        var applicationStepStatus = await createFellingLicenceApplicationUseCase.GetApplicationStepStatus(applicationId);

        var compartmentStatus = applicationStepStatus.CompartmentFellingRestockingStatuses.FirstOrDefault(c => c.CompartmentId == fellingCompartmentId);

        var unprocessedFellings = compartmentStatus.FellingStatuses.Where(fs => fs.Status.HasNoValue() || fs.Status.HasValue && !fs.Status.Value).ToList();

        if (unprocessedFellings.Any())
        {
            var fellingToProcess = unprocessedFellings[0];
            var fellingId = fellingToProcess.Id;

            return RedirectToAction(nameof(FellingDetail), new { applicationId, fellingCompartmentId, proposedFellingDetailsId = fellingId });
        }
        else
        {
            // are there any restockings that need processing?

            foreach (var fellingStatus in compartmentStatus.FellingStatuses)
            {
                var completion = fellingStatus.OverallCompletion();

                if (completion.HasNoValue() || (completion.HasValue && !completion.Value))
                {
                    return await IterateRestockingCompartmentsForFellingOperationType(applicationId, compartmentStatus.CompartmentId, fellingStatus.Id);
                }
            }

            return await IterateCompartments(applicationId);
        }
    }

    private async Task<IActionResult> IterateCompartments(Guid applicationId)
    {
        var applicationStepStatus = await createFellingLicenceApplicationUseCase.GetApplicationStepStatus(applicationId);

        var unprocessedCompartments = applicationStepStatus.CompartmentFellingRestockingStatuses.Where(c => c.Status.HasNoValue() || (c.Status.HasValue && ! c.Status.Value)).ToList();

        if (unprocessedCompartments.Any())
        {
            var fellingCompartmentId = unprocessedCompartments[0].CompartmentId;
            return RedirectToAction(nameof(SelectFellingOperationTypes), new { applicationId, fellingCompartmentId });
        }
        else
        {
            // are there any fellings or restockings that aren't processed?
            foreach (var compartment in applicationStepStatus.CompartmentFellingRestockingStatuses)
            {
                var completion = compartment.OverallCompletion();

                if (completion.HasNoValue() || (completion.HasValue && !completion.Value))
                {
                    return await IterateFellingOperationTypesInCompartment(applicationId, compartment.CompartmentId);
                }
            }

            return RedirectToAction(nameof(FellingAndRestockingPlayback), new { applicationId });
        }
    }

}