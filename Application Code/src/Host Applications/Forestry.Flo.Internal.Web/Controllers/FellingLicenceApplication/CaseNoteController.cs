using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Internal.Web.Services;
using Ardalis.GuardClauses;
using Forestry.Flo.Services.FellingLicenceApplications.Services;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[ValidateAntiForgeryToken]
public class CaseNoteController : Controller
{
    [HttpPost]
    public async Task<IActionResult> AddCaseNote(
        AddCaseNoteModel model, 
        [FromServices] IAmendCaseNotes amendCaseNotes,
        CancellationToken cancellationToken)
    {
        var user = Guard.Against.Null(new InternalUser(User));

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

        return Redirect(model.ReturnUrl);
    }
}

