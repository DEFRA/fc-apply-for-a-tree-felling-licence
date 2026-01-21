using System.Web;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

[Authorize]
 [AutoValidateAntiforgeryToken]
public class ExternalConsulteeInviteController : Controller
{
    // GET index
    [HttpGet]
    public async Task<IActionResult> Index(
        Guid id,
        [FromServices] IExternalConsulteeInviteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, viewModel) = await useCase.GetConsulteeInvitesIndexViewModelAsync(
            id,
            cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        viewModel.Breadcrumbs = CreateBreadcrumbs(viewModel.FellingLicenceApplicationSummary!);
            
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Index(
        ExternalConsulteeIndexViewModel model,
        [FromServices] IExternalConsulteeInviteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (!ModelState.IsValid)
        {
            var (_, isFailure, viewModel) = await useCase.GetConsulteeInvitesIndexViewModelAsync(
                model.ApplicationId,
                cancellationToken);

            if (isFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            viewModel.Breadcrumbs = CreateBreadcrumbs(viewModel.FellingLicenceApplicationSummary!);

            return View(viewModel);
        }

        if (model.ApplicationNeedsConsultations is false)
        {
            var setNotNeededResult = await useCase.SetDoesNotRequireConsultationsAsync(
                model.ApplicationId, user, cancellationToken);

            if (setNotNeededResult.IsFailure)
            {
                this.AddErrorMessage("Could not update consultations to not needed");
                return RedirectToAction("Index", new { id = model.ApplicationId });
            }

            this.AddConfirmationMessage("Consultations updated to not needed");
            return RedirectToAction(nameof(Index), "WoodlandOfficerReview", new { id = model.ApplicationId });
        }

        var setCompleteResult = await useCase.SetConsultationsCompleteAsync(
            model.ApplicationId, user, cancellationToken);

        if (setCompleteResult.IsFailure)
        {
            this.AddErrorMessage("Could not complete consultations task");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        this.AddConfirmationMessage("Consultations completed");
        return RedirectToAction(nameof(Index), "WoodlandOfficerReview", new { id = model.ApplicationId });
    }

    // GET invite new consultee
    [HttpGet]
    public async Task<IActionResult> InviteNewConsultee(
        Guid id,
        [FromServices] IExternalConsulteeInviteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, model) = await useCase
            .GetNewExternalConsulteeInviteViewModelAsync(id, cancellationToken);
        
        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }
        
        model.Breadcrumbs = CreateBreadcrumbs(model.FellingLicenceApplicationSummary!);
        return View(model);
    }

    // POST invite new consultee
    [HttpPost]
    public async Task<IActionResult> InviteNewConsultee(
        ExternalConsulteeInviteFormModel model,
        [FromServices] IExternalConsulteeInviteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (!ModelState.IsValid)
        {
            var reloadModel = await useCase
                .GetNewExternalConsulteeInviteViewModelAsync(model.ApplicationId, cancellationToken);

            if (reloadModel.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            reloadModel.Value.ConsulteeName = model.ConsulteeName;
            reloadModel.Value.Email = model.Email;
            reloadModel.Value.Purpose = model.Purpose;
            reloadModel.Value.AreaOfFocus = model.AreaOfFocus;
            reloadModel.Value.SelectedDocumentIds = model.SelectedDocumentIds;
            reloadModel.Value.ExemptFromConsultationPublicRegister = model.ExemptFromConsultationPublicRegister;
            reloadModel.Value.Breadcrumbs = CreateBreadcrumbs(reloadModel.Value.FellingLicenceApplicationSummary!);

            return View(reloadModel.Value);
        }

        var accessCode = Guid.NewGuid();
        var inviteModel = new ExternalConsulteeInviteModel
        {
            Email = model.Email,
            ConsulteeEmailText = model.AreaOfFocus!,
            ConsulteeName = model.ConsulteeName,
            ExemptFromConsultationPublicRegister = model.ExemptFromConsultationPublicRegister!.Value,
            ExternalAccessCode = accessCode,
            ExternalAccessLink =
                $"{GetExternalConsulteeInviteLink()}?applicationId={model.ApplicationId}&accessCode={accessCode}&emailAddress={HttpUtility.HtmlEncode(model.Email)}",
            Purpose = model.Purpose!.GetDisplayName(),
            SelectedDocumentIds = model.SelectedDocumentIds.Where(x => x.HasValue).Select(x => x!.Value).ToList()
        };

        var result =
            await useCase.InviteExternalConsulteeAsync(inviteModel, model.ApplicationId, user, cancellationToken);

        if (result.IsSuccess)
        {
            this.AddConfirmationMessage("Consultee invite sent");
            return RedirectToAction("Index", new { id = model.ApplicationId });
        }

        return RedirectToAction("Error", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> GetReceivedComments(
        Guid id,
        Guid accessCode,
        [FromServices] IExternalConsulteeInviteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, viewModel) = await useCase.GetReceivedCommentsAsync(
            id,
            accessCode, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        viewModel.Breadcrumbs = CreateBreadcrumbs(viewModel.FellingLicenceApplicationSummary!);

        return View(viewModel);
    }

    private static BreadcrumbsModel CreateBreadcrumbs(FellingLicenceApplicationSummaryModel summaryModel, string currentPage = "Invite consultees")
    {
        var breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = new List<BreadCrumb> { 
                new("Open applications", "Home", "Index", null) ,
                new( summaryModel.ApplicationReference,
                    "FellingLicenceApplication",
                    "ApplicationSummary",
                    summaryModel.Id.ToString()),
                new( "Woodland Officer Review", "WoodlandOfficerReview", "Index", summaryModel.Id.ToString())
            },
            CurrentPage = currentPage
        };
        return breadcrumbs;
    }
    
    private string GetExternalConsulteeInviteLink() => Url.AbsoluteAction("Index", "ExternalConsulteeReview")!;
}