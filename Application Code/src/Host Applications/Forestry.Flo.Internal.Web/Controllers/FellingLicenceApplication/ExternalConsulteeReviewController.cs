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

        var result = await externalConsulteeReviewUseCase.AddConsulteeCommentAsync(
            commentModel, consulteeAttachmentFiles, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return RedirectToAction("Index", new
        {
            applicationId = commentModel.ApplicationId, 
            accessCode = commentModel.AccessCode,
            emailAddress = commentModel.AuthorContactEmail
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