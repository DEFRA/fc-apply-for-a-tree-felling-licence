using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using MassTransit;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
public class ApproverReviewController : Controller
{
    private readonly ILogger<ApproverReviewController> _logger;
    private readonly IApproverReviewUseCase _approverReviewUseCase;

    public ApproverReviewController(
        IApproverReviewUseCase approverReviewUseCase,
        ILogger<ApproverReviewController> logger)
    {
        ArgumentNullException.ThrowIfNull(approverReviewUseCase);
        _approverReviewUseCase = approverReviewUseCase;
        _logger = logger ?? new NullLogger<ApproverReviewController>();
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid id,
        CancellationToken cancellationToken)
    {
        var model = await _approverReviewUseCase.RetrieveApproverReviewAsync(
            id,
            new InternalUser(User),
            cancellationToken);

        if (model.HasNoValue)
        {
            _logger.LogError("Failed to retrieve approver review for application with ID {ApplicationId}", id);
            return RedirectToAction("Error", "Home");
        }
        
        if (model.Value.FellingLicenceApplicationSummary!.StatusHistories.NotAny(x => x.Status == FellingLicenceStatus.SentForApproval))
        {
            _logger.LogWarning("Approver review requested for application with ID {ApplicationId} that is not in the SentForApproval status", id);
            return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id });
        }

        model.Value.Breadcrumbs = new BreadcrumbsModel
        {
            CurrentPage = "Approver Review",
            Breadcrumbs = new List<BreadCrumb>() { 
                new("Open applications", "Home", "Index", null),  
                new(model.Value.FellingLicenceApplicationSummary!.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", id.ToString())
            }
        };
        
        // Restore decision from TempData if available
        if (TempData.TryGetValue("Decision", out var decisionObj) && bool.TryParse(decisionObj?.ToString(), out var decision))
        {
            _logger.LogDebug("Retrieving decision from TempData, value: {Decision}", decision);
            model.Value.Decision = decision;
            TempData.Keep("Decision");
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SaveApproverReview(
        ApproverReviewSummaryModel model,
        [FromServices] IApproverReviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving approver review for application with ID {ApplicationId}", model.Id);

        TempData["Decision"] = model.Decision?.ToString() ?? "";
        var user = new InternalUser(User);
        var result = await ValidateAndSaveApproverReview(model, useCase, user, cancellationToken);
        
        if (result.IsFailure)
        {
            _logger.LogWarning("Validation failed for approver review for application with ID {ApplicationId}. Errors: {Errors}", model.Id, result.Error);

            foreach (var modelError in result.Error)
            {
                ModelState.AddModelError(modelError.Key, modelError.Value);
            }

            var reloadModel = await useCase.RetrieveApproverReviewAsync(
                model.Id,
                new InternalUser(User),
                cancellationToken);

            if (reloadModel.HasNoValue)
            {
                _logger.LogError("Failed to reload approver review for application with ID {ApplicationId} after validation failure", model.Id);
                return RedirectToAction("Error", "Home");
            }

            model.ApproverReview.ApprovedLicenceDuration = model.ApproverReview.ApprovedLicenceDuration ?? reloadModel.Value.ApproverReview.ApprovedLicenceDuration;
            reloadModel.Value.ApproverReview = model.ApproverReview;
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
        return RedirectToAction("ApproveRefuseApplicationConfirmation", "ApproverReview", new { model.Id, model.ApproverReview.RequestedStatus});
    }

    [HttpPost]
    public IActionResult ReturnApplication(
        ApproverReviewSummaryModel model,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Returning application with ID {ApplicationId} to the return application page", model.Id);
        TempData["Decision"] = model.Decision?.ToString() ?? "";
        var user = new InternalUser(User);

        return RedirectToAction("Index", "ReturnApplication", new { model.Id, model.ApproverReview.RequestedStatus});
    }

    [HttpPost]
    public async Task<IActionResult> SaveGeneratePdfPreview(
        ApproverReviewSummaryModel model,
        [FromServices] IApproverReviewUseCase useCase,
        [FromServices] IGeneratePdfApplicationUseCase generatePdfApplicationUseCase,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving approver review and generating PDF preview for application with ID {ApplicationId}", model.Id);

        var user = new InternalUser(User);

        var resultPdfGenerated =
            await generatePdfApplicationUseCase.GeneratePdfApplicationAsync(user.UserAccountId!.Value, model.Id, cancellationToken);

        if (resultPdfGenerated.IsFailure)
        {
            _logger.LogError("Failed to generate PDF preview for application with ID {ApplicationId}. Error: {Error}", model.Id, resultPdfGenerated.Error);
            this.AddErrorMessage("Unable to generate the preview licence document for the application");
            return RedirectToAction("Index", new { model.Id });
        }
        return RedirectToAction("Index", "ApproverReview", new { model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> ApproveRefuseApplicationConfirmation(
        Guid id,
        FellingLicenceStatus requestedStatus,
        [FromServices] IFellingLicenceApplicationUseCase applicationUseCase,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Showing confirmation page to change application status for application with ID {ApplicationId} to {RequestedStatus}", id, requestedStatus);

        var user = new InternalUser(User);

        if (!user.CanApproveApplications)
        {
            _logger.LogWarning("User {UserId} attempted to approve/refuse application with ID {ApplicationId} without permission", user.UserAccountId, id);
            this.AddErrorMessage("Your account is not currently allowed to approve/refuse applications.  If you believe this to be in error, please contact your local administrator.");
            return RedirectToAction("Index", new { id });
        }

        var summary = await applicationUseCase.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            id,
            user,
            cancellationToken);

        if (summary.HasNoValue)
        {
            _logger.LogError("Failed to retrieve felling licence application summary for application with ID {ApplicationId}", id);
            return RedirectToAction("Error", "Home");
        }

        if (summary.Value.FellingLicenceApplicationSummary!.Status is not FellingLicenceStatus.SentForApproval
            || summary.Value.FellingLicenceApplicationSummary.AssigneeHistories.NotAny(x => x.UserAccount?.Id == summary.Value.ViewingUser?.UserAccountId && x.Role is AssignedUserRole.FieldManager))
        {
            _logger.LogWarning("Application with ID {ApplicationId} is not in the SentForApproval status or the user does not have the required role", id);
            return RedirectToAction("Index", new { id });
        }

        var confirmationModel = new ApproveRefuseReferApplicationModel
        {
            FellingLicenceApplicationSummary = summary.Value.FellingLicenceApplicationSummary,
            RequestedStatus = requestedStatus, 
            Breadcrumbs = new BreadcrumbsModel
            {
                CurrentPage = "Confirm Status Change",
                Breadcrumbs = new List<BreadCrumb>
                {
                    new(summary.Value.FellingLicenceApplicationSummary!.ApplicationReference,
                        "FellingLicenceApplication", "ApplicationSummary", id.ToString()),
                    new("Approver Review",
                        "ApproverReview", "Index", id.ToString())

                }
            }
        };

        return View(confirmationModel);
    }

    public async Task<IActionResult> RefuseApplication(
        Guid id,
        [FromServices] IApproveRefuseOrReferApplicationUseCase approvalRefusalUseCase,
        [FromServices] IGeneratePdfApplicationUseCase generatePdfApplicationUseCase,
        [FromServices] IRemoveSupportingDocumentUseCase removeSupportingDocumentUseCase,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Changing application status to Refused for application with ID {ApplicationId}", id);
        return await ChangeApplicationStatus(
            id,
            FellingLicenceStatus.Refused,
            approvalRefusalUseCase,
            generatePdfApplicationUseCase,
            cancellationToken);
    }

    public async Task<IActionResult> ApproveApplication(
        Guid id,
        [FromServices] IApproveRefuseOrReferApplicationUseCase approvalRefusalUseCase,
        [FromServices] IGeneratePdfApplicationUseCase generatePdfApplicationUseCase,
        [FromServices] IRemoveSupportingDocumentUseCase removeSupportingDocumentUseCase,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Changing application status to Approved for application with ID {ApplicationId}", id);
        return await ChangeApplicationStatus(
            id,
            FellingLicenceStatus.Approved,
            approvalRefusalUseCase,
            generatePdfApplicationUseCase,
            cancellationToken);
    }

    public async Task<IActionResult> ReferApplicationToLocalAuthority(
        Guid id,
        [FromServices] IApproveRefuseOrReferApplicationUseCase approvalRefusalUseCase,
        [FromServices] IGeneratePdfApplicationUseCase generatePdfApplicationUseCase,
        [FromServices] IRemoveSupportingDocumentUseCase removeSupportingDocumentUseCase,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Changing application status to ReferredToLocalAuthority for application with ID {ApplicationId}", id);
        return await ChangeApplicationStatus(
            id,
            FellingLicenceStatus.ReferredToLocalAuthority,
            approvalRefusalUseCase,
            generatePdfApplicationUseCase,
            cancellationToken);
    }

    async Task<UnitResult<Dictionary<string, string>>> ValidateAndSaveApproverReview(
        ApproverReviewSummaryModel model,
        IApproverReviewUseCase useCase,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>();

        if (model.ApproverReview!.RequestedStatus.HasNoValue())
        {
            _logger.LogWarning("Approver review requested status is null or empty for application with ID {ApplicationId}", model.Id);
            errors.Add(
                "ApproverReview.RequestedStatus",
                "A decision on the outcome of the application must be provided");
        }

        if (model.ApproverReview!.RequestedStatus == FellingLicenceStatus.Approved && model.ApproverReview!.ApprovedLicenceDuration != model.RecommendedLicenceDuration
            && string.IsNullOrEmpty(model.ApproverReview.DurationChangeReason))
        {
            _logger.LogWarning("Duration change reason is required for application with ID {ApplicationId} when duration differs from recommendation", model.Id);
            errors.Add(
                "ApproverReview.DurationChangeReason",
                model.IsWOReviewed
                    ? "A reason for changing the duration must be provided if it differs from the Woodland Officer recommendation"
                    : "A reason for changing the duration must be provided if it differs from the default");
        }

        if (model.ApproverReview!.PublicRegisterPublish != true
            && string.IsNullOrEmpty(model.ApproverReview.PublicRegisterExemptionReason))
        {
            _logger.LogWarning("Decision public register exemption reason is required for application with ID {ApplicationId} when not publishing to decision public register", model.Id);
            errors.Add(
                "ApproverReview.PublicRegisterExemptionReason",
                "The public register exemption reason must be provided");
        }

        if (!model.ApproverReview!.CheckedApplication)
        {
            _logger.LogWarning("Approver review checked application is false for application with ID {ApplicationId}", model.Id);
            errors.Add(
                "ApproverReview.CheckedApplication",
                "Please confirm you have reviewed the felling licence application");
        }

        if (!model.ApproverReview!.CheckedDocumentation)
        {
            _logger.LogWarning("Approver review checked documentation is false for application with ID {ApplicationId}", model.Id);
            errors.Add(
                "ApproverReview.CheckedDocumentation",
                "Please confirm you have reviewed the supporting documentation");
        }

        if (!model.ApproverReview!.CheckedCaseNotes)
        {
            _logger.LogWarning("Approver review checked case notes is false for application with ID {ApplicationId}", model.Id);
            errors.Add(
                "ApproverReview.CheckedCaseNotes",
                "Please confirm you have reviewed the case notes");
        }

        if (model.IsWOReviewed && !model.ApproverReview!.CheckedWOReview)
        {
            _logger.LogWarning("Approver review checked WO review is false for application with ID {ApplicationId}", model.Id);
            errors.Add(
                "ApproverReview.CheckedWOReview",
                "Please confirm you have reviewed the Woodland Officer review and recommendations");
        }

        if (model.ApproverReview!.RequestedStatus == FellingLicenceStatus.Approved && (model.ApproverReview?.ApprovedLicenceDuration is null or RecommendedLicenceDuration.None))
        {
            _logger.LogWarning("Approved licence duration is null or None for application with ID {ApplicationId}", model.Id);
            errors.Add(
                "ApproverReview.ApprovedLicenceDuration",
                "A recommended licence duration must be provided");
        }

        if (!model.ApproverReview!.InformedApplicant)
        {
            _logger.LogWarning("Approver review informed applicant is false for application with ID {ApplicationId}", model.Id);
            errors.Add(
                "ApproverReview.InformedApplicant",
                "Please confirm you have informed the applicant about the decision application");
        }

        if (errors.Count > 0)
        {
            return UnitResult.Failure(errors);
        }

        model.ApproverReview.ApplicationId = model.Id;
        var result = await useCase.SaveApproverReviewAsync(model.ApproverReview, user, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to save approver review for application with ID {ApplicationId}. Error: {Error}", model.Id, result.Error);
            this.AddErrorMessage($"Approval Review could not be updated, please try again.");
            return UnitResult.Failure(new Dictionary<string, string>());
        }
        return UnitResult.Success<Dictionary<string, string>>();
    }

    private async Task<IActionResult> ChangeApplicationStatus(
        Guid id,
        FellingLicenceStatus requestedStatus,
        IApproveRefuseOrReferApplicationUseCase approvalRefusalUseCase,
        IGeneratePdfApplicationUseCase generatePdfApplicationUseCase,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Changing application status for application with ID {ApplicationId} to {RequestedStatus}", id, requestedStatus);

        var internalUser = new InternalUser(User);

        if (!internalUser.CanApproveApplications)
        {
            _logger.LogWarning("User {UserId} attempted to change application status for application with ID {ApplicationId} without permission", internalUser.UserAccountId, id);
            this.AddErrorMessage("Your account is not currently allowed to approve/refuse applications.  If you believe this to be in error, please contact your local administrator.");
            return RedirectToAction("Index", new { id });
        }

        await using var transaction = await _approverReviewUseCase.BeginTransactionAsync(cancellationToken);

        var result = await approvalRefusalUseCase.ApproveOrRefuseOrReferApplicationAsync(
            internalUser,
            id,
            requestedStatus,
            cancellationToken);

        if (result.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            this.AddErrorMessage($"Application status could not be updated to {requestedStatus.GetDisplayNameByActorType(ActorType.InternalUser)}, please try again.");
            return RedirectToAction("Index", new { id });
        }

        if (requestedStatus is FellingLicenceStatus.Approved)
        {
            _logger.LogDebug("Generating licence document for application with ID {ApplicationId} as it is being approved", id);

            // update the approver id in order for the licence document to be generated with the correct approver details
            var updateResult = await approvalRefusalUseCase.UpdateApproverIdAsync(
                id,
                internalUser.UserAccountId!.Value,
                cancellationToken);

            if (updateResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update approver ID for application with ID {ApplicationId}. Error: {Error}", id, updateResult.Error);
                this.AddErrorMessage("Unable to update the approver id for the application");
                return RedirectToAction("Index", new { id });
            }

            // generate a final licence document for the approved application
            // this cannot be asynchronous, as the approval should not complete if the document generation fails
            var resultPdfGenerated =
                await generatePdfApplicationUseCase.GeneratePdfApplicationAsync(internalUser.UserAccountId.Value, id, cancellationToken);
            if (resultPdfGenerated.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to generate licence document for application with ID {ApplicationId}. Error: {Error}", id, resultPdfGenerated.Error);
                this.AddErrorMessage("Unable to generate the licence document for the application");
                return RedirectToAction("Index", new { id });
            }
        }

        await transaction.CommitAsync(cancellationToken);

        if (result is { IsSuccess: true, SubProcessFailures.Count: > 0 })
        {
            _logger.LogWarning("Application with ID {ApplicationId} status changed to {RequestedStatus} with warnings: {Warnings}", id, requestedStatus, result.SubProcessFailures);

            var warnings = new StringBuilder();

            if (result.SubProcessFailures.Contains(FinaliseFellingLicenceApplicationProcessOutcomes
                    .CouldNotPublishToDecisionPublicRegister))
            {
                warnings.AppendLine("Unable to publish this application to the Decision Public Register. ");
            }

            if (result.SubProcessFailures.Contains(FinaliseFellingLicenceApplicationProcessOutcomes
                    .CouldNotSendNotificationToApplicant))
            {
                warnings.AppendLine("Unable to send the Applicant the notification of this decision. ");
            }

            if (result.SubProcessFailures.Contains(FinaliseFellingLicenceApplicationProcessOutcomes
                    .CouldNotStoreDecisionDetailsLocally))
            {
                warnings.AppendLine("Successfully sent application to the decision public register, but could not save its date for removal on the local system.");
            }

            if (warnings.Length > 0)
            {
                this.AddUserGuide("One or more issues occured", warnings.ToString());
            }
        }

        this.AddConfirmationMessage($"Application status successfully set to {requestedStatus.GetDisplayNameByActorType(ActorType.InternalUser)}.");
        return RedirectToAction("Index", new { id });
    }
}

