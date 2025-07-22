using FluentValidation;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.External.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Services.Common.User;
using Newtonsoft.Json;

namespace Forestry.Flo.External.Web.Controllers
{
    [Authorize, RequireCompletedRegistration]
    public class PropertyProfileController : Controller
    {
        private readonly IValidator<PropertyProfileModel> _validator;

        private const string ChangeTheNameAndTryAgainMessage =
            "The woodland name has been used already, change the name and try again";
        private const string PropertyDoesNotExistMessage =
            "The woodland does not exist, create a new woodland.";

        public PropertyProfileController(IValidator<PropertyProfileModel> validator)
        {
            _validator = validator;

        }

        // GET: Compartment/Index/1
        public async Task<ActionResult> Index(Guid id,
            Guid? agencyId,
            [FromQuery] Guid woodlandOwnerId,
            [FromServices] ManagePropertyProfileUseCase useCase,
            CancellationToken cancellationToken)
        {
            var user = new ExternalApplicant(User);
            var compartmentListModel =
                await useCase.RetrievePropertyProfileCompartments(id, user, cancellationToken);

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = compartmentListModel.Value.WoodlandOwnerId;
            compartmentListModel.Value.AgencyId = agencyId;

            switch (compartmentListModel.HasValue)
            {
                case false:
                    this.AddErrorMessage(PropertyDoesNotExistMessage);
                    return RedirectToAction(nameof(Create), "PropertyProfile");
                default:
                    return CreateViewWithBreadcrumbs(compartmentListModel.Value, "Index");
            }
        }

        [HttpPost]
        public ActionResult CreateFromNewApplication(
            Guid applicationId,
            Guid woodlandOwnerId
            )
        {
            return RedirectToAction(nameof(Create), "PropertyProfile", new { isApplication = true, applicationId, woodlandOwnerId = woodlandOwnerId });
        }

        // GET: PropertyProfile/Create
        [HttpGet]
        public async Task<ActionResult> Create(
            [FromQuery] bool? isApplication,
            [FromQuery] Guid applicationId,
            [FromQuery] Guid woodlandOwnerId,
            [FromQuery] Guid? agencyId,
            [FromServices] AgentUserHomePageUseCase agentUserHomePageUseCase,
            CancellationToken cancellationToken)
        {
            var user = new ExternalApplicant(User);

            if (woodlandOwnerId == Guid.Empty)
            {
                woodlandOwnerId = Guid.Parse(user.WoodlandOwnerId!);
            }

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = woodlandOwnerId;

            var agencyIdToUse = ChooseAgencyId(agencyId, user);
            var clientName = await GetClientName(agencyIdToUse, woodlandOwnerId, user, agentUserHomePageUseCase, cancellationToken);

            var model = new PropertyProfileModel
            {
                ApplicationId = applicationId,
                IsApplication = isApplication,
                WoodlandOwnerId = woodlandOwnerId,
                ClientName = clientName,
                AgencyId = agencyIdToUse,
                Compartments = Array.Empty<CompartmentModel>()
            };
            return CreateViewWithBreadcrumbs(model, "CreateUpdate", "Create");
        }

        // GET: PropertyProfile/Edit/1
        [HttpGet]
        public async Task<ActionResult> Edit(
            Guid id,
            Guid woodlandOwnerId,
            Guid? agencyId,
            [FromServices] ManagePropertyProfileUseCase useCase,
            [FromServices] AgentUserHomePageUseCase agentUserHomePageUseCase,
            CancellationToken cancellationToken)
        {
            var user = new ExternalApplicant(User);
            var model = await useCase.RetrievePropertyProfileAsync(id, user, cancellationToken);

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = woodlandOwnerId;

            switch (model.HasValue)
            {
                case false:
                    this.AddErrorMessage(PropertyDoesNotExistMessage);
                    return RedirectToAction(nameof(Create));
                default:
                    var agencyIdToUse = ChooseAgencyId(agencyId, user);
                    model.Value.AgencyId = agencyIdToUse;
                    model.Value.ClientName = await GetClientName(agencyIdToUse, woodlandOwnerId, user, agentUserHomePageUseCase, cancellationToken);
                    model.Value.Compartments = model.Value.Compartments;

                    var gisCompartment = model.Value.Compartments.Select(c => new
                    {
                        c.Id,
                        c.GISData,
                        DisplayName = c.CompartmentNumber,
                        Selected = true
                    });

                    model.Value.GIS = JsonConvert.SerializeObject(gisCompartment);

                    return CreateViewWithBreadcrumbs(model.Value, "CreateUpdate");
            }
        }

