using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using AssignedUserRole = Forestry.Flo.Services.FellingLicenceApplications.Entities.AssignedUserRole;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
public partial class WoodlandOfficerReviewController : Controller
{
    private readonly IValidator<CompartmentConfirmedFellingRestockingDetailsModel> _fellingAndRestockingDetailsModelValidator;

    public WoodlandOfficerReviewController(
        IValidator<CompartmentConfirmedFellingRestockingDetailsModel> fellingAndRestockingDetailsModelValidator)
    {
        _fellingAndRestockingDetailsModelValidator = fellingAndRestockingDetailsModelValidator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid id,
        [FromServices]WoodlandOfficerReviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var hostingPage = Url.Action("Index", new { id = id });

        var model = await useCase.WoodlandOfficerReviewAsync(id, user, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmWoodlandOfficerReview(
        WoodlandOfficerReviewModel model,
        [FromServices] WoodlandOfficerReviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (string.IsNullOrWhiteSpace(model.AssignedFieldManager))
        {
            this.AddErrorMessage("A Field Manager must be assigned before completing the review");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        if (model.RecommendationForDecisionPublicRegister.HasNoValue())
        {
            this.AddErrorMessage("A recommendation for whether to publish to the decision public register must be provided");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        if (model.RecommendedLicenceDuration is null or RecommendedLicenceDuration.None)
        {
            this.AddErrorMessage("A recommended licence duration must be provided");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        //retrieve the review from the system - rather than rely on posted data from view to ascertain completeness.
        var getWoodlandOfficeReview = await useCase.WoodlandOfficerReviewAsync(model.ApplicationId, user, string.Empty, cancellationToken);

        if (getWoodlandOfficeReview.IsFailure)
        {
            this.AddErrorMessage("Unable to retrieve the required woodland officer data for completing the review");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        if (getWoodlandOfficeReview.IsSuccess 
            && getWoodlandOfficeReview.Value.WoodlandOfficerReviewTaskListStates.AllAreComplete() == false)
        {
            this.AddErrorMessage("All of the woodland officer review tasks must be completed before completing the review");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }
        
        var internalLinkToApplication = Url.Action("ApplicationSummary", "FellingLicenceApplication", new { id = model.ApplicationId }, this.Request.Scheme);

        var result = await useCase.CompleteWoodlandOfficerReviewAsync(
            model.ApplicationId, model.RecommendedLicenceDuration, model.RecommendationForDecisionPublicRegister, internalLinkToApplication, user, cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage(result.Error);
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Woodland Officer review submitted");
        return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id = model.ApplicationId });
    }

    public async Task<IActionResult> AssignFieldManager(
        Guid id,
        [FromServices]WoodlandOfficerReviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var hostingPage = Url.Action("Index", new { id = id });

        var model = await useCase.WoodlandOfficerReviewAsync(id, user, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        if (string.IsNullOrWhiteSpace(model.Value.AssignedFieldManager) == false)
        {
            return RedirectToAction("ConfirmReassignApplication", "AssignFellingLicenceApplication", new
            {
                id,
                selectedRole = AssignedUserRole.FieldManager,
                returnUrl = hostingPage
            });
        }

        return RedirectToAction("SelectUser", "AssignFellingLicenceApplication", new
        {
            id,
            selectedRole = AssignedUserRole.FieldManager,
            returnUrl = hostingPage
        });
    }

    [HttpGet]
    public async Task<IActionResult> PublicRegister(
        Guid id,
        [FromServices] PublicRegisterUseCase useCase,
        bool? withExemption,
        CancellationToken cancellationToken)
    {
        var model = await useCase.GetPublicRegisterDetailsAsync(id, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        if (withExemption.HasValue)
        {
            model.Value.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = withExemption.Value;
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SaveExemption(
        PublicRegisterViewModel model,
        [FromServices] PublicRegisterUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (model.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister 
            && string.IsNullOrWhiteSpace(model.PublicRegister.WoodlandOfficerConsultationPublicRegisterExemptionReason))
        {
            this.AddErrorMessage("A reason must be provided when the application is exempt from the public register", nameof(model.PublicRegister.WoodlandOfficerConsultationPublicRegisterExemptionReason));

            return RedirectToAction("PublicRegister", new { id = model.ApplicationId, withExemption = true });
        }

        var user = new InternalUser(User);

        var result = await useCase.StorePublicRegisterExemptionAsync(
            model.ApplicationId,
            model.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister,
            model.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister ?  model.PublicRegister.WoodlandOfficerConsultationPublicRegisterExemptionReason : null,
            user,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong saving the public register exemption, please try again");
            return RedirectToAction("PublicRegister", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Public register exemption saved");
        return RedirectToAction("PublicRegister", new { id = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> PublishToConsultationPublicRegister(
        PublicRegisterViewModel model,
        [FromServices] PublicRegisterUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (model.PublicRegister?.ConsultationPublicRegisterPeriodDays.HasNoValue() ?? true)
        {
            this.AddErrorMessage("The period for the application to be on the public register must be provided", nameof(model.PublicRegister.ConsultationPublicRegisterPeriodDays));
            return RedirectToAction("PublicRegister", new { id = model.ApplicationId });
        }

        var user = new InternalUser(User);

        var result = await useCase.PublishToConsultationPublicRegisterAsync(
            model.ApplicationId,
            TimeSpan.FromDays(model.PublicRegister.ConsultationPublicRegisterPeriodDays!.Value), 
            user,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong publishing the application to the consultation public register, please try again");
            return RedirectToAction("PublicRegister", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Published to consultation public register");
        return RedirectToAction("PublicRegister", new { id = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromPublicRegister(
        PublicRegisterViewModel model,
        [FromServices] PublicRegisterUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var result = await useCase.RemoveFromPublicRegisterAsync(
            model.ApplicationId,
            user,
            model.RemoveFromPublicRegister.EsriId.Value,
            model.RemoveFromPublicRegister.ApplicationReference,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong removing the application from the public register, please try again");
            return RedirectToAction("PublicRegister", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Removed from consultation public register");
        return RedirectToAction("PublicRegister", new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> SiteVisit(
        Guid id,
        [FromServices] SiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var hostingPage = Url.Action("SiteVisit", new { id = id });

        var model = await useCase.GetSiteVisitDetailsAsync(id, user, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SiteVisitNotNeeded(
        SiteVisitViewModel model,
        [FromServices] SiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (string.IsNullOrWhiteSpace(model.SiteVisitNotNeededReason))
        {
            this.AddErrorMessage("A reason must be provided when a site visit is not required", nameof(model.SiteVisitNotNeededReason));
            return RedirectToAction("SiteVisit", new { id = model.ApplicationId });
        }

        var result = await useCase.SiteVisitIsNotNeededAsync(
            model.ApplicationId,
            user,
            model.SiteVisitNotNeededReason,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong saving the site visit details, please try again");
            return RedirectToAction("SiteVisit", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Site visit not needed reason saved");
        return RedirectToAction("SiteVisit", new { id = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> GenerateSiteVisitArtefacts(
        SiteVisitViewModel model,
        [FromServices] GeneratePdfApplicationUseCase pdfUseCase,
        [FromServices] SiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        // If no application document has been generated yet, then do so.
        if (model.ApplicationDocumentHasBeenGenerated == false)
        {
            var createDocResult = await pdfUseCase.GeneratePdfApplicationAsync(
                user, model.ApplicationId, false, cancellationToken);
            if (createDocResult.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        var result = await useCase.GenerateSiteVisitArtefactsAsync(
            model.ApplicationId,
            user,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong starting the site visit process, please try again");
            return RedirectToAction("SiteVisit", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Site visit process started");
        return RedirectToAction("SiteVisit", new { id = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> RetrieveSiteVisitNotes(
        SiteVisitViewModel model,
        [FromServices] SiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var result = await useCase.RetrieveSiteVisitNotesAsync(
            model.ApplicationId,
            model.ApplicationReference,
            user,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong completing the site visit process, please try again");
            return RedirectToAction("SiteVisit", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Site visit notes retrieved");
        return RedirectToAction("Index", new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> Pw14Checks(
        Guid id,
        [FromServices] Pw14UseCase useCase,
        CancellationToken cancellationToken)
    {
        var model = await useCase.GetPw14CheckDetailsAsync(id, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Pw14Checks(
        Pw14ChecksViewModel model,
        [FromServices] Pw14UseCase useCase,
        [FromServices] IAmendCaseNotes amendCaseNotes,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        model.Pw14Checks ??= new Pw14ChecksModel();
        var result = await useCase.SavePw14ChecksAsync(model, user, cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong saving the Woodland officer checks, please try again");
            return RedirectToAction("Pw14Checks", new { id = model.ApplicationId });
        }

        if (!string.IsNullOrWhiteSpace(model.FormLevelCaseNote.CaseNote))
        {
            var caseNoteRecord = new AddCaseNoteRecord(
                FellingLicenceApplicationId: model.ApplicationId,
                Type: CaseNoteType.WoodlandOfficerReviewComment,
                Text: model.FormLevelCaseNote.CaseNote,
                VisibleToApplicant: model.FormLevelCaseNote.VisibleToApplicant,
                VisibleToConsultee: model.FormLevelCaseNote.VisibleToConsultee
            );

            var caseNoteResult = await amendCaseNotes.AddCaseNoteAsync(caseNoteRecord, user.UserAccountId!.Value, cancellationToken);

            if (caseNoteResult.IsFailure)
            {
                // No need for a transaction here as the user can retry adding the case note from the index page
                this.AddErrorMessage("Unable to save case note. The Woodland officer checks have still been saved");
                return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
            }
        }

        this.AddConfirmationMessage("Woodland officer checks saved");
        return RedirectToAction("Index", new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> Conditions(
        Guid id,
        [FromServices] ConditionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var model = await useCase.GetConditionsAsync(id, user, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not load conditions");
            return RedirectToAction("Index", new { id = id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SaveConditionalStatus(
        ConditionsViewModel model,
        [FromServices] ConditionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (model.ConditionsStatus.IsConditional.HasNoValue())
        {
            this.AddErrorMessage("Select whether or not the application is Conditional");
            return RedirectToAction("Conditions");
        }

        var saveResult = await useCase.SaveConditionStatusAsync(model.ApplicationId, user, model.ConditionsStatus, cancellationToken);

        if (saveResult.IsFailure)
        {
            this.AddErrorMessage("Could not update conditional status for application");
        }

        return RedirectToAction("Conditions", new { id = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> GenerateConditions(
        ConditionsViewModel model,
        [FromServices] ConditionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var buildConditionsResult = await useCase.GenerateConditionsAsync(model.ApplicationId, user, cancellationToken);

        if (buildConditionsResult.IsFailure)
        {
            this.AddErrorMessage("Could not generate conditions for application");
        }
        else
        {
            var message = buildConditionsResult.Value.Conditions.Any()
                ? "Conditions were generated successfully"
                : "No conditions were generated from the current confirmed felling and restocking details";
            this.AddConfirmationMessage(message);
        }

        return RedirectToAction("Conditions", new { id = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> SaveConditions(
        ConditionsViewModel model,
        [FromServices] ConditionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var saveConditionsResult = await useCase.SaveConditionsAsync(model.ApplicationId, user, model.Conditions, cancellationToken);

        if (saveConditionsResult.IsFailure)
        {
            this.AddErrorMessage("Could not save conditions for application");
            return RedirectToAction("Conditions", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Conditions were saved");
        return RedirectToAction("Conditions", new { id = model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> SendConditionsNotification(
        ConditionsViewModel model,
        [FromServices] ConditionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (model.ConfirmedFellingAndRestockingComplete == false)
        {
            this.AddErrorMessage("You must complete the confirm felling and restocking details, then generate and complete the conditions, before sending them to the applicant");
            return RedirectToAction("Conditions", new { id = model.ApplicationId });
        }

        var sendNotificationResult = await useCase.SendConditionsToApplicantAsync(model.ApplicationId, user, cancellationToken);

        if (sendNotificationResult.IsFailure)
        {
            this.AddErrorMessage("Could not send the conditions to the applicant");
        }
        else
        {
            this.AddConfirmationMessage("The conditions were sent to the applicant successfully");
        }

        return RedirectToAction("Conditions", new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> LarchCheck(
        Guid id,
        [FromServices] LarchCheckUseCase useCase,
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
        [FromServices] WoodlandOfficerReviewUseCase woodlandOfficerReviewUseCase,
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

        var result = await useCase.SaveLarchCheckAsync(
            model,
            user.UserAccountId!.Value,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to complete Larch check");
            return RedirectToAction(nameof(LarchCheck), new { id = model.ApplicationId });
        }

        var getWoodlandOfficeReview = await woodlandOfficerReviewUseCase.CompleteLarchCheckAsync(model.ApplicationId, user.UserAccountId!.Value, cancellationToken);

        if (getWoodlandOfficeReview.IsFailure)
        {
            this.AddErrorMessage("Unable to retrieve the required woodland officer data for completing the review");
            return RedirectToAction("Index", new { id = model.ApplicationId });
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

    private void ValidateModel<T>(T model, IValidator<T> validator, bool createErrors = true)
    {
        if (createErrors)
        {
            ModelState.Clear();
        }
        var validationErrors = validator.Validate(model).Errors;

        if (validationErrors.NotAny()) return;

        Type[] checkedTypes =
        [
            typeof(AddNewConfirmedFellingDetailsViewModel),
            typeof(AmendConfirmedFellingDetailsViewModel)
        ];

        foreach (var validationFailure in validationErrors)
        {
            if (checkedTypes.Contains(model?.GetType()))
            {
                var regex = SpeciesValueRegex();
                if (regex.IsMatch(validationFailure.PropertyName))
                {
                    ModelState.AddModelError(validationFailure.FormattedMessagePlaceholderValues["PropertyName"].ToString()!, validationFailure.ErrorMessage);
                    continue;
                }
            }

            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }

    [GeneratedRegex(@"Species\[\d+\]\.Value", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpeciesValueRegex();
}