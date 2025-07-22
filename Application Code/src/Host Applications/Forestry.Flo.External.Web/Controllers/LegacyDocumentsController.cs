using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize, RequireCompletedRegistration]
public class LegacyDocumentsController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] LegacyDocumentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        // FLOV2-2143 legacy documents no longer a requirement
        return RedirectToAction("Index", "Home");

        var user = new ExternalApplicant(User);

        var result = await useCase.RetrieveLegacyDocumentsAsync(user, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve legacy documents, please try again");
        }

        var list = result.IsSuccess ? result.Value : new List<LegacyDocumentModel>(0);

        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> GetContent(
        Guid id,
        [FromServices] LegacyDocumentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.RetrieveLegacyDocumentContentAsync(user, id, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve legacy document, please try again");
            return RedirectToAction("Index");
        }

        return result.Value;
    }
}