        private async Task<IActionResult> HandleCreateOrAddCompartment(PropertyProfileModel model,
            bool addCompartment,
            int? compartmentCount,
            ManagePropertyProfileUseCase useCase,
            CreateFellingLicenceApplicationUseCase _createFellingLicenceApplicationUseCase,
            CancellationToken cancellationToken)
        {
            ApplyValidationModelErrors(model);

            if (!ModelState.IsValid)
            {
                return CreateViewWithBreadcrumbs(model, "CreateUpdate");
            }

            var user = new ExternalApplicant(User);
            var result = await useCase.CreatePropertyProfile(model, user, cancellationToken);
            switch (result.IsFailure)
            {
                case true when result.Error.ErrorType == ErrorTypes.Conflict && result.Error.FieldName is not null &&
                               result.Error.FieldName == nameof(model.Name):
                    ModelState.AddModelError(result.Error.FieldName, ChangeTheNameAndTryAgainMessage);
                    return CreateViewWithBreadcrumbs(model, "CreateUpdate", "Create");
                case true:
                    return RedirectToAction(nameof(HomeController.Error), "Home");
                default:
                    if (model.IsApplication.GetValueOrDefault())
                    {
                        if (model.ApplicationId.Equals(Guid.Empty))
                        {
                            var createApplicationResult = await _createFellingLicenceApplicationUseCase.CreateFellingLicenceApplication(
                                user,
                                result.Value.Id,
                                model.WoodlandOwnerId,
                                true,
                                cancellationToken);

                            if (createApplicationResult.IsSuccess)
                            {
                                return RedirectToAction(nameof(FellingLicenceApplicationController.Operations), "FellingLicenceApplication", new { applicationId = createApplicationResult.Value });
                            }
                            return RedirectToAction(nameof(FellingLicenceApplicationController.SelectWoodland), "FellingLicenceApplication", new { woodlandOwnerId = model.WoodlandOwnerId, agencyId = model.AgencyId });
                        }
                        return RedirectToAction(nameof(FellingLicenceApplicationController.UpdateSelectWoodland), "FellingLicenceApplication", new { applicationId = model.ApplicationId });
                    }
                    if (addCompartment || compartmentCount == 0)
                    {
                        return RedirectToAction(nameof(CompartmentController.CreateDetails), "Compartment", new { propertyProfileId = result.Value.Id, woodlandOwnerId = model.WoodlandOwnerId, agencyId = model.AgencyId });
                    }
                    return RedirectAfterPropertySave(model.WoodlandOwnerId, model.AgencyId);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("PropertyProfile/Create")]
        [Route("PropertyProfile/Create/Save")]
        public async Task<IActionResult> Create(PropertyProfileModel model,
            [FromForm] int? compartmentCount,
            [FromServices] ManagePropertyProfileUseCase useCase,
            [FromServices] CreateFellingLicenceApplicationUseCase _createFellingLicenceApplicationUseCase,
            CancellationToken cancellationToken)
        {
            return await HandleCreateOrAddCompartment(model, false, compartmentCount, useCase, _createFellingLicenceApplicationUseCase, cancellationToken);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("PropertyProfile/Create/AddCompartment")]
        public async Task<IActionResult> AddCompartment(PropertyProfileModel model,
            [FromForm] int? compartmentCount,
            [FromServices] ManagePropertyProfileUseCase useCase,
            [FromServices] CreateFellingLicenceApplicationUseCase _createFellingLicenceApplicationUseCase,
            CancellationToken cancellationToken)
        {
            return await HandleCreateOrAddCompartment(model, true, compartmentCount, useCase, _createFellingLicenceApplicationUseCase, cancellationToken);
        }

        // POST: PropertyProfile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PropertyProfileModel model,
            [FromForm] int? compartmentCount,
            [FromServices] ManagePropertyProfileUseCase useCase,
            CancellationToken cancellationToken)
        {
            ApplyValidationModelErrors(model);
            if (!ModelState.IsValid)
            {
                return CreateViewWithBreadcrumbs(model, "CreateUpdate");
            }

            var user = new ExternalApplicant(User);

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = model.WoodlandOwnerId;

            var result = await useCase.EditPropertyProfile(model, user, cancellationToken);
            switch (result.IsFailure)
            {
                case true when result.Error.ErrorType == ErrorTypes.Conflict && result.Error.FieldName is not null &&
                               result.Error.FieldName == nameof(model.Name):
                    ModelState.AddModelError(result.Error.FieldName, ChangeTheNameAndTryAgainMessage);
                    return CreateViewWithBreadcrumbs(model, "CreateUpdate");
                case true:
                    return RedirectToAction(nameof(HomeController.Error), "Home");
                default:
                    if (compartmentCount == 0 || (HttpContext.Request.Path.HasValue && HttpContext.Request.Path.Value.Contains("AddCompartment")))
                    {
                        return RedirectToAction(nameof(CompartmentController.CreateDetails), "Compartment", new { propertyProfileId = result.Value.Id, woodlandOwnerId = model.WoodlandOwnerId, agencyId = model.AgencyId });
                    }
                    return RedirectAfterPropertySave(model.WoodlandOwnerId, model.AgencyId);
            }
        }

