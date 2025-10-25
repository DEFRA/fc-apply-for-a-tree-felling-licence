using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.AdminHub;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.AdminHubs.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers;

[Authorize, RequireAdminHubManagerUser]
[AutoValidateAntiforgeryToken]
public class AdminHubController : Controller
{
    [HttpGet]
    public async Task<IActionResult> AdminHubSummary(
        [FromServices] IManageAdminHubUseCase usecase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var model = await usecase.RetrieveAdminHubDetailsAsync(user, cancellationToken);
        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddAdminOfficer(
        ViewAdminHubModel model,
        [FromServices] IManageAdminHubUseCase usecase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var result = await usecase.AddAdminOfficerAsync(model, user, cancellationToken);

        if(result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong, please try again");
            return Redirect("AdminHubSummary");
        }

        this.AddConfirmationMessage("Officer successfully added to Admin Hub.");
        return Redirect("AdminHubSummary");
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAdminOfficer(
        ViewAdminHubModel model,
        [FromServices] IManageAdminHubUseCase usecase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var result = await usecase.RemoveAdminOfficerAsync(model, user, cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong, please try again");
            return Redirect("AdminHubSummary");
        }

        this.AddConfirmationMessage("Officer removed from the Admin Hub.");
        return Redirect("AdminHubSummary");
    }

    [HttpPost]
    public async Task<IActionResult> EditAdminHubDetails(
        ViewAdminHubModel model,
        [FromServices] IManageAdminHubUseCase usecase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        var result = await usecase.EditAdminHub(model, user, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == ManageAdminHubOutcome.NoChangeSubmitted)
            {
                this.AddErrorMessage("No changes were provided, please try again");
                return Redirect("AdminHubSummary");
            }

            this.AddErrorMessage("Something went wrong, please try again");
            return Redirect("AdminHubSummary");
        }
        this.AddConfirmationMessage("Admin Hub edited successful.");
        return Redirect("AdminHubSummary");
    }
}
