using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.External.Web.Infrastructure;

namespace Forestry.Flo.External.Web.Controllers;

public partial class FellingLicenceApplicationController
{
    public async Task<IActionResult> ReviewAmendments(
        Guid applicationId,
        [FromServices] ReviewFellingAndRestockingAmendmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var model = await useCase.GetReviewAmendmentsViewModelAsync(
            applicationId,
            new ExternalApplicant(User), 
            cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> ReviewAmendments(
        ReviewAmendmentsViewModel model,
        [FromServices] ReviewFellingAndRestockingAmendmentsUseCase useCase,
        [FromServices] IValidator<ReviewAmendmentsViewModel> validator,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        ValidateModel(model, validator);

        if (!ModelState.IsValid)
        {
            var newModel = await useCase.GetReviewAmendmentsViewModelAsync(
                model.ApplicationId,
                new ExternalApplicant(User),
                cancellationToken);

            if (newModel.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.AmendedFellingAndRestockingDetails = newModel.Value.AmendedFellingAndRestockingDetails;

            return View(model);
        }

        var result = await useCase.CompleteAmendmentReviewAsync(
            new FellingAndRestockingAmendmentReviewUpdateRecord
            {
                FellingLicenceApplicationId = model.ApplicationId,
                ApplicantAgreed = model.ApplicantAgreed!.Value,
                ApplicantDisagreementReason = model.ApplicantDisagreementReason
            },
            user.UserAccountId!.Value,
            cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        this.AddConfirmationMessage("Amendment review saved.");
        return RedirectToAction(nameof(ApplicationTaskList), new { applicationId = model.ApplicationId });
    }
}