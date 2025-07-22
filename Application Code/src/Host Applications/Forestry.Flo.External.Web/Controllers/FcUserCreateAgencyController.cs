using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.FcUser;
using Forestry.Flo.External.Web.Models.Agency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Forestry.Flo.External.Web.Controllers;

/// <summary>
/// Controller class handling the request made by an FC user
/// to create a new managed agency/agent.
/// </summary>
[Authorize(Policy = AuthorizationPolicyConstants.FcUserPolicyName)]
[AutoValidateAntiforgeryToken]
[TypeFilter(typeof(ApplicationExceptionFilter))]
public class FcUserCreateAgencyController : Controller
{
    private const string FormDataExpiredError = "Your form data has expired, please try again";
    private readonly ILogger<FcUserCreateAgencyController> _logger;

    public FcUserCreateAgencyController(ILogger<FcUserCreateAgencyController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult AgentTypeSelection()
    {
        var user = new ExternalApplicant(User);
        _logger.LogDebug("Received request to create a new agency from user having account Id {userId}", user.UserAccountId);
        return View();
    }

    [HttpPost]
    public IActionResult AgentTypeSelection(AgentTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var agencyModel = new FcUserAgencyCreationModel
        {
            IsOrganisation = model.OrganisationStatus is OrganisationStatus.Organisation,
            OrganisationStatus = model.OrganisationStatus
        };

        StoreModel(agencyModel);

        return RedirectToAction(nameof(RegisterAgencyDetails));
    }

    [HttpGet]
    public IActionResult RegisterAgencyDetails()
    {
        var model = GetModel();

        if (model.HasNoValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(AgentTypeSelection));
        }

        if (model.Value.IsOrganisation is false)
        {
            model.Value.OrganisationName = null;
        }

        return View(model.Value);
    }

    [HttpPost]
    public IActionResult RegisterAgencyDetails(FcUserAgencyCreationModel model)
    {
        if (!model.IsOrganisation && ModelState.ContainsKey(nameof(model.OrganisationName)))
        {
            ModelState.Remove(nameof(model.OrganisationName));
        }
     
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        StoreModel(model);

        return RedirectToAction(nameof(FcCreateAgencySummary));
    }

    [HttpGet]
    public IActionResult FcCreateAgencySummary()
    {
        var model = GetModel();

        if (model.HasNoValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(AgentTypeSelection));
        }
        
        StoreModel(model.Value);

        return View(model.Value);
    }

    [HttpPost]
    public async Task<IActionResult> FcCreateAgencySummary(
        [FromServices] FcUserCreateAgencyUseCase useCase,
        CancellationToken cancellationToken)
    {
        var model = GetModel();
        if (model.HasNoValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(AgentTypeSelection));
        }
        var user = new ExternalApplicant(User);

        _logger.LogDebug("Submitting request to create a new agency from user having account Id {userId}", user.UserAccountId);

       var createResult = await useCase.ExecuteAsync(user, model.Value, cancellationToken);

        if (createResult.IsFailure)
        {
            _logger.LogWarning("Unable to create new agency requested by Fc user having account id {userId}",
                user.UserAccountId);

            this.AddErrorMessage(
                "Agency could not be added, try again. Contact support if this issue persists.");
        }
        else
        {
            ClearModels();
            this.AddConfirmationMessage("Agency details added successfully.");
        }

        return RedirectToAction("Index", "AgentAuthorityForm", new {agencyId = createResult.Value.AgencyId});
    }

    private void StoreModel(FcUserAgencyCreationModel model)
    {
        TempData[$"{nameof(FcUserAgencyCreationModel)}"] = JsonConvert.SerializeObject(model);
    }

    private Maybe<FcUserAgencyCreationModel> GetModel()
    {
        return RetrieveTempData<FcUserAgencyCreationModel>(nameof(FcUserAgencyCreationModel));
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
        var keys = TempData.Keys.Where(k => k.Contains(nameof(FcUserAgencyCreationModel)));
        foreach (var key in keys)
        {
            TempData.Remove(key);
        }
    }
}