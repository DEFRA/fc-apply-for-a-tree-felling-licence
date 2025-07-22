using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.Agency;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize, RequireCompletedRegistration, UserIsAgentOrAgentAdministrator]
public class AgentAuthorityFormController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] AgentAuthorityFormUseCase useCase,
        Guid agencyId,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var models = await useCase.GetAgentAuthorityFormsAsync(user, agencyId, cancellationToken);

        if (models.IsFailure)
        {
            this.AddErrorMessage("Could not retrieve Agent Authority Forms, please try again");
            return RedirectToAction(nameof(HomeController.AgentUser), "Home");
        }

        var viewModel = new AgentAuthorityFormsViewModel
        {
            AgencyId = agencyId,
            AgentAuthorityForms = models.Value,
            Breadcrumbs = new BreadcrumbsModel
            {
                Breadcrumbs = new List<BreadCrumb>
                {
                    new BreadCrumb("Home", "Home", "AgentUser", null)
                },
                CurrentPage = "Agent authority forms"
            }
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult ContactDetails(
        [FromQuery] Guid agencyId,
        [FromQuery] bool reset = false,
        [FromQuery] bool fromSummary = false)
    {
        var (hasValue, agentAuthorityModel) = GetAgentAuthorityFormModel();
        if (!hasValue || reset)
        {
            ClearModels();
            agentAuthorityModel = new AgentAuthorityFormModel
            {
                AgencyId = agencyId
            };
        }

        var viewModel = new ContactDetailsFormModel
        {
            Breadcrumbs = AddFormBreadcrumbs,
            FromSummary = fromSummary
        };

        if (!reset && hasValue)
        {
            viewModel.ContactAddress = agentAuthorityModel.ContactAddress;
            viewModel.ContactEmail = agentAuthorityModel.ContactEmail;
            viewModel.ContactName = agentAuthorityModel.ContactName;
            viewModel.IsOrganisation = agentAuthorityModel.IsOrganisation;
            viewModel.ContactTelephoneNumber = agentAuthorityModel.ContactTelephoneNumber;
        }

        StoreAgentAuthorityFormModel(agentAuthorityModel);

        return View(viewModel);
    }

    public IActionResult ContactDetails(ContactDetailsFormModel model)
    {
        var (hasValue, agentAuthorityModel) = GetAgentAuthorityFormModel();
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(ContactDetails));
        }

        if (!ModelState.IsValid)
        {
            StoreAgentAuthorityFormModel(agentAuthorityModel);
            model.Breadcrumbs = AddFormBreadcrumbs;
            return View(model);
        }

        var mustCompleteOrganisation = model.IsOrganisation is true
                                       && string.IsNullOrWhiteSpace(agentAuthorityModel.OrganisationName);

        agentAuthorityModel = agentAuthorityModel with
        {
            ContactEmail = model.ContactEmail,
            ContactAddress = model.ContactAddress!,
            ContactName = model.ContactName,
            ContactTelephoneNumber = model.ContactTelephoneNumber!,
            IsOrganisation = model.IsOrganisation!.Value,
            OrganisationAddress = model.IsOrganisation is false
                ? null
                : agentAuthorityModel.OrganisationAddress,
            OrganisationName = model.IsOrganisation is false
                ? null
                : agentAuthorityModel.OrganisationName
        };

        StoreAgentAuthorityFormModel(agentAuthorityModel);

        return (model.FromSummary || !agentAuthorityModel.IsOrganisation) && !mustCompleteOrganisation
            ? RedirectToAction(nameof(ConfirmationPage))
            : RedirectToAction(nameof(OrganisationDetails));
    }

    [HttpGet]
    public IActionResult OrganisationDetails()
    {
        var (hasValue, agentAuthorityModel) = GetAgentAuthorityFormModel();
        if (!hasValue)
        {
            ClearModels();
            return RedirectToAction(nameof(ContactDetails));
        }

        var viewModel = new OrganisationDetailsFormModel()
        {
            Breadcrumbs = AddFormBreadcrumbs,
            OrganisationAddress = agentAuthorityModel.OrganisationAddress ?? agentAuthorityModel.ContactAddress,
            OrganisationName = (agentAuthorityModel.OrganisationName ?? null)!
        };

        StoreAgentAuthorityFormModel(agentAuthorityModel);

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult OrganisationDetails(OrganisationDetailsFormModel model)
    {
        var (hasValue, agentAuthorityModel) = GetAgentAuthorityFormModel();
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(ContactDetails));
        }

        if (!ModelState.IsValid)
        {
            StoreAgentAuthorityFormModel(agentAuthorityModel);
            model.Breadcrumbs = AddFormBreadcrumbs;
            return View(model);
        }

        agentAuthorityModel = agentAuthorityModel with
        {
            OrganisationName = model.OrganisationName,
            OrganisationAddress = model.OrganisationAddress!
        };

        StoreAgentAuthorityFormModel(agentAuthorityModel);

        return RedirectToAction(nameof(ConfirmationPage));
    }

    
    [HttpGet]
    public IActionResult ConfirmationPage()
    {
        var (hasValue, agentAuthorityModel) = GetAgentAuthorityFormModel();
        if (!hasValue)
        {
            ClearModels();
            return RedirectToAction(nameof(ContactDetails));
        }

        agentAuthorityModel.Breadcrumbs = AddFormBreadcrumbs;
        StoreAgentAuthorityFormModel(agentAuthorityModel);

        return View(agentAuthorityModel);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmationPage(
        AgentAuthorityFormModel model,
        [FromServices] AgentAuthorityFormUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (hasValue, agentAuthorityModel) = GetAgentAuthorityFormModel();
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError);
            return RedirectToAction(nameof(ContactDetails));
        }

        // process saving details/auditing/notifications in usecase

        var result = await useCase.HandleNewAgentAuthorityRequestAsync(
            agentAuthorityModel,
            new ExternalApplicant(User),
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong saving the authority form information, please try again");
            ClearModels();
            return RedirectToAction(nameof(Index), "AgentAuthorityForm");
        }
        
        return RedirectToAction(nameof(Index), "AgentAuthorityFormDocuments", new { agentAuthorityId = result.Value.AgentAuthorityId });
    }
    
    private const string AgentAuthorityFormModel = "AgentAuthorityFormModel";

    private void StoreAgentAuthorityFormModel(AgentAuthorityFormModel viewModel) =>
        TempData[$"{AgentAuthorityFormModel}"] = JsonConvert.SerializeObject(viewModel);

    private Maybe<AgentAuthorityFormModel> GetAgentAuthorityFormModel()
    {
        return RetrieveTempData<AgentAuthorityFormModel>(AgentAuthorityFormModel);
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
        var keys = TempData.Keys.Where(k => k.Contains(AgentAuthorityFormModel));
        foreach (var key in keys)
        {
            TempData.Remove(key);
        }
    }

    private const string FormDataExpiredError = "Your form data has expired, please try again";

    private BreadcrumbsModel AddFormBreadcrumbs => new()
    {
        Breadcrumbs = new List<BreadCrumb>
        {
            new("Home", "Home", "AgentUser", null),
            new("Authority forms", "AgentAuthorityForm", "Index", null),
        },
        CurrentPage = "Add agent authority form"
    };
}
