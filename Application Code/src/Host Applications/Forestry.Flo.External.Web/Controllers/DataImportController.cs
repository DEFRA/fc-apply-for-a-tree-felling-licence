using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.DataImport;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.DataImport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize]
[TypeFilter(typeof(ApplicationExceptionFilter))]
public class DataImportController : Controller
{
    [HttpGet]
    public IActionResult Index(Guid woodlandOwnerId)
    {
        var user = new ExternalApplicant(User);
        var model = new DataImportViewModel
        {
            WoodlandOwnerId = woodlandOwnerId,
            Breadcrumbs = new BreadcrumbsModel
            {
                CurrentPage = "Data import",
                Breadcrumbs =
                [
                    new BreadCrumb("Home", "Home", "Index", null),
                    new BreadCrumb(user.WoodlandOwnerName!, "Home", "WoodlandOwner", null)
                ]
            }
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ParseImportFiles(
        Guid woodlandOwnerId,
        FormFileCollection dataImportFiles,
        [FromServices] DataImportUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.ParseImportDataFilesAsync(user, woodlandOwnerId, dataImportFiles, cancellationToken);

        if (result.IsFailure)
        {
            var error = "There were errors parsing the data within the provided files:\n\n" + string.Join("\n", result.Error);
            this.AddErrorMessage(error);
            return RedirectToAction(nameof(Index), new { woodlandOwnerId });
        }

        StoreParsedDataModel(result.Value);

        return RedirectToAction(nameof(CheckParsedData), new { woodlandOwnerId });
    }

    [HttpGet]
    public async Task<IActionResult> CheckParsedData(
        Guid woodlandOwnerId,
        [FromServices] DataImportUseCase useCase)
    {
        var data = RetrieveParsedDataModel<ImportFileSetContents>();

        if (data.HasNoValue)
        {
            this.AddErrorMessage("No data has been imported yet. Please import data first.");
            return RedirectToAction(nameof(Index), new { woodlandOwnerId });
        }

        var user = new ExternalApplicant(User);

        var model = new ParsedDataViewModel
        {
            WoodlandOwnerId = woodlandOwnerId,
            ImportFileSetContents = data.Value,
            Breadcrumbs = new BreadcrumbsModel
            {
                CurrentPage = "Check parsed data",
                Breadcrumbs =
                [
                    new BreadCrumb("Home", "Home", "Index", null),
                    new BreadCrumb(user.WoodlandOwnerName!, "Home", "WoodlandOwner", null),
                    new BreadCrumb("Data import", "DataImport", "Index", null)
                ]
            }
        };

        StoreParsedDataModel(data.Value);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ImportParsedData(
        Guid woodlandOwnerId,
        [FromServices] DataImportUseCase useCase,
        CancellationToken cancellationToken)
    {
        var data = RetrieveParsedDataModel<ImportFileSetContents>();

        if (data.HasNoValue)
        {
            this.AddErrorMessage("No data has been imported yet. Please import data first.");
            return RedirectToAction(nameof(Index), new { woodlandOwnerId });
        }

        var user = new ExternalApplicant(User);

        var result = await useCase.ImportDataAsync(user, woodlandOwnerId, data.Value, cancellationToken);

        if (result.IsFailure)
        {
            var error = "There were errors importing the data within the provided files:\n\n" + string.Join("\n", result.Error);
            this.AddErrorMessage(error);
            return RedirectToAction(nameof(Index), new { woodlandOwnerId });
        }

        this.AddConfirmationMessage("Your data was imported successfully. Complete any remaining tasks to submit your application.");

        // if not exactly one application was imported, redirect to woodland owner dashboard
        if (result.Value.Count != 1)
        {
            return RedirectToAction(nameof(HomeController.WoodlandOwner), "Home", new { woodlandOwnerId });
        }

        var applicationId = result.Value.Keys.First();
        if (user.IsFcUser)
        {
            // FC user - redirect to "is for ten year licence?" page
            return RedirectToAction(nameof(FellingLicenceApplicationController.TenYearLicence), "FellingLicenceApplication", new { applicationId, fromDataImport = true });
        }

        //normal external applicant - redirect to felling and restocking playback
        return RedirectToAction(nameof(FellingLicenceApplicationController.FellingAndRestockingPlayback), "FellingLicenceApplication", new { applicationId });
    }

    private void StoreParsedDataModel(ImportFileSetContents model) => TempData[DataImportUseCase.ParsedDataKey] = JsonConvert.SerializeObject(model);

    private Maybe<T> RetrieveParsedDataModel<T>()
    {
        if (!TempData.ContainsKey(DataImportUseCase.ParsedDataKey))
        {
            return Maybe<T>.None;
        }
        
        try
        {
            return Maybe<T>.From(
                JsonConvert.DeserializeObject<T>((TempData[DataImportUseCase.ParsedDataKey] as string)!)!);
        }
        catch 
        {
            return Maybe<T>.None;
        }
    }
}