        // GET: PropertyProfile/List
        [HttpGet]
        public async Task<ActionResult> List(
            Guid woodlandOwnerId,
            [FromServices] WoodlandOwnerHomePageUseCase useCase,
            CancellationToken cancellationToken)
        {
            var user = new ExternalApplicant(User);

            if (woodlandOwnerId == Guid.Empty)
            {
                woodlandOwnerId = Guid.Parse(user.WoodlandOwnerId!);
            }

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = woodlandOwnerId;

            var result = await useCase.RetrievePropertyProfilesForWoodlandOwnerAsync(
                woodlandOwnerId,
                user,
                cancellationToken);

            if (result.IsFailure)
            {
                return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            var model = result.Value.Any()
                ? ModelMapping.ToPropertyProfileDetailsListViewModel(result.Value)
                : new PropertyProfileDetailsListViewModel();

            model.Breadcrumbs = new BreadcrumbsModel
            {
                Breadcrumbs = DetermineDefaultBreadCrumbs()
            };

            return View(model);
        }

        // GET: PropertyProfile/Question
        [HttpGet]
        public ActionResult Question(Guid id,
            [FromQuery] Guid woodlandOwnerId,
            [FromQuery] Guid? agencyId)
        {
            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = woodlandOwnerId;

            var model = new PropertyProfileImportQuestion
            {
                Id = id,
                Breadcrumbs = new BreadcrumbsModel
                {
                    Breadcrumbs = DetermineDefaultBreadCrumbs()
                },
                AgencyId = agencyId
            };
            return View(model);
        }

        // Post: PropertyProfile/Question
        [HttpPost]
        public ActionResult Question(PropertyProfileImportQuestion model,
            [FromQuery] Guid woodlandOwnerId)
        {
            return model.ImportShapes == true
                ? RedirectToAction(nameof(CompartmentController.BulkImport), "Compartment", new { propertyProfileId = model.Id, woodlandOwnerId, agencyId = model.AgencyId })
                : RedirectToAction(nameof(Index), new { id = model.Id, agencyId = model.AgencyId });
        }

        [HttpGet]
        public async Task<ActionResult> BackToWoodlands(Guid woodlandOwnerId, Guid? agencyId, CancellationToken cancellationToken)
        {
            var user = new ExternalApplicant(User);

            var isAgent = user.AccountType == AccountTypeExternal.Agent || user.AccountType == AccountTypeExternal.AgentAdministrator || user.AccountType == AccountTypeExternal.FcUser;

            if (isAgent)
            {
                return RedirectToAction(nameof(WoodlandOwnerController.ManagedClientSummary), "WoodlandOwner", new { woodlandOwnerId, agencyId });
            }
            else
            {
                return RedirectToAction(nameof(List), "PropertyProfile", new { woodlandOwnerId });
            }
        }

        private ActionResult CreateViewWithBreadcrumbs<T>(T model, string viewName, string? currentPageDisplayText = "") where T : IPropertyWithBreadcrumbsViewModel
        {
            if (string.IsNullOrEmpty(currentPageDisplayText))
            {
                SetBreadcrumbs(model);
            }
            else
            {
                SetBreadcrumbs(model, currentPageDisplayText);

            }
            return View(viewName, model);
        }

        private void ApplyValidationModelErrors(PropertyProfileModel model)
        {
            ModelState.Clear();

            var validationErrors = _validator.Validate(model).Errors;

            if (!validationErrors.Any()) return;

            foreach (var validationFailure in validationErrors)
            {
                ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
            }
        }

        private void SetBreadcrumbs(IPropertyWithBreadcrumbsViewModel model)
        {
            model.Breadcrumbs = new BreadcrumbsModel
            {
                Breadcrumbs = DetermineDefaultBreadCrumbs(),
                CurrentPage = string.IsNullOrWhiteSpace(model.Name) ? string.Empty : model.Name
            };
        }

        private void SetBreadcrumbs(IPropertyWithBreadcrumbsViewModel model, string currentPageDisplayText)
        {
            model.Breadcrumbs = new BreadcrumbsModel
            {
                Breadcrumbs = DetermineDefaultBreadCrumbs(),
                CurrentPage = currentPageDisplayText
            };
        }

        private List<BreadCrumb> DetermineDefaultBreadCrumbs()
        {
            List<BreadCrumb> breadCrumbs = new List<BreadCrumb>
            {
                new ("Home", "Home", "Index", null),
            };

            ExternalApplicant user = new ExternalApplicant(User);

            if (user.AccountType is AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator)
            {
                var woodlandOwnerName =
                    string.IsNullOrEmpty(user.WoodlandOwnerName) ? "Woodland Owner" : user.WoodlandOwnerName;

                breadCrumbs.Add(new(woodlandOwnerName, "Home", "WoodlandOwner", null));
            }

            breadCrumbs.Add(new("Woodlands", "PropertyProfile", "List", null));

            return breadCrumbs;
        }

        private async Task<string> GetClientName(Guid? agencyId, Guid woodlandOwnerId, ExternalApplicant? user, AgentUserHomePageUseCase agentUserHomePageUseCase, CancellationToken cancellationToken)
        {
            var agencyIdToUse = ChooseAgencyId(agencyId, user);

            if (user is { AccountType: AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator or AccountTypeExternal.FcUser }
                && agencyIdToUse.HasValue)
            {
                var woodlandOwners = await agentUserHomePageUseCase.GetWoodlandOwnersForAgencyAsync(user, agencyIdToUse.Value, cancellationToken);
                if (woodlandOwners.HasValue)
                {
                    var thisWoodlandOwner = woodlandOwners.Value.FirstOrDefault(w => w.Id == woodlandOwnerId);
                    if (thisWoodlandOwner != null)
                    {
                        return thisWoodlandOwner.DisplayName;
                    }
                }
            }
            return string.Empty;
        }

        private static Guid? ChooseAgencyId(Guid? agencyId, ExternalApplicant? user)
        {
            return agencyId ?? (Guid.TryParse(user?.AgencyId, out var userAgencyId) ? userAgencyId : null);
        }

        private IActionResult RedirectAfterPropertySave(Guid woodlandOwnerId, Guid? agencyId)
        {
            var user = new ExternalApplicant(User);

            switch (user.AccountType)
            {
                case AccountTypeExternal.WoodlandOwner:
                case AccountTypeExternal.WoodlandOwnerAdministrator:
                    return RedirectToAction(nameof(List), new { woodlandOwnerId });
                default:
                    return RedirectToAction(nameof(WoodlandOwnerController.ManagedClientSummary), "WoodlandOwner", new { woodlandOwnerId, agencyId = agencyId.Value });
            }
        }
    }
}