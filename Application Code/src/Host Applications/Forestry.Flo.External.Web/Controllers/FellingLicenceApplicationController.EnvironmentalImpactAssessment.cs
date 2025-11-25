using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Controllers;

public partial class FellingLicenceApplicationController
{
    [EditingAllowed]
    public async Task<IActionResult> EnvironmentalImpactAssessment(
        Guid applicationId,
        [FromServices] EnvironmentalImpactAssessmentUseCase useCase,
        CancellationToken cancellationToken,
        ModelStateDictionary? modelState = null)
    {
        var user = new ExternalApplicant(User);

        var fellingLicenceApplicationModelResult =
            await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (fellingLicenceApplicationModelResult.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var model = fellingLicenceApplicationModelResult.Value.EnvironmentalImpactAssessment;

        if (modelState is not null)
        {
            ModelState.Merge(modelState);
        }

        model.ApplicationSummary = fellingLicenceApplicationModelResult.Value.ApplicationSummary;
        SetTaskBreadcrumbs(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EnvironmentalImpactAssessment(
        EnvironmentalImpactAssessmentViewModel model,
        [FromServices] EnvironmentalImpactAssessmentUseCase useCase,
        [FromServices] IValidator<EnvironmentalImpactAssessmentViewModel> validator,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (model.HasApplicationBeenCompleted is false)
        {
            // if the user has not completed an EIA then they don't need to answer if it's been sent
            // can't use validation for this, as with the controls being radio buttons there's no way for the user to unselect an answer
            model.HasApplicationBeenSent = null;
        }

        ValidateModel(model, validator);

        if (!ModelState.IsValid)
        {

            var fellingLicenceApplicationModelResult =
                await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, model.ApplicationId, cancellationToken);

            if (fellingLicenceApplicationModelResult.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.ApplicationSummary = fellingLicenceApplicationModelResult.Value.ApplicationSummary;
            model.EiaDocuments = fellingLicenceApplicationModelResult.Value.EnvironmentalImpactAssessment.EiaDocuments;
            model.EiaApplicationExternalUri = fellingLicenceApplicationModelResult.Value.EnvironmentalImpactAssessment.EiaApplicationExternalUri;

            SetTaskBreadcrumbs(model);

            return View(model);
        }

        var confirmCompletionResult = await useCase.MarkEiaAsCompletedAsync(
            model.ApplicationId,
            new EnvironmentalImpactAssessmentRecord
                {
                    HasApplicationBeenCompleted = model.HasApplicationBeenCompleted,
                    HasApplicationBeenSent = model.HasApplicationBeenSent
                },
            user, 
            cancellationToken);

        return confirmCompletionResult.IsFailure 
            ? RedirectToAction(nameof(HomeController.Error), "Home") 
            : RedirectToAction(nameof(ConstraintsCheck), new { model.ApplicationId });
    }

    [HttpPost]
    public async Task<IActionResult> AttachEiaDocument(
        Guid applicationId,
        FormFileCollection eiaFiles,
        [FromServices] EnvironmentalImpactAssessmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (eiaFiles.NotAny())
        {
            this.AddErrorMessage("Select at least one file to upload");
            return RedirectToEiaPage();
        }

        var user = new ExternalApplicant(User);

        var saveDocumentsResult = await useCase.AddEiaDocumentsToApplicationAsync(
            user,
            applicationId,
            eiaFiles,
            ModelState,
            cancellationToken);

        if (saveDocumentsResult.IsSuccess && ModelState.IsValid)
        {
            this.AddConfirmationMessage("Successfully uploaded EIA documents");
            return RedirectToEiaPage();
        }

        this.AddErrorMessage("One or more selected documents could not be uploaded, try again");

        var fellingLicenceApplicationModelResult =
            await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (fellingLicenceApplicationModelResult.HasNoValue)
        {
            return RedirectToEiaPage();
        }

        return RedirectToAction(nameof(EnvironmentalImpactAssessment), new { applicationId, modelState = ModelState });

        IActionResult RedirectToEiaPage()
        {
            return RedirectToAction(nameof(EnvironmentalImpactAssessment), new { applicationId });
        }
    }

    public async Task<IActionResult> RemoveEiaDocument(
        Guid applicationId,
        Guid documentIdentifier,
        [FromServices] RemoveSupportingDocumentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var removeResult = await useCase.RemoveSupportingDocumentAsync(user, applicationId, documentIdentifier, cancellationToken);

        if (removeResult.IsFailure)
        {
            logger.LogError("Failed to remove eia document with error {Error}", removeResult.Error);
            this.AddErrorMessage("Could not remove EIA document at this time, try again");
        }

        this.AddConfirmationMessage("EIA document successfully removed");
        return RedirectToAction(nameof(EnvironmentalImpactAssessment), new { applicationId });
    }
}