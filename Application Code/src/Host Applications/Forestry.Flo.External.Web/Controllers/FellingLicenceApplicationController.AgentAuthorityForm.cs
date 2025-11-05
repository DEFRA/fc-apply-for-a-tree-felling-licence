using FluentValidation;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.AgentAuthority;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

public partial class FellingLicenceApplicationController
{
    [HttpGet]
    public async Task<IActionResult> AgentAuthorityForm(
    Guid agentAuthorityId,
    Guid applicationId,
    bool? returnToApplicationSummary,
    [FromServices] GetAgentAuthorityFormDocumentsUseCase useCase,
    CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.GetAgentAuthorityFormDocumentsAsync(user, agentAuthorityId, cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong getting the authority form information, please try again");
            return RedirectToAction(nameof(Index), "AgentAuthorityForm");
        }
        var fellingLicenceApplicationModelResult =
            await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (fellingLicenceApplicationModelResult.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }
        
        ViewData["AafStepStatus"] = fellingLicenceApplicationModelResult.Value.AgentAuthorityForm.StepComplete;

        if (result.Value.DoesNotHaveAnyAuthorityForms)
        {
            ModelState.AddModelError(nameof(AgentAuthorityFormDocumentModel.DoesNotHaveAnyAuthorityForms), "You must have a current agent authority form to submit your application");
        }

        return View(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AgentAuthorityForm(
        [FromForm] Guid applicationId,
        [FromForm] bool aafStepStatus,
        [FromForm] bool? returnToApplicationSummary,
        AgentAuthorityFormDocumentModel model,
        [FromServices] GetAgentAuthorityFormDocumentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var reloadModel = await useCase.GetAgentAuthorityFormDocumentsAsync(user, model.AgentAuthorityId, cancellationToken);

        if (reloadModel.IsFailure)
        {
            this.AddErrorMessage("Something went wrong getting the authority form information, please try again");
            return RedirectToAction(nameof(Index), "AgentAuthorityForm");
        }

        if (reloadModel.Value.DoesNotHaveAnyAuthorityForms && aafStepStatus)
        {
            ModelState.AddModelError(nameof(AgentAuthorityFormDocumentModel.DoesNotHaveAnyAuthorityForms), "You must have a current agent authority form to submit your application");

            ViewData["AafStepStatus"] = false;

            return View(reloadModel.Value);
        }

        // Attempt to set the AAF step status for the application.
        var completeResult = await useCase.CompleteAafStepAsync(applicationId, user, aafStepStatus, cancellationToken);
        if (completeResult.IsFailure)
        {
            this.AddErrorMessage("Something went wrong updating your application, please try again");

            var reloadResult = await useCase.GetAgentAuthorityFormDocumentsAsync(user, model.AgentAuthorityId, cancellationToken);
            if (reloadResult.IsFailure)
            {
                return RedirectToAction(nameof(Index), "AgentAuthorityForm");
            }

            return View(reloadResult.Value);
        }

        if (returnToApplicationSummary is true)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId });
        }

        return RedirectToAction(nameof(Operations), new { applicationId });
    }
}