using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using AddConsulteeCommentModel = Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview.AddConsulteeCommentModel;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public class ExternalConsulteeReviewController : Controller
{
    // GET: ExternalConsulteeReview
    public async Task<IActionResult> Index(
        [FromQuery] Guid applicationId, 
        [FromQuery] Guid accessCode,
        [FromQuery] string emailAddress,
        [FromQuery] string? consulteeOrganisation,
        [FromQuery] string? consulteeJobRole,
        [FromServices] IExternalConsulteeReviewUseCase externalConsulteeReviewUseCase,
        CancellationToken cancellationToken)
    {
        var validationResult = await externalConsulteeReviewUseCase.ValidateAccessCodeAsync(applicationId, accessCode, emailAddress, cancellationToken);
        if (validationResult.IsFailure)
        {
            return RedirectToAction("LinkExpired");
        }

        var model = await externalConsulteeReviewUseCase.GetApplicationSummaryForConsulteeReviewAsync(applicationId, validationResult.Value, accessCode, cancellationToken);
        
        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        model.Value.AddConsulteeComment.AuthorOrganisation = consulteeOrganisation ?? string.Empty;
        model.Value.AddConsulteeComment.AuthorJobRole = consulteeJobRole ?? string.Empty;

        return View(model.Value);
    }

    // POST: ExternalConsulteeReview
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        AddConsulteeCommentModel commentModel, 
        FormFileCollection consulteeAttachmentFiles,
        [FromServices] IExternalConsulteeReviewUseCase externalConsulteeReviewUseCase,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid is false)
        {
            var validationResult = await externalConsulteeReviewUseCase.ValidateAccessCodeAsync(commentModel.ApplicationId, commentModel.AccessCode, commentModel.AuthorContactEmail, cancellationToken);
            if (validationResult.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            var reloadModel = await externalConsulteeReviewUseCase.GetApplicationSummaryForConsulteeReviewAsync(commentModel.ApplicationId, validationResult.Value, commentModel.AccessCode, cancellationToken);
            if (reloadModel.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            reloadModel.Value.AddConsulteeComment = commentModel;
            return View(reloadModel.Value);
        }

        var viewApplicationUrl = Url.Action(nameof(FellingLicenceApplicationController.ApplicationSummary), "FellingLicenceApplication", new { id = commentModel.ApplicationId }, this.Request.Scheme);
        var result = await externalConsulteeReviewUseCase.AddConsulteeCommentAsync(
            commentModel, consulteeAttachmentFiles, viewApplicationUrl, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        this.AddConfirmationMessage("Your comment has been added to this application, you can add further comments or close this tab");

        return RedirectToAction("Index", new
        {
            applicationId = commentModel.ApplicationId, 
            accessCode = commentModel.AccessCode,
            emailAddress = commentModel.AuthorContactEmail,
            consulteeOrganisation = commentModel.AuthorOrganisation,
            consulteeJobRole = commentModel.AuthorJobRole
        });
    }

    public IActionResult LinkExpired()
    {
        return View();
    }

    public async Task<IActionResult> DownloadSupportingDocument(
        [FromServices] IExternalConsulteeReviewUseCase useCase,
        [FromQuery] Guid applicationId,
        [FromQuery] Guid accessCode,
        [FromQuery] Guid documentId,
        [FromQuery] string emailAddress,
        CancellationToken cancellationToken)
    {
        
        var result = await useCase.GetSupportingDocumentAsync(
            applicationId, accessCode, documentId, emailAddress, cancellationToken);

        if (result.IsSuccess)
        {
            return result.Value;
        }

        this.AddErrorMessage("Could not download document content, please try again");

        return RedirectToAction("Index", new { applicationId, accessCode, emailAddress });
    }
}