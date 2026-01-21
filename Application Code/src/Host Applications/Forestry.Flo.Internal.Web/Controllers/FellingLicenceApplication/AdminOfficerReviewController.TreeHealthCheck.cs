using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public partial class AdminOfficerReviewController
{
    [HttpGet]
    public async Task<IActionResult> TreeHealthCheck(
        Guid applicationId,
        [FromServices] IAdminOfficerTreeHealthCheckUseCase treeHealthCheckUseCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, viewModel) = await treeHealthCheckUseCase
            .GetTreeHealthCheckAdminOfficerViewModelAsync(applicationId, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> TreeHealthCheck(
        CheckTreeHealthIssuesViewModel model,
        [FromServices] IAdminOfficerTreeHealthCheckUseCase treeHealthCheckUseCase,
        CancellationToken cancellationToken)
    {
        if (!model.Confirmed)
        {
            ModelState.Clear();

            var (_, isReloadFailure, viewModel) = await treeHealthCheckUseCase
                .GetTreeHealthCheckAdminOfficerViewModelAsync(model.ApplicationId, cancellationToken);

            if (isReloadFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            ModelState.AddModelError(nameof(model.Confirmed), "Confirm that you have checked the tree health issues and prioritised the application appropriately.");

            return View(viewModel);
        }

        var user = new InternalUser(User);

        var (_, isFailure) = await treeHealthCheckUseCase
            .ConfirmTreeHealthCheckedAsync(model.ApplicationId, user, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return RedirectToAction(nameof(Index), "AdminOfficerReview", new { id = model.ApplicationId });
    }
}