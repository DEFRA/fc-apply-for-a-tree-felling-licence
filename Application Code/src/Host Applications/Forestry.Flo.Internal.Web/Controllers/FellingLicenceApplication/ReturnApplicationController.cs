using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
[Route("[controller]")]
public class ReturnApplicationController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        Guid id,
        [FromServices] FellingLicenceApplicationUseCase applicationUseCase,
        [FromServices] ReturnApplicationUseCase approvalRefusalUseCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (!user.CanApproveApplications)
        {
            this.AddErrorMessage("Your account is not currently allowed to approve/refuse applications.  If you believe this to be in error, please contact your local administrator.");
            return RedirectToAction("Index", new { id });
        }

        var summary = await applicationUseCase.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            id,
            user,
            cancellationToken);

        if (summary.HasNoValue)
        {
            return RedirectToAction("Error", "Home");
        }

        if (summary.Value.FellingLicenceApplicationSummary!.Status is not FellingLicenceStatus.SentForApproval
            || summary.Value.FellingLicenceApplicationSummary.AssigneeHistories.NotAny(x => x.UserAccount?.Id == summary.Value.ViewingUser?.UserAccountId && x.Role is AssignedUserRole.FieldManager))
        {
            return RedirectToAction("Index", new { id });
        }

        var confirmationModel = new ReturnApplicationModel
        {
            FellingLicenceApplicationSummary = summary.Value.FellingLicenceApplicationSummary,
            RequestedStatus = summary.Value.FellingLicenceApplicationSummary.PreviousStatus, 
            Breadcrumbs = new BreadcrumbsModel
            {
                CurrentPage = "Return Application",
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

    [HttpPost("")]
    public async Task<IActionResult> ReturnApplication(
        Guid id,
        ReturnApplicationModel model,
        [FromServices] FellingLicenceApplicationUseCase applicationUseCase,
        [FromServices] ReturnApplicationUseCase approvalRefusalUseCase,
        [FromServices] IAmendCaseNotes amendCaseNotes,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (!user.CanApproveApplications)
        {
            this.AddErrorMessage("Your account is not currently allowed to approve/refuse applications.  If you believe this to be in error, please contact your local administrator.");
            return RedirectToAction("Index", new { id });
        }

        var summary = await applicationUseCase.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            id,
            user,
            cancellationToken);

        if (summary.HasNoValue)
        {
            return RedirectToAction("Error", "Home");
        }

        var previousStatus = summary.Value.FellingLicenceApplicationSummary!.PreviousStatus;

        if (!string.IsNullOrWhiteSpace(model.CaseNote))
        {
            CaseNoteType caseNoteType = previousStatus switch
            {
                FellingLicenceStatus.WoodlandOfficerReview => CaseNoteType.WoodlandOfficerReviewComment,
                FellingLicenceStatus.AdminOfficerReview => CaseNoteType.AdminOfficerReviewComment,
                _ => CaseNoteType.CaseNote
            };

            var caseNoteRecord = new AddCaseNoteRecord(
                FellingLicenceApplicationId: id,
                Type: caseNoteType,
                Text: model.CaseNote,
                VisibleToApplicant: model.VisibleToApplicant,
                VisibleToConsultee: model.VisibleToConsultee
            );
            var caseNoteresult = await amendCaseNotes.AddCaseNoteAsync(caseNoteRecord, user.UserAccountId.Value, cancellationToken);

            if (caseNoteresult.IsFailure)
            {
                this.AddErrorMessage(caseNoteresult.Error);
                return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id });
            }
        }
        else
        {
            this.AddErrorMessage("A case note is required to return application");
            return RedirectToAction("Index", new { id });
        }

        var result = await approvalRefusalUseCase.ReturnApplication(
                user,
                id,
                cancellationToken);

        if (result is { IsSuccess: true, SubProcessFailures.Count: > 0 })
        {
            var warnings = new StringBuilder();

            if (warnings.Length > 0)
            {
                this.AddUserGuide("One or more issues occured", warnings.ToString());
            }
        }
        this.AddConfirmationMessage($"Application successfully returned.");
        return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id });
    }

}

