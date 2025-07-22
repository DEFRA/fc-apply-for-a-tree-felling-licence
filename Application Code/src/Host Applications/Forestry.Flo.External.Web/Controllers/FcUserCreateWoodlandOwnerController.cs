using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WoodlandOwnerModel = Forestry.Flo.External.Web.Models.WoodlandOwner.WoodlandOwnerModel;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize(Policy = AuthorizationPolicyConstants.FcUserPolicyName)]
[AutoValidateAntiforgeryToken]
[TypeFilter(typeof(ApplicationExceptionFilter))]
public class FcUserCreateWoodlandOwnerController : Controller
{
    private const string ModelName = "WoodlandOwnerModel";
    private const string FormDataExpiredError = "Your form data has expired, please try again";

    private readonly ILogger<FcUserCreateWoodlandOwnerController> _logger;

    public FcUserCreateWoodlandOwnerController(ILogger<FcUserCreateWoodlandOwnerController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult ContactDetails(
        [FromQuery] bool reset = false,
        [FromQuery] bool fromSummary = false)
    {
        var (hasValue, woodlandOwnerModel) = GetModel();
        if (!hasValue || reset)
        {
            ClearModels();
            woodlandOwnerModel = new WoodlandOwnerModel();
        }

        var viewModel = new ContactDetailsFormModel
        {
            Breadcrumbs = AddFormBreadcrumbs,
            FromSummary = fromSummary
        };

        if (!reset && hasValue)
        {
            viewModel.ContactAddress = woodlandOwnerModel.ContactAddress!;
            viewModel.ContactEmail = woodlandOwnerModel.ContactEmail;
            viewModel.ContactName = woodlandOwnerModel.ContactName!;
            viewModel.IsOrganisation = woodlandOwnerModel.IsOrganisation;
            viewModel.ContactTelephoneNumber = woodlandOwnerModel.ContactTelephoneNumber;
        }

        var user = new ExternalApplicant(User);        
        
        _logger.LogDebug("Received request to create a new Woodland owner from user having account Id [{userId}].",
        user.UserAccountId);

        StoreModel(woodlandOwnerModel);

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult ContactDetails(ContactDetailsFormModel model)
    {
        var (hasValue, woodlandOwnerModel) = GetModel();
        
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(ContactDetails));
        }

        if (!ModelState.IsValid)
        {
            StoreModel(woodlandOwnerModel);
            model.Breadcrumbs = AddFormBreadcrumbs;
            return View(model);
        }

        var mustCompleteOrganisation = model.IsOrganisation is true
                                       && string.IsNullOrWhiteSpace(woodlandOwnerModel.OrganisationName);

        woodlandOwnerModel.ContactEmail = model.ContactEmail;
        woodlandOwnerModel.ContactAddress = model.ContactAddress;
        woodlandOwnerModel.ContactName = model.ContactName;
        woodlandOwnerModel.ContactTelephoneNumber = model.ContactTelephoneNumber!;
        woodlandOwnerModel.IsOrganisation = model.IsOrganisation!.Value;
        woodlandOwnerModel.OrganisationAddress = model.IsOrganisation is false
            ? null
            : woodlandOwnerModel.OrganisationAddress;
        woodlandOwnerModel.OrganisationName = model.IsOrganisation is false
            ? null
            : woodlandOwnerModel.OrganisationName;
        
        StoreModel(woodlandOwnerModel);

        return (model.FromSummary || !woodlandOwnerModel.IsOrganisation) && !mustCompleteOrganisation
            ? RedirectToAction(nameof(ConfirmationPage))
            : RedirectToAction(nameof(OrganisationDetails));
    }

    [HttpGet]
    public IActionResult OrganisationDetails()
    {
        var (hasValue, woodlandOwnerModel) = GetModel();
        if (!hasValue)
        {
            ClearModels();
            return RedirectToAction(nameof(ContactDetails));
        }

        var viewModel = new OrganisationDetailsFormModel()
        {
            Breadcrumbs = AddFormBreadcrumbs,
            OrganisationAddress = woodlandOwnerModel.OrganisationAddress ?? woodlandOwnerModel.ContactAddress,
            OrganisationName = (woodlandOwnerModel.OrganisationName ?? null)!
        };

        StoreModel(woodlandOwnerModel);

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult OrganisationDetails(OrganisationDetailsFormModel model)
    {
        var (hasValue, woodlandOwnerModel) = GetModel();
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(ContactDetails));
        }

        if (!ModelState.IsValid)
        {
            StoreModel(woodlandOwnerModel);
            model.Breadcrumbs = AddFormBreadcrumbs;
            return View(model);
        }

        woodlandOwnerModel.OrganisationName = model.OrganisationName;
        woodlandOwnerModel.OrganisationAddress = model.OrganisationAddress!;

        StoreModel(woodlandOwnerModel);

        return RedirectToAction(nameof(ConfirmationPage));
    }

    [HttpGet]
    public IActionResult ConfirmationPage()
    {
        var (hasValue, woodlandOwnerModel) = GetModel();
        if (!hasValue)
        {
            ClearModels();
            return RedirectToAction(nameof(ContactDetails));
        }

        woodlandOwnerModel.Breadcrumbs = AddFormBreadcrumbs;
        StoreModel(woodlandOwnerModel);

        return View(woodlandOwnerModel);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmationPage(
        [FromServices] FcAgentCreatesWoodlandOwnerUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (hasValue, woodlandOwnerModel) = GetModel();
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(ContactDetails));
        }

        var user = new ExternalApplicant(User);

        _logger.LogDebug("Submitting request to create a new Woodland owner from user having account Id [{userId}].",
            user.UserAccountId);

        var result = await useCase.CreateWoodlandOwnerAsync(user,  woodlandOwnerModel, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Unable to create new Woodland Owner requested by Fc user having account id [{userId}].",
                user.UserAccountId);

            this.AddErrorMessage(
                "Woodland owner details could not be added, try again. Contact support if this issue persists.");
        }
        else
        {
            this.AddConfirmationMessage("Woodland Owner details added successfully.");
        }

        return RedirectToAction(nameof(FcUserController.Index), "FcUser");
    }


    private void StoreModel(WoodlandOwnerModel woodlandOwnerModel)
    {
        TempData[$"{ModelName}"] = JsonConvert.SerializeObject(woodlandOwnerModel);
    }

    private Maybe<WoodlandOwnerModel> GetModel()
    {
        return RetrieveTempData<WoodlandOwnerModel>(ModelName);
    }

    private Maybe<T> RetrieveTempData<T>(string keyString)
    {
        return TempData.ContainsKey($"{keyString}")
            ? Maybe<T>
                .From(JsonConvert.DeserializeObject<T>(
                    (TempData[$"{keyString}"] as string)!)!)
            : Maybe<T>.None;
    }

    private void ClearModels()
    {
        var keys = TempData.Keys.Where(k => k.Contains(ModelName));
        foreach (var key in keys)
        {
            TempData.Remove(key);
        }
    }

    private BreadcrumbsModel AddFormBreadcrumbs => new()
    {
        Breadcrumbs = new List<BreadCrumb>
        {
            new("Home", "Home", "AgentUser", null),
        },
        CurrentPage = "Create new woodland owner"
    };
}