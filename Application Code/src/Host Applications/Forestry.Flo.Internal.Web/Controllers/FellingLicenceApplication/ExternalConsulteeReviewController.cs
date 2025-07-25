using Ardalis.GuardClauses;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Microsoft.AspNetCore.Mvc;
using AddConsulteeCommentModel = Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview.AddConsulteeCommentModel;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public class ExternalConsulteeReviewController : Controller
{
    private readonly ExternalConsulteeReviewUseCase _externalConsulteeReviewUseCase;

    public ExternalConsulteeReviewController(
        ExternalConsulteeReviewUseCase externalConsulteeReviewUseCase)
    {
        _externalConsulteeReviewUseCase = Guard.Against.Null(externalConsulteeReviewUseCase);
    }

    // GET: ExternalConsulteeReview
    public async Task<IActionResult> Index(
        [FromQuery] Guid applicationId, 
        [FromQuery] Guid accessCode,
        [FromQuery] string emailAddress,
        [FromServices] ExternalConsulteeReviewUseCase externalConsulteeReviewUseCase,
        CancellationToken cancellationToken)
    {
        var validationResult = await externalConsulteeReviewUseCase.ValidateAccessCodeAsync(applicationId, accessCode, emailAddress, cancellationToken);
        if (validationResult.IsFailure)
        {
            return RedirectToAction("LinkExpired");
        }

        var model = await externalConsulteeReviewUseCase.GetApplicationSummaryForConsulteeReviewAsync(applicationId, validationResult.Value, cancellationToken);
        
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
        [FromServices] ExternalConsulteeReviewUseCase externalConsulteeReviewUseCase,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid is false)
        {
            var validationResult = await externalConsulteeReviewUseCase.ValidateAccessCodeAsync(commentModel.ApplicationId, commentModel.ApplicationId, commentModel.AuthorContactEmail, cancellationToken);
            if (validationResult.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            var reloadModel = await externalConsulteeReviewUseCase.GetApplicationSummaryForConsulteeReviewAsync(commentModel.ApplicationId, validationResult.Value, cancellationToken);
            if (reloadModel.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            reloadModel.Value.AddConsulteeComment = commentModel;
            return View(reloadModel.Value);
        }

        var result = await externalConsulteeReviewUseCase.AddConsulteeCommentAsync(
            commentModel, cancellationToken);

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
}