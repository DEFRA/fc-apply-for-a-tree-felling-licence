using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.Reports;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Reports;
using Forestry.Flo.Services.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class ReportsController : Controller
{
    private readonly ILogger<ReportsController> _logger;
    private readonly List<BreadCrumb> _breadCrumbsRoot = new()
    {
        new ("Home", "Home", "Index", null)
    };

    public ReportsController(ILogger<ReportsController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "User management"
        };
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> FellingLicenceApplicationsDataReport(
        [FromServices] GenerateReportUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var defaultModel = await useCase.GetReferenceModelAsync(user, addDefaultDates: true, cancellationToken);
        
        if (defaultModel.IsFailure)
        {
            _logger.LogError("Could not produce view model for report UI error is [{error}]", defaultModel.Error);
            return RedirectToAction("Error", "Home");
        }

        return View(defaultModel.Value);
    }
    
    [HttpPost]
    public async Task<IActionResult> SubmitFellingLicenceApplicationsDataReport(
        ReportRequestViewModel viewModel,
        [FromServices] GenerateReportUseCase useCase,
        [FromServices] IValidator<ReportRequestViewModel> reportValidator,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var referenceModel = await useCase.GetReferenceModelAsync(user, addDefaultDates: false, cancellationToken);

        if (referenceModel.IsSuccess)
        {
            viewModel.AdminHubs = referenceModel.Value.AdminHubs;
            viewModel.ConfirmedFcUsers = referenceModel.Value.ConfirmedFcUsers;
        }
        else
        {
            _logger.LogError("Could not produce view model for report UI error is [{error}]", referenceModel.Error);
            return RedirectToAction("Error", "Home");
        }

        ValidateModel(viewModel, reportValidator);
        
        if (!ModelState.IsValid)
        {
            return View(nameof(FellingLicenceApplicationsDataReport), viewModel);
        }

        var (isSuccess, _, actionResult) = await useCase.GenerateReportAsync(viewModel, user, cancellationToken);

        if (isSuccess)
        {
            switch (actionResult)
            {
                case FileStreamResult:
                    return actionResult;
                default:
                    this.AddUserGuide("No data was found. Adjust your criteria and retry.");
                    break;
            }
        }
        else
        {
            this.AddErrorMessage("Unable to create report at this time. Try again. If this problem persists please contact support.");
        }
        return View(nameof(FellingLicenceApplicationsDataReport), viewModel);
    }

    private void ValidateModel<T>(T model, IValidator<T> validator, bool createErrors = true)
    {
        if (createErrors)
        {
            ModelState.Clear();
        }
        var validationErrors = validator.Validate(model).Errors;

        if (validationErrors.NotAny()) return;

        foreach (var validationFailure in validationErrors)
        {
            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }
}
