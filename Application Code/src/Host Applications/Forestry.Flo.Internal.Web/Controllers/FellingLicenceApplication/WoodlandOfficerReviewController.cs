using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using AssignedUserRole = Forestry.Flo.Services.FellingLicenceApplications.Entities.AssignedUserRole;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
public partial class WoodlandOfficerReviewController(
    IValidator<CompartmentConfirmedFellingRestockingDetailsModel> fellingAndRestockingDetailsModelValidator,
    IEnvironmentalImpactAssessmentAdminOfficerUseCase eiaUseCase,
    IWoodlandOfficerReviewUseCase woReviewUseCase)
    : Controller
{
    private readonly IValidator<CompartmentConfirmedFellingRestockingDetailsModel> _fellingAndRestockingDetailsModelValidator = fellingAndRestockingDetailsModelValidator;

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid id,
        [FromServices]IWoodlandOfficerReviewUseCase useCase,
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
        [FromServices] IWoodlandOfficerReviewUseCase useCase,
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

        if (string.IsNullOrWhiteSpace(model.RecommendationForDecisionPublicRegisterReason))
        {
            this.AddErrorMessage("A reason for the recommendation for whether to publish to the decision public register must be provided");
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
            model.ApplicationId, model.RecommendedLicenceDuration, model.RecommendationForDecisionPublicRegister, model.RecommendationForDecisionPublicRegisterReason, internalLinkToApplication, user, cancellationToken);

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
        [FromServices]IWoodlandOfficerReviewUseCase useCase,
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
        [FromServices] IPublicRegisterUseCase useCase,
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
        [FromServices] IPublicRegisterUseCase useCase,
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
        [FromServices] IPublicRegisterUseCase useCase,
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

    [HttpGet]
    public async Task<IActionResult> ReviewComment(
        Guid id,
        Guid commentId,
        [FromServices] IPublicRegisterUseCase useCase,
        bool? withExemption,
        CancellationToken cancellationToken)
    {
        var commentResult = await useCase.GetPublicRegisterCommentAsync(id, commentId, cancellationToken);
        if (commentResult.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(commentResult.Value);
    }

    [HttpPost]
    public async Task<IActionResult> ReviewComment(
        Guid id,
        Guid commentId,
        [FromForm] ReviewCommentModel model,
        [FromServices] IPublicRegisterUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (model.Comment is not null)
        {
            var user = new InternalUser(User);
            model.Comment.LastUpdatedById = user.UserAccountId!.Value;
            var updateResult = await useCase.UpdatePublicRegisterDetailsAsync(commentId, model.Comment, cancellationToken);
            if (!updateResult.IsFailure)
            {
                this.AddConfirmationMessage("Comment updated successfully.");
                return RedirectToAction("PublicRegister", new { id });
            }
        }
        this.AddErrorMessage("Failed to update comment.");
        return RedirectToAction("ReviewComment", new { id, commentId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromPublicRegister(
        PublicRegisterViewModel model,
        [FromServices] IPublicRegisterUseCase useCase,
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
        [FromServices] ISiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var hostingPage = Url.Action(nameof(SiteVisit), new { id = id });

        var model = await useCase.GetSiteVisitDetailsAsync(id, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        if (model.Value.SiteVisitArrangementsMade.HasValue)  // this page is already completed, so redirect to evidence upload/review
        {
            return model.Value.SiteVisitComplete
                ? RedirectToAction(nameof(ReviewSiteVisitEvidence), new { id })
                : RedirectToAction(nameof(AddSiteVisitEvidence), new { id });
        }

        return View(model.Value);
    }

    [HttpGet]
    public async Task<IActionResult> SiteVisitSummary(
        Guid id,
        [FromServices] ISiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var hostingPage = Url.Action(nameof(SiteVisitSummary), new { id = id });

        var model = await useCase.GetSiteVisitSummaryAsync(id, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SiteVisit(
        SiteVisitViewModel model,
        [FromServices] ISiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var hostingPage = Url.Action(nameof(SiteVisit), new { id = model.ApplicationId });

        // site visit not needed scenario
        if (model.SiteVisitNeeded is false)
        {
            if (string.IsNullOrWhiteSpace(model.SiteVisitNotNeededReason.CaseNote))
            {
                var reloadModel = await useCase.GetSiteVisitDetailsAsync(model.ApplicationId, hostingPage, cancellationToken);
                if (reloadModel.IsFailure)
                {
                    return RedirectToAction("Error", "Home");
                }

                reloadModel.Value.SiteVisitNeeded = model.SiteVisitNeeded;

                this.AddErrorMessage("A reason must be provided when a site visit is not required", nameof(model.SiteVisitNotNeededReason));
                return View(reloadModel.Value);
            }

            var result = await useCase.SiteVisitIsNotNeededAsync(
                model.ApplicationId,
                user,
                model.SiteVisitNotNeededReason,
                cancellationToken);

            if (result.IsFailure)
            {
                this.AddErrorMessage("Something went wrong saving the site visit details, please try again");
                return RedirectToAction(nameof(SiteVisit), new { id = model.ApplicationId });
            }

            this.AddConfirmationMessage("Site visit not needed reason saved");
            return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
        }

        // site visit needed scenario
        if (model.SiteVisitNeeded is true)
        {
            if (model.SiteVisitArrangementsMade.HasNoValue())
            {
                var reloadModel = await useCase.GetSiteVisitDetailsAsync(model.ApplicationId, hostingPage, cancellationToken);
                if (reloadModel.IsFailure)
                {
                    return RedirectToAction("Error", "Home");
                }

                reloadModel.Value.SiteVisitNeeded = model.SiteVisitNeeded;
                reloadModel.Value.SiteVisitArrangementNotes = model.SiteVisitArrangementNotes;

                this.AddErrorMessage("Select if site visit arrangements have been made", nameof(model.SiteVisitNotNeededReason));
                return View(reloadModel.Value);
            }

            var result = await useCase.SetSiteVisitArrangementsAsync(
                model.ApplicationId,
                user,
                model.SiteVisitArrangementsMade,
                model.SiteVisitArrangementNotes,
                cancellationToken);

            if (result.IsFailure)
            {
                this.AddErrorMessage("Something went wrong saving the site visit details, please try again");
                return RedirectToAction(nameof(SiteVisit), new { id = model.ApplicationId });
            }

            this.AddConfirmationMessage("Site visit arrangements saved");
            return RedirectToAction(nameof(AddSiteVisitEvidence), new { id = model.ApplicationId });
        }

        return RedirectToAction(nameof(SiteVisit), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> AddSiteVisitEvidence(
        Guid id,
        [FromServices] ISiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var hostingPage = Url.Action(nameof(AddSiteVisitEvidence), new { id = id });

        var model = await useCase.GetSiteVisitEvidenceModelAsync(id, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        if (model.Value.SiteVisitComplete)  // this page is already completed, so redirect to evidence review
        {
            return RedirectToAction(nameof(ReviewSiteVisitEvidence), new { id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddSiteVisitEvidence(
        AddSiteVisitEvidenceModel model,
        FormFileCollection siteVisitAttachmentFiles,
        [FromServices] ISiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var result = await useCase.AddSiteVisitEvidenceAsync(
            model,
            siteVisitAttachmentFiles,
            user,
            cancellationToken);
        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong saving the site visit evidence, please try again");
            return RedirectToAction(nameof(AddSiteVisitEvidence), new { id = model.ApplicationId });
        }
        this.AddConfirmationMessage("Site visit evidence saved");
        
        return model.SiteVisitComplete 
            ? RedirectToAction(nameof(ReviewSiteVisitEvidence), new { id = model.ApplicationId })
            : RedirectToAction(nameof(AddSiteVisitEvidence), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> ReviewSiteVisitEvidence(
        Guid id,
        [FromServices] ISiteVisitUseCase useCase,
        CancellationToken cancellationToken)
    {
        var hostingPage = Url.Action(nameof(ReviewSiteVisitEvidence), new { id = id });

        var model = await useCase.GetSiteVisitEvidenceModelAsync(id, hostingPage, cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Pw14Checks(
        Guid id,
        [FromServices] IPw14UseCase useCase,
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
        [FromServices] IPw14UseCase useCase,
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
        [FromServices] IConditionsUseCase useCase,
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
        [FromServices] IConditionsUseCase useCase,
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
        [FromServices] IConditionsUseCase useCase,
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
        [FromServices] IConditionsUseCase useCase,
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
        [FromServices] IConditionsUseCase useCase,
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
        [FromServices] ILarchCheckUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var model = await useCase.GetLarchCheckModelAsync(id, user, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve larch check status");
            return RedirectToAction("Index", new { id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> LarchCheck(
        Guid id,
        LarchCheckModel model,
        [FromServices] ILarchCheckUseCase useCase,
        [FromServices] IWoodlandOfficerReviewUseCase woodlandOfficerReviewUseCase,
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

        if (!string.IsNullOrWhiteSpace(model.FormLevelCaseNote.CaseNote))
        {
            var caseNoteRecord = new AddCaseNoteRecord(
                FellingLicenceApplicationId: model.ApplicationId,
                Type: CaseNoteType.LarchCheckComment,
                Text: model.FormLevelCaseNote.CaseNote,
                VisibleToApplicant: model.FormLevelCaseNote.VisibleToApplicant,
                VisibleToConsultee: model.FormLevelCaseNote.VisibleToConsultee
            );

            var caseNoteResult = await amendCaseNotes.AddCaseNoteAsync(caseNoteRecord, user.UserAccountId.Value, cancellationToken);

            if (caseNoteResult.IsFailure)
            {
                this.AddErrorMessage(caseNoteResult.Error);
                return RedirectToAction(nameof(LarchFlyover), new { id = model.ApplicationId });
            }
        }

        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> LarchFlyover(
        Guid id,
        [FromServices] ILarchCheckUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);
        var model = await useCase.GetLarchFlyoverModelAsync(id, user, cancellationToken);

        if (model.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve larch flyover status");
            return RedirectToAction("Index", new { id });
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> LarchFlyover(
        Guid id,
        LarchFlyoverModel model,
        [FromServices] ILarchCheckUseCase useCase,
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

        if (!string.IsNullOrWhiteSpace(model.FormLevelCaseNote.CaseNote))
        {
            var caseNoteRecord = new AddCaseNoteRecord(
                FellingLicenceApplicationId: model.ApplicationId,
                Type: CaseNoteType.LarchCheckComment,
                Text: model.FormLevelCaseNote.CaseNote,
                VisibleToApplicant: model.FormLevelCaseNote.VisibleToApplicant,
                VisibleToConsultee: model.FormLevelCaseNote.VisibleToConsultee
            );

            var caseNoteResult = await amendCaseNotes.AddCaseNoteAsync(caseNoteRecord, user.UserAccountId.Value, cancellationToken);

            if (caseNoteResult.IsFailure)
            {
                this.AddErrorMessage(caseNoteResult.Error);
                return RedirectToAction(nameof(LarchFlyover), new { id = model.ApplicationId });
            }
        }

        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> EiaScreening(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, viewModel) = await LoadEiaScreeningViewModel(id, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        SetEiaBreadcrumbs(viewModel);

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> EiaScreening(
        EiaScreeningViewModel model,
        [FromServices] IWoodlandOfficerReviewUseCase useCase,
        [FromServices] IValidator<EiaScreeningViewModel> validator,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        ValidateModel(model, validator);

        if (ModelState.IsValid is false)
        {
            var (_, isFailure, viewModel) = await LoadEiaScreeningViewModel(model.ApplicationId, cancellationToken);

            if (isFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.RequestHistoryItems = viewModel.RequestHistoryItems;
            model.EiaDocumentModels = viewModel.EiaDocumentModels;
            model.FellingLicenceApplicationSummary = viewModel.FellingLicenceApplicationSummary;

            SetEiaBreadcrumbs(model);

            return View(model);
        }

        var result = await useCase.CompleteEiaScreeningAsync(
            model.ApplicationId,
            user,
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to complete EIA screening");
            return RedirectToAction(nameof(EiaScreening), new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Successfully completed the EIA screening");
        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });
    }

    [HttpGet]
    public async Task<IActionResult> ViewDesignations(
        Guid id,
        [FromServices] IDesignationsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, viewModel) = await useCase.GetApplicationDesignationsAsync(id, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ViewDesignations(
        DesignationsViewModel model,
        [FromServices] IDesignationsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, viewModel) = await useCase.GetApplicationDesignationsAsync(model.ApplicationId, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (viewModel.CompartmentDesignations.CompartmentDesignations.Any(x => !x.HasCompletedDesignations))
        {
            this.AddErrorMessage("Please review all of the compartments before submitting the task");
            return View(viewModel);
        }

        var user = new InternalUser(User);
        var result = await useCase.UpdateCompartmentDesignationsCompletionAsync(model.ApplicationId, user, true, cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to complete the designations task");
            return View(viewModel);
        }

        this.AddConfirmationMessage("Successfully completed the designations task");
        return RedirectToAction(nameof(Index), new { id = model.ApplicationId });

    }

    [HttpGet]
    public async Task<IActionResult> UpdateDesignations(
        Guid id,
        Guid compartmentId,
        [FromServices] IDesignationsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, viewModel) = await useCase.GetUpdateDesignationsModelAsync(id, compartmentId, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDesignations(
        UpdateDesignationsViewModel model,
        [FromServices] IDesignationsUseCase useCase,
        [FromServices] IValidator<UpdateDesignationsViewModel> validator,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        ValidateModel(model, validator);
        if (ModelState.IsValid is false)
        {
            var (_, isFailure, viewModel) = await useCase.GetUpdateDesignationsModelAsync(model.ApplicationId, model.CompartmentDesignations.SubmittedFlaCompartmentId, cancellationToken);
            if (isFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            viewModel.CompartmentDesignations = model.CompartmentDesignations;
            return View(viewModel);
        }

        var result = await useCase.UpdateCompartmentDesignationsAsync(model.ApplicationId, model.CompartmentDesignations, user, cancellationToken);
        if (result.IsFailure)
        {
            this.AddErrorMessage("Unable to update designations");
            var (_, isFailure, viewModel) = await useCase.GetUpdateDesignationsModelAsync(model.ApplicationId, model.CompartmentDesignations.SubmittedFlaCompartmentId, cancellationToken);
            if (isFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            viewModel.CompartmentDesignations = model.CompartmentDesignations;
            return View(viewModel);
        }

        this.AddConfirmationMessage($"Successfully updated designations for {model.CompartmentDesignations.CompartmentName}");
        if (model.NextCompartmentId.HasValue)
        {
            return RedirectToAction(nameof(UpdateDesignations), new { id = model.ApplicationId, compartmentId = model.NextCompartmentId });
        }
        
        return RedirectToAction(nameof(ViewDesignations), new { id = model.ApplicationId });
    }

    public async Task<IActionResult> GenerateLicencePreview(
        [FromQuery] Guid applicationId,
        [FromServices] IGeneratePdfApplicationUseCase generatePdfApplicationUseCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var resultPdfGenerated =
            await generatePdfApplicationUseCase.GeneratePdfApplicationAsync(user.UserAccountId!.Value, applicationId, cancellationToken);

        if (resultPdfGenerated.IsFailure)
        {
            this.AddErrorMessage("Unable to generate the preview licence document for the application");
        }

        return RedirectToAction(nameof(Index), new { id = applicationId });
    }

    private async Task<Result<EiaScreeningViewModel>> LoadEiaScreeningViewModel(Guid id, CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var woReview = await woReviewUseCase.WoodlandOfficerReviewAsync(
            id,
            user,
            string.Empty,
            cancellationToken);

        if (woReview.IsFailure)
        {
            return Result.Failure<EiaScreeningViewModel>("Unable to retrieve WO review");
        }

        var environmentalImpactAssessment = await eiaUseCase.GetEnvironmentalImpactAssessmentAsync(id, cancellationToken);

        if (environmentalImpactAssessment.IsFailure)
        {
            return Result.Failure<EiaScreeningViewModel>("Unable to retrieve EIA details");
        }

        var authorRequests =
            await eiaUseCase.RetrieveUserAccountsByIdsAsync(
                environmentalImpactAssessment.Value.EiaRequests
                    .Where(y => y.RequestingUserId is not null)
                    .Select(x => x.RequestingUserId!.Value).ToList(),
                cancellationToken);

        if (authorRequests.IsFailure ||
            environmentalImpactAssessment.Value.EiaRequests
                .Any(x => 
                    x.RequestingUserId is not null && 
                    authorRequests.Value.Select(y => y.UserAccountId).Contains(x.RequestingUserId!.Value) is false))
        {
            return Result.Failure<EiaScreeningViewModel>("Unable to retrieve EIA request authors");
        }

        var nameDictionary = authorRequests.Value.ToDictionary(x => x.UserAccountId, x => x.FullName);

        return new EiaScreeningViewModel
        {
            ScreeningCompleted = woReview.Value.WoodlandOfficerReviewTaskListStates.EiaScreeningStatus is InternalReviewStepStatus.Completed,
            RequestHistoryItems = environmentalImpactAssessment.Value.EiaRequests.Select(x => new RequestHistoryItem(
                nameDictionary[x.RequestingUserId!.Value],
                x.NotificationTime,
                x.RequestType)),
            EiaDocumentModels = environmentalImpactAssessment.Value.EiaDocuments,
            ApplicationId = id,
            FellingLicenceApplicationSummary = woReview.Value.FellingLicenceApplicationSummary
        };
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

    private void SetEiaBreadcrumbs(FellingLicenceApplicationPageViewModel model)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Home", "Home", "Index", null),
            new BreadCrumb(model.FellingLicenceApplicationSummary.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary.Id.ToString()),
            new BreadCrumb("Woodland officer review", "WoodlandOfficerReview", "Index", model.FellingLicenceApplicationSummary.Id.ToString())
        };
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = "EIA screening"
        };
    }

    [GeneratedRegex(@"Species\[\d+\]\.Value", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpeciesValueRegex();
}