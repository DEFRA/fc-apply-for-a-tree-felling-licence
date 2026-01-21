using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
public class ApprovedInErrorController : Controller
{
    private readonly ILogger<ApprovedInErrorController> _logger;
    private readonly IApprovedInErrorUseCase _approvedInError;

    public ApprovedInErrorController(
        IApprovedInErrorUseCase approvedInError,
        ILogger<ApprovedInErrorController> logger)
    {
        ArgumentNullException.ThrowIfNull(approvedInError);
        _approvedInError = approvedInError;
        _logger = logger ?? new NullLogger<ApprovedInErrorController>();
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid id,
        CancellationToken cancellationToken)
    {
        var model = await _approvedInError.RetrieveApprovedInErrorAsync(
            id,
            new InternalUser(User),
            cancellationToken);

        if (model.HasNoValue)
        {
            _logger.LogError("Failed to retrieve Approved in error details for application with ID {ApplicationId}", id);
            return RedirectToAction("Error", "Home");
        }

        if (model.Value.FellingLicenceApplicationSummary!.Status != FellingLicenceStatus.Approved)
        {
            _logger.LogWarning("Approved in error requested for application with ID {ApplicationId} that is not in the Approved status", id);
            return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id });
        }

        model.Value.Breadcrumbs = new BreadcrumbsModel
        {
            CurrentPage = "Approved in error",
            Breadcrumbs = new List<BreadCrumb>() { 
                new("Open applications", "Home", "Index", null),  
                new(model.Value.FellingLicenceApplicationSummary!.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", id.ToString())
            }
        };

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmApprovedInError(
        ApprovedInErrorViewModel model,
        [FromServices] IApprovedInErrorUseCase useCase,
        [FromServices] IValidator<ApprovedInErrorViewModel> validator,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving Approved in error for application with ID {ApplicationId}", model.Id);

        ValidateModel(model, validator);

        if (!ModelState.IsValid)
        {
            return await ReloadIndexViewAsync(model, useCase, cancellationToken);
        }

        var user = new InternalUser(User);
        var aie = new ApprovedInErrorModel
        {
            ApplicationId = model.Id,
            PreviousReference = model.PreviousReference,
            ReasonExpiryDate = model.ReasonExpiryDate,
            ReasonSupplementaryPoints = model.ReasonSupplementaryPoints,
            ReasonOther = model.ReasonOther,
            ReasonOtherText = model.ReasonOtherText,
            ReasonExpiryDateText = model.ReasonExpiryDateText
        };

        var saveResult = await useCase.ConfirmApprovedInErrorAsync(
            aie,
            user,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            _logger.LogWarning("Validation failed for Approved in error for application with ID {ApplicationId}. Errors: {Errors}", model.Id, saveResult.Error);
            this.AddErrorMessage(saveResult.Error);
            return await ReloadIndexViewAsync(model, useCase, cancellationToken);
        }

        this.AddConfirmationMessage($"Approved in error process successfully started for application with previous reference {model.PreviousReference}");
        return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> ReApproveInError(Guid id, CancellationToken cancellationToken)
    {
        var model = await _approvedInError.RetrieveReApproveInErrorAsync(
            id,
            new InternalUser(User),
            cancellationToken);

        if (model.HasNoValue)
        {
            _logger.LogError("Failed to retrieve Re-approve in error details for application with ID {ApplicationId}", id);
            return RedirectToAction("Error", "Home");
        }

        model.Value.IsReadonly = model.Value.FellingLicenceApplicationSummary!.Status != FellingLicenceStatus.ApprovedInError;

        model.Value.Breadcrumbs = new BreadcrumbsModel
        {
            CurrentPage = "Approved in error",
            Breadcrumbs = new List<BreadCrumb>() {
                new("Open applications", "Home", "Index", null),
                new(model.Value.FellingLicenceApplicationSummary!.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", id.ToString())
            }
        };

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> ReApproveInError(
        ReApproveInErrorViewModel model,
        [FromServices] IApprovedInErrorUseCase useCase,
        [FromServices] IValidator<ReApproveInErrorViewModel> validator,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing Re-approve in error for application with ID {ApplicationId}", model.Id);

        ValidateModel(model, validator);

        if (!ModelState.IsValid)
        {
            return await ReloadReApproveInErrorViewAsync(model, useCase, cancellationToken);
        }

        var internalUser = new InternalUser(User);

        // Convert DatePart to DateTime for the licence expiry date
        DateTime? licenceExpiryDate = null;
        if (model.NewLicenceExpiryDate is not null)
        {
            // Create DateTime with UTC kind for PostgreSQL compatibility
            // Validation has already ensured this is a valid date
            licenceExpiryDate = new DateTime(
                model.NewLicenceExpiryDate.Year,
                model.NewLicenceExpiryDate.Month,
                model.NewLicenceExpiryDate.Day,
                0, 0, 0, DateTimeKind.Utc);
        }

        // Create ApprovedInErrorModel with all the corrected details
        var approvedInErrorModel = new ApprovedInErrorModel
        {
            ApplicationId = model.Id,
            PreviousReference = model.ApprovedInErrorViewModel.PreviousReference,
            ReasonExpiryDate = model.ApprovedInErrorViewModel.ReasonExpiryDate,
            ReasonExpiryDateText = model.ApprovedInErrorViewModel.ReasonExpiryDateText,
            LicenceExpiryDate = licenceExpiryDate,
            ReasonSupplementaryPoints = model.ApprovedInErrorViewModel.ReasonSupplementaryPoints,
            SupplementaryPointsText = model.ApprovedInErrorViewModel.SupplementaryPointsText,
            ReasonOther = model.ApprovedInErrorViewModel.ReasonOther,
            ReasonOtherText = model.ApprovedInErrorViewModel.ReasonOtherText,
            ApproverId = internalUser.UserAccountId!.Value
        };

        // Call the use case method to handle the transaction, update, and PDF generation
        var result = await useCase.ReApprovedInErrorAsync(
            model.Id,
            internalUser,
            approvedInErrorModel,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to re-approve application {ApplicationId}. Error: {Error}", model.Id, result.Error);
            this.AddErrorMessage(result.Error);
            return RedirectToAction(nameof(ReApproveInError), new { id = model.Id });
        }

        this.AddConfirmationMessage($"Licence successfully updated and re-issued for application {model.FellingLicenceApplicationSummary?.ApplicationReference}");
        return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { model.Id });
    }

    private async Task<IActionResult> ReloadIndexViewAsync(
        ApprovedInErrorViewModel model,
        IApprovedInErrorUseCase useCase,
        CancellationToken cancellationToken)
    {
        var reloadModel = await useCase.RetrieveApprovedInErrorAsync(
            model.Id,
            new InternalUser(User),
            cancellationToken);

        if (reloadModel.HasNoValue)
        {
            _logger.LogError("Failed to reload Approved in error details for application with ID {ApplicationId} after validation failure", model.Id);
            return RedirectToAction("Error", "Home");
        }

        reloadModel.Value.ApplicationId = model.ApplicationId;
        reloadModel.Value.PreviousReference = model.PreviousReference;
        reloadModel.Value.ReasonExpiryDate = model.ReasonExpiryDate;
        reloadModel.Value.ReasonSupplementaryPoints = model.ReasonSupplementaryPoints;
        reloadModel.Value.ReasonOther = model.ReasonOther;
        reloadModel.Value.ReasonExpiryDateText = model.ReasonExpiryDateText;
        reloadModel.Value.Breadcrumbs = new BreadcrumbsModel
        {
            CurrentPage = "Approver Review",
            Breadcrumbs = new List<BreadCrumb>() {
                new("Open applications", "Home", "Index", null),
                new(reloadModel.Value.FellingLicenceApplicationSummary!.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.Id.ToString())
            }
        };

        return View(nameof(Index), reloadModel.Value);
    }

    private async Task<IActionResult> ReloadReApproveInErrorViewAsync(
        ReApproveInErrorViewModel model,
        IApprovedInErrorUseCase useCase,
        CancellationToken cancellationToken)
    {
        var reloadModel = await useCase.RetrieveReApproveInErrorAsync(
            model.Id,
            new InternalUser(User),
            cancellationToken);

        if (reloadModel.HasNoValue)
        {
            _logger.LogError("Failed to reload Re-approve in error details for application with ID {ApplicationId} after validation failure", model.Id);
            return RedirectToAction("Error", "Home");
        }

        var approvedInErrorModel = await useCase.RetrieveApprovedInErrorAsync(
            model.Id,
            new InternalUser(User),
            cancellationToken);

        if (approvedInErrorModel.HasNoValue)
        {
            _logger.LogError("Failed to retrieve Approved in error details for application with ID {ApplicationId}", model.Id);
            return RedirectToAction("Error", "Home");
        }

        reloadModel.Value.ApprovedInErrorViewModel = approvedInErrorModel.Value;
        reloadModel.Value.NewLicenceExpiryDate = model.NewLicenceExpiryDate;
        
        // Preserve user input from the posted form - check for null to avoid NullReferenceException
        if (model.ApprovedInErrorViewModel != null)
        {
            if (model.ApprovedInErrorViewModel.ReasonExpiryDateText != null)
            {
                reloadModel.Value.ApprovedInErrorViewModel.ReasonExpiryDateText = model.ApprovedInErrorViewModel.ReasonExpiryDateText;
            }
            if (model.ApprovedInErrorViewModel.SupplementaryPointsText != null)
            {
                reloadModel.Value.ApprovedInErrorViewModel.SupplementaryPointsText = model.ApprovedInErrorViewModel.SupplementaryPointsText;
            }
        }
        
        reloadModel.Value.Breadcrumbs = new BreadcrumbsModel
        {
            CurrentPage = "Edit duplicate licence created in error",
            Breadcrumbs = new List<BreadCrumb>() {
                new("Open applications", "Home", "Index", null),
                new(reloadModel.Value.FellingLicenceApplicationSummary!.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.Id.ToString())
            }
        };

        return View(nameof(ReApproveInError), reloadModel.Value);
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

