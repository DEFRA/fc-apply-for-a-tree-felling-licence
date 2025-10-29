using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public partial class AdminOfficerReviewController(
    IEnvironmentalImpactAssessmentAdminOfficerUseCase eiaUseCase)
{

    public async Task<IActionResult> EiaCheck(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, eiaModel) = await eiaUseCase.GetEnvironmentalImpactAssessmentAsync(applicationId, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return eiaModel.EiaDocuments.Any()
            ? await EiaWithFormsPresent(eiaModel, cancellationToken)
            : await EiaWithFormsAbsent(eiaModel, cancellationToken);
    }

    public async Task<IActionResult> EiaWithFormsAbsent(
        EnvironmentalImpactAssessmentModel eiaModel,
        CancellationToken cancellationToken)
    {
        var summary = await eiaUseCase.GetSummaryModel(eiaModel.FellingLicenceApplicationId, cancellationToken);

        if (summary.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var viewModel = new EiaWithFormsAbsentViewModel
        {
            ApplicationId = eiaModel.FellingLicenceApplicationId,
            HaveTheFormsBeenReceived = eiaModel.HasTheEiaFormBeenReceived,
            EiaProcessInLineWithCode = !string.IsNullOrEmpty(eiaModel
                .EiaTrackerReferenceNumber), // this must have been checked for the reference number to be populated
            EiaTrackerReferenceNumber = eiaModel.EiaTrackerReferenceNumber,
            FellingLicenceApplicationSummary = summary.Value
        };

        SetEiaBreadcrumbs(viewModel, eiaModel);

        return View(nameof(EiaWithFormsAbsent), viewModel);
    }


    public async Task<IActionResult> EiaWithFormsPresent(
        EnvironmentalImpactAssessmentModel eiaModel,
        CancellationToken cancellationToken)
    {
        var summary = await eiaUseCase.GetSummaryModel(eiaModel.FellingLicenceApplicationId, cancellationToken);

        if (summary.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var viewModel = new EiaWithFormsPresentViewModel
        {
            ApplicationId = eiaModel.FellingLicenceApplicationId,
            AreTheFormsCorrect = eiaModel.AreAttachedFormsCorrect,
            EiaProcessInLineWithCode =
                !string.IsNullOrEmpty(eiaModel
                    .EiaTrackerReferenceNumber), // this must have been checked for the reference number to be populated
            EiaTrackerReferenceNumber = eiaModel.EiaTrackerReferenceNumber,
            EiaDocumentModels = eiaModel.EiaDocuments,
            FellingLicenceApplicationSummary = summary.Value
        };

        SetEiaBreadcrumbs(viewModel, eiaModel);

        return View(nameof(EiaWithFormsPresent), viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> EiaWithFormsPresent(
        EiaWithFormsPresentViewModel model,
        [FromServices] IValidator<EiaWithFormsPresentViewModel> validator,
        CancellationToken cancellationToken)
    {
        ValidateModel(model, validator);

        if (!ModelState.IsValid)
        {
            var (_, isFailure, eiaModel) = await eiaUseCase.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, cancellationToken);

            if (isFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var summary = await eiaUseCase.GetSummaryModel(eiaModel.FellingLicenceApplicationId, cancellationToken);

            if (summary.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.EiaDocumentModels = eiaModel.EiaDocuments;
            model.FellingLicenceApplicationSummary = summary.Value;
            SetEiaBreadcrumbs(model, eiaModel);

            return View(model);
        }

        var user = new InternalUser(User);

        var result = await eiaUseCase.ConfirmAttachedEiaFormsAreCorrectAsync(model, user.UserAccountId!.Value, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        this.AddConfirmationMessage("Successfully saved EIA check");
        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> EiaWithFormsAbsent(
        EiaWithFormsAbsentViewModel model,
        [FromServices] IValidator<EiaWithFormsAbsentViewModel> validator,
        CancellationToken cancellationToken)
    {
        ValidateModel(model, validator);

        if (!ModelState.IsValid)
        {
            var (_, isFailure, eiaModel) = await eiaUseCase.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, cancellationToken);

            if (isFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var summary = await eiaUseCase.GetSummaryModel(eiaModel.FellingLicenceApplicationId, cancellationToken);

            if (summary.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.FellingLicenceApplicationSummary = summary.Value;

            SetEiaBreadcrumbs(model, eiaModel);

            return View(model);
        }

        var user = new InternalUser(User);

        var result = await eiaUseCase.ConfirmEiaFormsHaveBeenReceivedAsync(model, user.UserAccountId!.Value, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        this.AddConfirmationMessage("Successfully saved EIA check");
        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    private static void SetEiaBreadcrumbs(PageWithBreadcrumbsViewModel model, EnvironmentalImpactAssessmentModel eiaModel)
    {
        model.Breadcrumbs = new BreadcrumbsModel
        {
            CurrentPage = "Check Environmental Impact Assessment (EIA) details",
            Breadcrumbs =
            [
                new BreadCrumb("Open applications",
                    "Home",
                    nameof(HomeController.Index),
                    null),
                new BreadCrumb(eiaModel.ApplicationReference ?? string.Empty,
                    "FellingLicenceApplication",
                    nameof(FellingLicenceApplicationController.ApplicationSummary),
                    eiaModel.FellingLicenceApplicationId.ToString()),
                new BreadCrumb("Admin officer review",
                    "AdminOfficerReview",
                    nameof(Index),
                    eiaModel.FellingLicenceApplicationId.ToString())
            ]
        };
    }
}