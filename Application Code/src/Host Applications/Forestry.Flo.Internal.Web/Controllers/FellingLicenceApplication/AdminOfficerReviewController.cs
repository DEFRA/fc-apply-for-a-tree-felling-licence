using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
public class AdminOfficerReviewController : Controller
{
    public async Task<IActionResult> Index(
        Guid id,
        [FromServices] AdminOfficerReviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var hostingPage = Url.Action("Index", new { id })!;

        var model = await useCase.GetAdminOfficerReviewAsync(id, user, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model.Value);
    }

    public async Task<IActionResult> AssignWoodlandOfficer(
        Guid id,
        [FromServices] AdminOfficerReviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var hostingPage = Url.Action("Index", new { id });

        var model = await useCase.GetAdminOfficerReviewAsync(id, user, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        if (string.IsNullOrWhiteSpace(model.Value.AssignedWoodlandOfficer) == false)
        {
            return RedirectToAction("ConfirmReassignApplication", "AssignFellingLicenceApplication", new
            {
                id,
                selectedRole = AssignedUserRole.WoodlandOfficer,
                returnUrl = hostingPage
            });
        }

        return RedirectToAction("SelectUser", "AssignFellingLicenceApplication", new
        {
            id,
            selectedRole = AssignedUserRole.WoodlandOfficer,
            returnUrl = hostingPage
        });
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmAdminOfficerReview(
        AdminOfficerReviewModel model,
        [FromServices] AdminOfficerReviewUseCase useCase,
        [FromServices] IClock clock,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (string.IsNullOrWhiteSpace(model.AssignedWoodlandOfficer))
        {
            this.AddErrorMessage("A Woodland Officer must be assigned before completing the review");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        // if the application wasn't input by an external applicant user, check that the date received has been populated and is not in the future
        if (model.ApplicationSource != FellingLicenceApplicationSource.ApplicantUser)
        {
            if (model.DateReceived?.IsPopulated() is not true)
            {
                this.AddErrorMessage(
                    "The date the application was received must be provided for applications that weren't created by the external applicant");
                return RedirectToAction("Index", new { id = model.ApplicationId });
            }

            if (model.DateReceived.CalculateDate().ToUniversalTime().Date >
                clock.GetCurrentInstant().ToDateTimeUtc().Date)
            {
                this.AddErrorMessage("The date the application was received cannot be in the future");
                return RedirectToAction("Index", new { id = model.ApplicationId });
            }
        }

        var internalLinkToApplication = Url.Action("ApplicationSummary", "FellingLicenceApplication",
            new { id = model.ApplicationId }, this.Request.Scheme)!;

        var result = await useCase.ConfirmAdminOfficerReview(
            model.ApplicationId,
            user,
            internalLinkToApplication,
            model.DateReceived!.CalculateDate().ToUniversalTime(),
            model.AgentApplication,
            cancellationToken);

        //TODO if the notifications failed but the rest worked, we're asking the user to try again but the application _has_ transitioned to WO Review state?
        if (result.IsFailure)
        {
            this.AddErrorMessage("Could not complete the operations admin officer review, please try again");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Operations admin officer review submitted");
        return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> AgentAuthorityFormCheck(
        Guid id,
        [FromServices] AgentAuthorityFormCheckUseCase useCase,
        CancellationToken cancellationToken)
    {
        var model = await useCase.GetAgentAuthorityFormCheckModelAsync(id, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve agent authority form");
            return RedirectToAction("Index", new { id = id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AgentAuthorityFormCheck(
        AgentAuthorityFormCheckModel model,
        [FromServices] AgentAuthorityFormCheckUseCase useCase,
        [FromServices] IValidator<AgentAuthorityFormCheckModel> validator,
        CancellationToken cancellationToken)
    {
        var performingUser = new InternalUser(User);

        ValidateModel(model, validator);
        if (ModelState.IsValid is false)
        {
            var newModel = await useCase.GetAgentAuthorityFormCheckModelAsync(model.ApplicationId, cancellationToken);

            if (newModel.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            newModel.Value.CheckFailedReason = model.CheckFailedReason;
            newModel.Value.CheckPassed = model.CheckPassed;

            return View(newModel.Value);
        }

        // only provide a failure reason for failed checks
        var failureReason = model.CheckPassed!.Value 
            ? null
            : model.CheckFailedReason;

        var result = await useCase.CompleteAgentAuthorityCheckAsync(
            model.ApplicationId,
            performingUser.UserAccountId!.Value,
            model.CheckPassed.Value,
            failureReason,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to complete agent authority check");
            return RedirectToAction(nameof(AgentAuthorityFormCheck), new { id = model.ApplicationId });
        }

        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> MappingCheck(
        Guid id,
        [FromServices] MappingCheckUseCase useCase,
        CancellationToken cancellationToken)
    {
        var model = await useCase.GetMappingCheckModelAsync(id, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve mapping check status");
            return RedirectToAction("Index", new { id = id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> MappingCheck(
        MappingCheckModel model,
        [FromServices] MappingCheckUseCase useCase,
        [FromServices] IValidator<MappingCheckModel> validator,
        CancellationToken cancellationToken)
    {
        var performingUser = new InternalUser(User);

        ValidateModel(model, validator);
        if (ModelState.IsValid is false)
        {
            var newModel = await useCase.GetMappingCheckModelAsync(model.ApplicationId, cancellationToken);

            if (newModel.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            newModel.Value.CheckFailedReason = model.CheckFailedReason;
            newModel.Value.CheckPassed = model.CheckPassed;

            return View(newModel.Value);
        }

        // only provide a failure reason for failed checks
        var failureReason = model.CheckPassed!.Value
            ? null
            : model.CheckFailedReason;

        var result = await useCase.CompleteMappingCheckAsync(
            model.ApplicationId,
            performingUser.UserAccountId!.Value,
            model.CheckPassed.Value,
            failureReason,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to complete mapping check");
            return RedirectToAction(nameof(MappingCheck), new { id = model.ApplicationId });
        }

        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> ConstraintsCheck(
    Guid id,
    [FromServices] ConstraintsCheckUseCase useCase,
    [FromServices] AdminOfficerReviewUseCase adminOfficerReviewUseCase,
    CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var adminOfficerReview = await adminOfficerReviewUseCase.GetAdminOfficerReviewAsync(id, user, string.Empty, cancellationToken);

        if (adminOfficerReview.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (adminOfficerReview.Value.AdminOfficerReviewTaskListStates.ConstraintsCheckStepStatus is
            InternalReviewStepStatus.CannotStartYet)
        {
            this.AddErrorMessage("Constraints check cannot be started yet");
            return RedirectToAction(nameof(Index), new { id = id });
        }

        var model = await useCase.GetConstraintsCheckModel(id, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve constraints check status");
            return RedirectToAction("Index", new { id = id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> ConstraintsCheck(
        ConstraintsCheckModel model,
        [FromServices] ConstraintsCheckUseCase useCase,
        CancellationToken cancellationToken)
    {
        var performingUser = new InternalUser(User);
        var newModel = await useCase.GetConstraintsCheckModel(model.ApplicationId, cancellationToken);

        if (newModel.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (ModelState.IsValid is false)
        {
            return View(newModel.Value);
        }

        // only update the admin officer review entity if a LIS report is found

        if (newModel.Value.FellingLicenceApplicationSummary!.MostRecentFcLisReport.HasNoValue && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
        {
            this.AddErrorMessage("Cannot complete the constraints check unless a report has been generated");
            return View(newModel.Value);
        }

        var isAgencyApplication = !string.IsNullOrEmpty(newModel.Value.FellingLicenceApplicationSummary.AgentOrAgencyName);

        var result = await useCase.CompleteConstraintsCheckAsync(
            model.ApplicationId,
            isAgencyApplication,
            performingUser.UserAccountId!.Value,
            model.IsComplete,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to complete constraints check");
            return RedirectToAction(nameof(ConstraintsCheck), new { id = model.ApplicationId });
        }

        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> LarchCheck(
        Guid id,
        [FromServices] LarchCheckUseCase useCase,
        [FromServices] FellingLicenceApplicationUseCase applicationUseCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var model = await useCase.GetLarchCheckModelAsync(id, user, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve mapping check status");
            return RedirectToAction("Index", new { id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> LarchCheck(
        Guid id,
        LarchCheckModel model,
        [FromServices] LarchCheckUseCase useCase,
        [FromServices] AdminOfficerReviewUseCase adminOfficerUseCase,
        [FromServices] IAmendCaseNotes amendCaseNotes,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (ModelState.IsValid is false)
        {
            var newModel = await useCase.GetLarchCheckModelAsync(id, user, cancellationToken);

            if (newModel.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            return View(newModel.Value);
        }

        var resultLarch = await useCase.SaveLarchCheckAsync(
            model,
            user.UserAccountId!.Value,
            cancellationToken);

        if (resultLarch.IsFailure)
        {
            this.AddErrorMessage("Unable to complete Larch check");
            return RedirectToAction(nameof(LarchCheck), new { id = model.ApplicationId });
        }

        var result = await adminOfficerUseCase.CompleteLarchCheckAsync(
            model.ApplicationId,
            user.UserAccountId!.Value,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to complete Larch check");
            return RedirectToAction(nameof(LarchCheck), new { id = model.ApplicationId });
        }

        if (!string.IsNullOrWhiteSpace(model.CaseNote))
        {
            var caseNoteRecord = new AddCaseNoteRecord(
                FellingLicenceApplicationId: model.ApplicationId,
                Type: CaseNoteType.LarchCheckComment,
                Text: model.CaseNote,
                VisibleToApplicant: model.VisibleToApplicant,
                VisibleToConsultee: model.VisibleToConsultee
            );
            var caseNoteresult = await amendCaseNotes.AddCaseNoteAsync(caseNoteRecord, user.UserAccountId.Value, cancellationToken);

            if (caseNoteresult.IsFailure)
            {
                this.AddErrorMessage(result.Error);
                return RedirectToAction(nameof(LarchFlyover), new { id = model.ApplicationId });
            }
        }

        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> LarchFlyover(
        Guid id,
        [FromServices] LarchCheckUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var model = await useCase.GetLarchFlyoverModelAsync(id, user, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve mapping check status");
            return RedirectToAction("Index", new { id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> LarchFlyover(
        Guid id,
        LarchFlyoverModel model,
        [FromServices] LarchCheckUseCase useCase,
        [FromServices] IAmendCaseNotes amendCaseNotes,
        [FromServices] IValidator<LarchFlyoverModel> larchFlyoverValidator,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        ValidateModel(model, larchFlyoverValidator);

        if (ModelState.IsValid is false)
        {
            var hostingPage = Url.Action("LarchFlyover", new { id })!;
            var newModel = await useCase.GetLarchFlyoverModelAsync(id, user, cancellationToken);
            if (newModel.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.ActivityFeedItems = newModel.Value.ActivityFeedItems;
            return View(model);
        }

        var result = await useCase.SaveLarchFlyoverAsync(
            model,
            user.UserAccountId!.Value,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage(result.Error);
            return RedirectToAction(nameof(LarchFlyover), new { id = model.ApplicationId });
        }

        if (!string.IsNullOrWhiteSpace(model.CaseNote))
        {
            var caseNoteRecord = new AddCaseNoteRecord(
                FellingLicenceApplicationId: model.ApplicationId,
                Type: CaseNoteType.LarchCheckComment,
                Text: model.CaseNote,
                VisibleToApplicant: model.VisibleToApplicant,
                VisibleToConsultee: model.VisibleToConsultee
            );

            var caseNoteresult = await amendCaseNotes.AddCaseNoteAsync(caseNoteRecord, user.UserAccountId.Value, cancellationToken);

            if (caseNoteresult.IsFailure)
            {
                this.AddErrorMessage(result.Error);
                return RedirectToAction(nameof(LarchFlyover), new { id = model.ApplicationId });
            }
        }

        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> CBWCheck(
    Guid id,
    [FromServices] CBWCheckUseCase useCase,
    CancellationToken cancellationToken)
    {
        var model = await useCase.GetCBWCheckModelAsync(id, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve mapping check status");
            return RedirectToAction("Index", new { id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CBWCheck(
        CBWCheckModel model,
        [FromServices] CBWCheckUseCase useCase,
        [FromServices] IAmendCaseNotes amendCaseNotes,
        CancellationToken cancellationToken)
    {
        var performingUser = new InternalUser(User);

        if (ModelState.IsValid is false)
        {
            var newModel = await useCase.GetCBWCheckModelAsync(model.ApplicationId, cancellationToken);

            if (newModel.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            newModel.Value.CheckFailedReason = model.CheckFailedReason;
            newModel.Value.CheckPassed = model.CheckPassed;

            return View(newModel.Value);
        }

        // only provide a failure reason for failed checks
        var failureReason = model.CheckPassed!.Value
            ? null
            : model.CheckFailedReason;

        var result = await useCase.CompleteCBWCheckAsync(
            model.ApplicationId,
            performingUser.UserAccountId!.Value,
            model.CheckPassed.Value,
            failureReason,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to complete mapping check");
            return RedirectToAction(nameof(MappingCheck), new { id = model.ApplicationId });
        }

        if (!string.IsNullOrWhiteSpace(model.CaseNote))
        {
            var caseNoteRecord = new AddCaseNoteRecord(
                FellingLicenceApplicationId: model.ApplicationId,
                Type: CaseNoteType.CBWCheckComment,
                Text: model.CaseNote,
                VisibleToApplicant: model.VisibleToApplicant,
                VisibleToConsultee: model.VisibleToConsultee
            );
            var caseNoteresult = await amendCaseNotes.AddCaseNoteAsync(caseNoteRecord, performingUser.UserAccountId.Value, cancellationToken);

            if (caseNoteresult.IsFailure)
            {
                this.AddErrorMessage(caseNoteresult.Error);
                return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
            }
        }

        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
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