using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
public class AssignFellingLicenceApplicationController : Controller
{
    [HttpGet]
    public async Task<IActionResult> ConfirmReassignApplication(
        Guid id,
        AssignedUserRole selectedRole,
        string returnUrl,
        [FromServices] IAssignToUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var model = await useCase.ConfirmReassignApplicationForRole(id, selectedRole, returnUrl, user, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        SetBreadcrumbs(model.Value, "Assign FLA", returnUrl);

        return View(model.Value);
    }

    [HttpGet]
    public async Task<IActionResult> SelectUser(
        Guid id,
        AssignedUserRole selectedRole,
        string returnUrl,
        [FromServices] IAssignToUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var model = await useCase.RetrieveDetailsToAssignFlaToUserAsync(
            id, selectedRole, returnUrl, user, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        SetBreadcrumbs(model.Value, "Assign FLA", returnUrl);
        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SelectUser(
        Guid id,
        AssignToUserModel model,
        [FromServices] IAssignToUserUseCase useCase,
        [FromServices] IUrlHelper urlHelper,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (model.SelectedRole is not (AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer))
        {
            ModelState.Remove(nameof(model.SelectedFcAreaCostCode));
        }

        if (!ModelState.IsValid)
        {
            var reloadModel =
                await useCase.RetrieveDetailsToAssignFlaToUserAsync(
                    id, 
                    model.SelectedRole, 
                    model.ReturnUrl,
                    user, cancellationToken);
            
            reloadModel.Value.SelectedFcAreaCostCode = model.SelectedFcAreaCostCode;

            SetBreadcrumbs(reloadModel.Value, "Assign FLA", model.ReturnUrl);
            return View(reloadModel.Value);
        }

        var linkToApplication = Url.AbsoluteAction("ApplicationSummary", "FellingLicenceApplication", new { id = id });

        var result = await useCase.AssignToUserAsync(
            id, 
            model.SelectedUserId!.Value, 
            model.SelectedRole, 
            model.SelectedFcAreaCostCode, 
            user, 
            linkToApplication,
            model.FormLevelCaseNote.CaseNote,
            model.AdministrativeRegion,
            cancellationToken,
            model.FormLevelCaseNote.VisibleToApplicant,
            model.FormLevelCaseNote.VisibleToConsultee);
         
        if (result.IsSuccess)
        {
            if (urlHelper.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id = id });
        }

        return RedirectToAction("Error", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> AssignBackToApplicant(
        Guid applicationId,
        string? returnUrl,
        [FromServices] IAssignToApplicantUseCase useCase,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            returnUrl = Url.Action("ApplicationSummary", "FellingLicenceApplication", new { id = applicationId });
        }

        var model = await useCase.GetValidExternalApplicantsForAssignmentAsync(internalUser, applicationId, returnUrl, cancellationToken);
        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        SetBreadcrumbs(model.Value, "Assign FLA Back to Applicant", returnUrl);

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AssignBackToApplicant(
        AssignBackToApplicantModel assignBackToApplicantModel,
        [FromServices] IAssignToApplicantUseCase useCase,
        [FromServices] IValidator<AssignBackToApplicantModel> validator,
        [FromServices] IUrlHelper urlHelper,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);
        
        ValidateModel(assignBackToApplicantModel, validator);
        if (!ModelState.IsValid)
        {
            var reloadModel = await useCase.GetValidExternalApplicantsForAssignmentAsync(internalUser, assignBackToApplicantModel.FellingLicenceApplicationId, assignBackToApplicantModel.ReturnUrl, cancellationToken);
            if (reloadModel.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            var model = reloadModel.Value;

            model.ExternalApplicantId = assignBackToApplicantModel.ExternalApplicantId;
            model.ReturnToApplicantComment = model.ReturnToApplicantComment;
            SetBreadcrumbs(model, "Assign FLA Back to Applicant", assignBackToApplicantModel.ReturnUrl);
            return View(model);
        }

        var confirmResult = await useCase.AssignApplicationToApplicantAsync(
            assignBackToApplicantModel.FellingLicenceApplicationId,
            internalUser,
            assignBackToApplicantModel.ExternalApplicantId.Value,
            assignBackToApplicantModel.ReturnToApplicantComment,
            GetFellingLicenceUrlLink(),
            assignBackToApplicantModel.SectionsToReview,
            assignBackToApplicantModel.CompartmentIdentifiersToReview,
            cancellationToken);

        if (confirmResult.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        if (urlHelper.IsLocalUrl(assignBackToApplicantModel.ReturnUrl))
        {
            return Redirect(assignBackToApplicantModel.ReturnUrl);
        }
        
        return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id = assignBackToApplicantModel.FellingLicenceApplicationId });
    }

    private string GetFellingLicenceUrlLink() => Url.AbsoluteAction("ApplicationSummary", "FellingLicenceApplication")!;

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string currentPage, string returnUrl)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new("Open applications", "Home", "Index", null),
            new ($@"Application {model.FellingLicenceApplicationSummary.ApplicationReference}", "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary.Id.ToString())
        };

        if (returnUrl.Contains("AdminOfficerReview"))
        {
            breadCrumbs.Add(new BreadCrumb(
                "Operations Admin Officer Review", "AdminOfficerReview", "Index", model.FellingLicenceApplicationSummary.Id.ToString()));
        }
        if (returnUrl.Contains("WoodlandOfficerReview"))
        {
            breadCrumbs.Add(new BreadCrumb(
                "Woodland Officer Review", "WoodlandOfficerReview", "Index", model.FellingLicenceApplicationSummary.Id.ToString()));
        }

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = currentPage
        };
    }

    private void ValidateModel<T>(T model, IValidator<T> validator)
    {
        var validationErrors = validator.Validate(model).Errors;

        if (validationErrors.NotAny()) return;

        foreach (var validationFailure in validationErrors)
        {
            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }
}

