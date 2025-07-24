using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.DataImport;
using Forestry.Flo.External.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[TypeFilter(typeof(ApplicationExceptionFilter))]
public class DataImportController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(HomeController.Index), "Home");

        //Data import functionality is disabled until it is required
        //var user = new ExternalApplicant(User);
        //var model = new DataImportViewModel
        //{
        //    Breadcrumbs = new BreadcrumbsModel
        //    {
        //        CurrentPage = "Data import",
        //        Breadcrumbs = new List<BreadCrumb>
        //        {
        //            new BreadCrumb("Home", "Home", "Index", null),
        //            new BreadCrumb(user.WoodlandOwnerName!, "Home", "WoodlandOwner", null)
        //        }
        //    }
        //};
        //return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ImportData(
        FormFileCollection dataImportFiles,
        [FromServices] DataImportUseCase useCase,
        CancellationToken cancellationToken)
    {
        return RedirectToAction(nameof(HomeController.Index), "Home");

        //Data import functionality is disabled until it is required
        //var user = new ExternalApplicant(User);

        //var result = await useCase.PerformDataImportAsync(user, dataImportFiles, cancellationToken);

        //if (result.IsFailure)
        //{
        //    var error = "There were errors importing the data within the provided files:\n" + string.Join("\n", result.Error);
        //    this.AddErrorMessage(error);
        //    return RedirectToAction(nameof(Index));
        //}

        //this.AddConfirmationMessage("Data imported successfully");
        //return RedirectToAction(nameof(Index));
    }
}

