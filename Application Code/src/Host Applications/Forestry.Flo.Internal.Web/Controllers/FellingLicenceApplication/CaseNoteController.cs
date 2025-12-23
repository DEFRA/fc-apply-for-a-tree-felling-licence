using Ardalis.GuardClauses;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[ValidateAntiForgeryToken]
public class CaseNoteController : Controller
{
    [HttpPost]
    public async Task<IActionResult> AddCaseNote(
        AddCaseNoteModel model, 
        [FromServices] IAmendCaseNotes amendCaseNotes,
        [FromServices] IUrlHelper urlHelper,
        CancellationToken cancellationToken)
    {
        var user = Guard.Against.Null(new InternalUser(User));

        if (!string.IsNullOrWhiteSpace(model.Text))
        {
            var caseNoteRecord = new AddCaseNoteRecord(
                FellingLicenceApplicationId: model.FellingLicenceApplicationId,
                Type: model.CaseNoteType,
                Text: model.Text,
                VisibleToApplicant: model.VisibleToApplicant,
                VisibleToConsultee: model.VisibleToConsultee
            );

            var result = await amendCaseNotes.AddCaseNoteAsync(caseNoteRecord, user.UserAccountId.Value, cancellationToken);

            if (result.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }
        }
        else
        {
            this.AddErrorMessage("Enter some text to save a case note", nameof(model.Text));
        }

        if (urlHelper.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }
        return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id = model.FellingLicenceApplicationId });
    }
}

