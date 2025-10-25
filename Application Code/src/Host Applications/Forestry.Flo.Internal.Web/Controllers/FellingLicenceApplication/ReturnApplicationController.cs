using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
[AutoValidateAntiforgeryToken]
[Route("[controller]")]
public class ReturnApplicationController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        Guid id,
        [FromServices] IFellingLicenceApplicationUseCase applicationUseCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (!user.CanApproveApplications)
        {
            this.AddErrorMessage("Your account is not currently allowed to approve/refuse applications.  If you believe this to be in error, please contact your local administrator.");
            return RedirectToAction("Index", new { id });
        }

        var (_, isFailure, model) = await LoadViewModel(applicationUseCase, id, user, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model);
    }

    [HttpPost("")]
    public async Task<IActionResult> ReturnApplication(
        Guid id,
        ReturnApplicationModel model,
        [FromServices] IFellingLicenceApplicationUseCase applicationUseCase,
        [FromServices] IReturnApplicationUseCase approvalRefusalUseCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (!user.CanApproveApplications)
        {
            this.AddErrorMessage("Your account is not currently allowed to approve/refuse applications.  If you believe this to be in error, please contact your local administrator.");
            return RedirectToAction("Index", new { id });
        }

        if (string.IsNullOrWhiteSpace(model.ReturnReason.CaseNote))
        {
            (_, var isFailure, model) = await LoadViewModel(applicationUseCase, id, user, cancellationToken);

            if (isFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            ModelState.AddModelError(nameof(ReturnApplicationModel.ReturnReason.CaseNote), "Enter a reason for the application being returned");
            return View(nameof(Index), model);
        }

        var result = await approvalRefusalUseCase.ReturnApplication(
                user,
                id,
                model.ReturnReason,
                cancellationToken);

        if (result is { IsSuccess: true, SubProcessFailures.Count: > 0 })
        {
            var warnings = new StringBuilder();

            warnings.AppendJoin(", ", result.SubProcessFailures.Select(x => x.GetDescription()));

            if (warnings.Length > 0)
            {
                this.AddUserGuide("One or more issues occured", warnings.ToString());
            }
        }
        this.AddConfirmationMessage($"Application successfully returned.");
        return RedirectToAction("ApplicationSummary", "FellingLicenceApplication", new { id });
    }


    private async Task<Result<ReturnApplicationModel>> LoadViewModel(
        IFellingLicenceApplicationUseCase applicationUseCase,
        Guid id,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        var summary = await applicationUseCase.RetrieveFellingLicenceApplicationReviewSummaryAsync(
        id,
            user,
            cancellationToken);

        if (summary.HasNoValue)
        {
            return Result.Failure<ReturnApplicationModel>("Could not load application");
        }

        if (summary.Value.FellingLicenceApplicationSummary!.Status is not FellingLicenceStatus.SentForApproval
            || summary.Value.FellingLicenceApplicationSummary.AssigneeHistories.NotAny(x => x.UserAccount?.Id == summary.Value.ViewingUser?.UserAccountId && x.Role is AssignedUserRole.FieldManager))
        {
            return Result.Failure<ReturnApplicationModel>("User not authorised to return this application");
        }

        return Result.Success(new ReturnApplicationModel
        {
            FellingLicenceApplicationSummary = summary.Value.FellingLicenceApplicationSummary,
            RequestedStatus = summary.Value.FellingLicenceApplicationSummary.PreviousStatus,
            ReturnReason = new FormLevelCaseNote
            {
                InsetTextHeading = "Reason for returning the application"
            },
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
        });
    }
}

