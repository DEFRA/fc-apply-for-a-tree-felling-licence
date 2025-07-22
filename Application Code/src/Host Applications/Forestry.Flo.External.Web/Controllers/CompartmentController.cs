using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize, RequireCompletedRegistration]
public class CompartmentController : Controller
{
    private readonly ManageGeographicCompartmentUseCase _manageGeographicCompartmentUseCase;
    private readonly ILogger<CompartmentController> _logger;
    private const string TheCompartmentDoesNotExistMessage = "The compartment does not exist, please select a woodland profile and create a compartment";
    private const string ChangeTheNameAndTryAgainMessage = "The number and sub compartment name combination is not unique, please change the value and try again";

    public CompartmentController(ManageGeographicCompartmentUseCase manageGeographicCompartmentUseCase,
        ILogger<CompartmentController> logger)
    {
        _manageGeographicCompartmentUseCase = manageGeographicCompartmentUseCase 
                                              ?? throw new ArgumentNullException(nameof(manageGeographicCompartmentUseCase));

        _logger = logger ?? new NullLogger<CompartmentController>();;
    }

    // GET: Compartment/EditDetails/1
    public async Task<ActionResult> EditDetails(
        Guid id,
        Guid propertyProfileId,
        Guid? applicationId,
        [FromQuery] Guid woodlandOwnerId,
        [FromQuery] Guid? agencyId,
        CancellationToken cancellationToken)
    {
        var result = await FindCompartmentAsync(id, cancellationToken);

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = woodlandOwnerId;
        result.Value.WoodlandOwnerId = woodlandOwnerId;
        result.Value.AgencyId = agencyId;

        switch (result.HasValue)
        {
            case false:
                this.AddErrorMessage(TheCompartmentDoesNotExistMessage);
                return RedirectToAction(nameof(PropertyProfileController.Index), "PropertyProfile");
            default:
                result.Value.ApplicationId = applicationId;
                return CreateViewWithBreadcrumbs(result.Value, "CreateUpdateDetails");
        }
    }

    // POST: Compartment/EditDetails
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> EditDetails(
        CompartmentModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return CreateViewWithBreadcrumbs(model, "CreateUpdateDetails");
        }

        var result = await EditCompartmentAsync(model, cancellationToken);
        switch (result.IsFailure)
        {
            case true when result.Error.ErrorType == ErrorTypes.Conflict && result.Error.FieldName is not null &&
                           result.Error.FieldName == nameof(model.CompartmentNumber):
                ModelState.AddModelError(result.Error.FieldName, ChangeTheNameAndTryAgainMessage);
                return CreateViewWithBreadcrumbs(model, "CreateUpdateDetails");
            case true:
                return RedirectToAction(nameof(HomeController.Error), "Home");
            default:
                return RedirectToAction(nameof(Draw), new { model.Id, woodlandOwnerId = model.WoodlandOwnerId, agencyId = model.AgencyId, propertyProfileId = model.PropertyProfileId });
        }
    }

    [HttpPost]
    public ActionResult CreateFromApplication(Guid propertyProfileId, Guid applicationId, bool isForRestockingCompartmentSelection, Guid? fellingCompartmentId, string? fellingCompartmentName, Guid? proposedFellingDetailsId, FellingOperationType? fellingOperationType)
    {
        return RedirectToAction(nameof(CreateDetails), new 
        { 
            propertyProfileId, 
            applicationId, 
            isForRestockingCompartmentSelection,
            fellingCompartmentId, 
            fellingCompartmentName, 
            proposedFellingDetailsId, 
            fellingOperationType 
        });
    }

    // GET: Compartment/CreateDetails?propertyProfileId=1
    public async Task<ActionResult> CreateDetails(
        [FromQuery] Guid? compartmentId,
        [FromQuery] Guid propertyProfileId,
        [FromQuery] Guid? applicationId,
        [FromQuery] Guid woodlandOwnerId,
        [FromServices] ManageGeographicCompartmentUseCase useCase,
        bool isForRestockingCompartmentSelection,
        string? fellingCompartmentName,
        Guid? fellingCompartmentId,
        Guid? proposedFellingDetailsId,
        FellingOperationType? fellingOperationType,
        Guid? agencyId,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.VerifyUserPropertyProfileAsync(user, propertyProfileId, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return RedirectToAction(nameof(ChooseCreationMethod), new
        {
            compartmentId,
            propertyProfileId,
            applicationId,
            woodlandOwnerId,
            isForRestockingCompartmentSelection,
            fellingCompartmentId,
            fellingCompartmentName,
            proposedFellingDetailsId,
            fellingOperationType,
            agencyId
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateUsingMap(
        Guid? compartmentId,
        Guid propertyProfileId,
        Guid? applicationId,
        Guid woodlandOwnerId,
        Guid? agencyId,
        [FromServices] ManageGeographicCompartmentUseCase useCase,
        bool isForRestockingCompartmentSelection,
        string? fellingCompartmentName,
        Guid? fellingCompartmentId,
        Guid? proposedFellingDetailsId,
        FellingOperationType? fellingOperationType,
        CancellationToken cancellationToken)
    {

        var user = new ExternalApplicant(User);

        var result = await useCase.VerifyUserPropertyProfileAsync(user, propertyProfileId, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return RedirectToAction(nameof(Draw), new
        {
            applicationId,
            isForRestockingCompartmentSelection,
            proposedFellingDetailsId,
            fellingCompartmentId,
            fellingCompartmentName,
            fellingOperationType,
            propertyProfileId,
            agencyId,
            woodlandOwnerId
        });

        if (woodlandOwnerId == Guid.Empty)
        {
            woodlandOwnerId = result.Value.WoodlandOwnerId;
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = woodlandOwnerId;

        var compartmentModel = new CompartmentModel
        {
            PropertyProfileId = propertyProfileId,
            PropertyProfileName = result.Value.Name,
            ApplicationId = applicationId,
            WoodlandOwnerId = woodlandOwnerId,
            IsForRestockingCompartmentSelection = isForRestockingCompartmentSelection,
            FellingCompartmentId = fellingCompartmentId.HasValue ? fellingCompartmentId.Value : Guid.Empty,
            FellingCompartmentName = fellingCompartmentName,
            ProposedFellingDetailsId = proposedFellingDetailsId,
            FellingOperationType = fellingOperationType,
            AgencyId = agencyId
        };
        
        if (compartmentId.HasValue && compartmentId != Guid.Empty)
        {
            var (found, c) = await useCase.RetrieveCompartmentAsync(compartmentId.Value, user, cancellationToken);
            if (found)
            {
                compartmentModel.CompartmentNumber = c.CompartmentNumber;
                compartmentModel.SubCompartmentName = c.SubCompartmentName;
                compartmentModel.Designation = c.Designation;
            }
        }

        return CreateViewWithBreadcrumbs(compartmentModel, "CreateUpdateDetails");
    }

    // POST: Compartment/CreateUsingMap
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateUsingMap(CompartmentModel model,
        [FromServices] ManageGeographicCompartmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return CreateViewWithBreadcrumbs(model, "CreateUpdateDetails");
        }

        var user = new ExternalApplicant(User);
        var (_, isFailure, id, error) = await useCase.CreateCompartmentAsync(model, user, cancellationToken);

        switch (isFailure)
        {
            case true when error.ErrorType == ErrorTypes.Conflict && error.FieldName is not null &&
                           error.FieldName == nameof(model.CompartmentNumber):
                ModelState.AddModelError(error.FieldName, ChangeTheNameAndTryAgainMessage);
                return CreateViewWithBreadcrumbs(model, "CreateUpdateDetails");
            case true:
                return RedirectToAction(nameof(HomeController.Error), "Home");
            default:
                return RedirectToAction(nameof(Draw), new 
                { 
                    id, 
                    applicationId = model.ApplicationId,
                    isForRestockingCompartmentSelection = model.IsForRestockingCompartmentSelection,
                    proposedFellingDetailsId = model.ProposedFellingDetailsId,
                    fellingCompartmentId = model.FellingCompartmentId,
                    fellingCompartmentName = model.FellingCompartmentName,
                    fellingOperationType = model.FellingOperationType,
                    woodlandOwnerId = model.WoodlandOwnerId,
                    agencyId = model.AgencyId
                });
        }
    }

    [HttpGet]
    public async Task<ActionResult> BulkImport(
        [FromQuery] Guid propertyProfileId, 
        [FromQuery] Guid? applicationId,
        [FromQuery] Guid woodlandOwnerId,
        [FromQuery] Guid? agencyId,
        [FromServices] ManageGeographicCompartmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.VerifyUserPropertyProfileAsync(user, propertyProfileId, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = result.Value.WoodlandOwnerId;

        var compartmentModel = new CompartmentModel
        {
            PropertyProfileId = propertyProfileId,
            PropertyProfileName = result.Value.Name,
            ApplicationId = applicationId,
            WoodlandOwnerId = woodlandOwnerId,
            AgencyId = agencyId
        };

        SetBreadcrumbs(compartmentModel, "Bulk Import");

        return View(compartmentModel);
    }

    // POST: Compartment/SaveNoMap/{componentId}}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Compartment/SaveNoMap")]
    [Route("Compartment/Draw/SaveNoMap")]
    public async Task<ActionResult> SaveNoMap(
        CompartmentDrawModel model,
        [FromServices] ManageGeographicCompartmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model);
            return View("Draw", model);
        }
        model.CompartmentModelOfInterest.GISData = string.Empty;
        model.CompartmentModelOfInterest.TotalHectares = 0;

        if (model.CompartmentModelOfInterest.Id == Guid.Empty)
        {
            var user = new ExternalApplicant(User);

            var (_, isFailure, newCompartmentId, error) = await useCase.CreateCompartmentAsync(model.CompartmentModelOfInterest, user, cancellationToken);

            switch (isFailure)
            {
                case true when error.ErrorType == ErrorTypes.Conflict && error.FieldName is not null &&
                       error.FieldName == nameof(model.CompartmentModelOfInterest.CompartmentNumber):
                    ModelState.AddModelError(error.FieldName, ChangeTheNameAndTryAgainMessage);
                    return RedirectToAction(nameof(Draw), new
                    {
                        model.ApplicationId,
                        model.IsForRestockingCompartmentSelection,
                        model.ProposedFellingDetailsId,
                        model.FellingCompartmentId,
                        model.FellingCompartmentName,
                        model.FellingOperationType,
                        model.CompartmentModelOfInterest.PropertyProfileId
                    });
                case true:
                    return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.CompartmentModelOfInterest.Id = newCompartmentId;
        }

        var (hasValue, compartment) = await FindCompartmentAsync(model.CompartmentModelOfInterest.Id, cancellationToken);

        if (!hasValue)
        {
            _logger.LogWarning("Did not find a compartment having id of {Id}", model.CompartmentModelOfInterest.Id);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var updateResult = await EditCompartmentAsync(compartment, cancellationToken);

        if (updateResult.IsFailure)
        {
            _logger.LogWarning("Unable to update compartment having id of {CompartmentId}", compartment.Id);
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        _logger.LogDebug("Compartment having id of {CompartmentId} was successfully updated",compartment.Id);
        return model.ApplicationId.GetValueOrDefault() == Guid.Empty ?
            RedirectToAction(nameof(PropertyProfileController.Edit), "PropertyProfile", new { id = compartment.PropertyProfileId, woodlandOwnerid = model.CompartmentModelOfInterest.WoodlandOwnerId, agencyId = model.CompartmentModelOfInterest.AgencyId  })
            : RedirectToAction(nameof(FellingLicenceApplicationController.SelectNewCompartment), "FellingLicenceApplication", new 
            {
                applicationId = model.ApplicationId , 
                newCompartmentId = compartment.Id,
                proposedFellingDetailsId = model.ProposedFellingDetailsId,
                isForRestockingCompartmentSelection = model.IsForRestockingCompartmentSelection,
                fellingOperationType = model.FellingOperationType,
                fellingCompartmentName = model.FellingCompartmentName,
                fellingCompartmentId = model.FellingCompartmentId
            });
    }

    // POST: Compartment/Draw/{componentId}}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Compartment/Draw")]
    public async Task<ActionResult> Draw(
        CompartmentDrawModel model,
        [FromServices] ManageGeographicCompartmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model);
            return View(model);
        }

        if (string.IsNullOrEmpty(model.CompartmentModelOfInterest.GISData))
        {
            ModelState.AddModelError(nameof(model.CompartmentModelOfInterest.GISData),"No Geometry has been drawn or provided.");
            SetBreadcrumbs(model);

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = model.CompartmentModelOfInterest.WoodlandOwnerId;

            return View(model);
        }

        if (model.CompartmentModelOfInterest.Id == Guid.Empty)
        {
            var user = new ExternalApplicant(User);

            var (_, isFailure, newCompartmentId, error) = await useCase.CreateCompartmentAsync(model.CompartmentModelOfInterest, user, cancellationToken);

            switch (isFailure)
            {
                case true when error.ErrorType == ErrorTypes.Conflict && error.FieldName is not null &&
                       error.FieldName == nameof(model.CompartmentModelOfInterest.CompartmentNumber):
                    ModelState.AddModelError(error.FieldName, ChangeTheNameAndTryAgainMessage);
                    return RedirectToAction(nameof(Draw), new
                    {
                        model.ApplicationId,
                        model.IsForRestockingCompartmentSelection,
                        model.ProposedFellingDetailsId,
                        model.FellingCompartmentId,
                        model.FellingCompartmentName,
                        model.FellingOperationType,
                        model.CompartmentModelOfInterest.PropertyProfileId
                    });
                case true:
                    return RedirectToAction(nameof(HomeController.Error), "Home");
            }

            model.CompartmentModelOfInterest.Id = newCompartmentId;
        }

        //Can have negative ESRI planar areas out of the geometry service - when the polygon has been drawn anti-clockwise...
        if (model.CompartmentModelOfInterest.TotalHectares < 0)
        {
            model.CompartmentModelOfInterest.TotalHectares *= -1;
        }

        var (hasValue, compartment) = await FindCompartmentAsync(model.CompartmentModelOfInterest.Id, cancellationToken);

        if (!hasValue)
        {
            _logger.LogWarning("Did not find a compartment having id of {Id}", model.CompartmentModelOfInterest.Id);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        compartment.GISData = model.CompartmentModelOfInterest.GISData;
        compartment.TotalHectares = model.CompartmentModelOfInterest.TotalHectares;

        var updateResult = await EditCompartmentAsync(compartment, cancellationToken);

        if (updateResult.IsFailure)
        {
            _logger.LogWarning("Unable to update compartment having id of {CompartmentId}", compartment.Id);
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        _logger.LogDebug("Compartment having id of {CompartmentId} was successfully updated",compartment.Id);
        return model.ApplicationId.GetValueOrDefault() == Guid.Empty ?
            RedirectToAction(nameof(PropertyProfileController.Edit), "PropertyProfile", new { id = compartment.PropertyProfileId, woodlandOwnerid = model.CompartmentModelOfInterest.WoodlandOwnerId, agencyId = model.CompartmentModelOfInterest.AgencyId  })
            : RedirectToAction(nameof(FellingLicenceApplicationController.SelectNewCompartment), "FellingLicenceApplication", new 
            {
                applicationId = model.ApplicationId , 
                newCompartmentId = compartment.Id,
                proposedFellingDetailsId = model.ProposedFellingDetailsId,
                isForRestockingCompartmentSelection = model.IsForRestockingCompartmentSelection,
                fellingOperationType = model.FellingOperationType,
                fellingCompartmentName = model.FellingCompartmentName,
                fellingCompartmentId = model.FellingCompartmentId
            });
    }

    [HttpGet]
    public async Task<ActionResult> Draw(Guid? id, 
        Guid? applicationId,
        bool isForRestockingCompartmentSelection,
        Guid? proposedFellingDetailsId,
        Guid? fellingCompartmentId,
        string? fellingCompartmentName,
        FellingOperationType? fellingOperationType,
        Guid woodlandOwnerId,
        Guid? agencyId,
        Guid propertyProfileId,
        [FromServices] ManagePropertyProfileUseCase managePropertyProfileUseCase,
        CancellationToken cancellationToken)
    {
        bool hasValue;
        CompartmentModel? existingCompartment = null;

        if (id.HasValue)
        {
            (hasValue, existingCompartment) = await FindCompartmentAsync(id.Value, cancellationToken);

            if (!hasValue)
            {
                _logger.LogWarning("Did not find a compartment having id of {Id} for current user..", id);
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        var allPropertyCompartments = await managePropertyProfileUseCase.RetrievePropertyProfileCompartments(propertyProfileId,
            new ExternalApplicant(User), cancellationToken);

        var idToExclude = id.HasValue ? id.Value : Guid.Empty;

        var otherCompartmentsGisData = allPropertyCompartments.Value.Compartments.Where(x => !string.IsNullOrWhiteSpace(x.GISData) && x.Id != idToExclude)
        .Select(o => new { o.DisplayName, o.GISData });

        if (existingCompartment == null)
        {
            existingCompartment = new CompartmentModel
            {
                PropertyProfileId = propertyProfileId,
                PropertyProfileName = allPropertyCompartments.Value.Name
            };
        }

        existingCompartment.WoodlandOwnerId = woodlandOwnerId;
        existingCompartment.AgencyId = agencyId;

        var model = new CompartmentDrawModel
        {
            NearestTown = allPropertyCompartments.Value.NearestTown,
            CompartmentModelOfInterest = existingCompartment,
            AllOtherPropertyCompartmentJson = JsonConvert.SerializeObject(otherCompartmentsGisData),
            ApplicationId = applicationId,
            IsForRestockingCompartmentSelection = isForRestockingCompartmentSelection,
            FellingOperationType = fellingOperationType,
            FellingCompartmentId = fellingCompartmentId,
            FellingCompartmentName = fellingCompartmentName,
            ProposedFellingDetailsId = proposedFellingDetailsId
        };

        SetBreadcrumbs(model);

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = allPropertyCompartments.Value.WoodlandOwnerId;

        return View(model);
    }

    [HttpGet]
    public ActionResult ChooseCreationMethod(
        Guid? compartmentId,
        Guid propertyProfileId,
        Guid? applicationId,
        Guid woodlandOwnerId,
        bool isForRestockingCompartmentSelection,
        string? fellingCompartmentName,
        Guid? fellingCompartmentId,
        Guid? proposedFellingDetailsId,
        FellingOperationType? fellingOperationType, 
        Guid? agencyId,
        CancellationToken cancellationToken)
    {
        var model = new CompartmentCreationMethodModel()
        {
            CompartmentId = compartmentId,
            PropertyProfileId = propertyProfileId,
            ApplicationId = applicationId,
            WoodlandOwnerId = woodlandOwnerId,
            IsForRestockingCompartmentSelection = isForRestockingCompartmentSelection,
            FellingCompartmentId = fellingCompartmentId,
            FellingCompartmentName = fellingCompartmentName,
            ProposedFellingDetailsId = proposedFellingDetailsId,
            FellingOperationType = fellingOperationType,
            AgencyId = agencyId,
            CreationMethod = CreationMethod.UseMap
        };

        return View(model);
    }

    [HttpPost]
    public ActionResult ChooseCreationMethod(CompartmentCreationMethodModel model, CancellationToken cancellationToken)
    {

        switch (model.CreationMethod)
        {
            case CreationMethod.UploadShapefiles:
                return RedirectToAction(nameof(BulkImport), new 
                { 
                    propertyProfileId = model.PropertyProfileId, 
                    applicationId = model.ApplicationId, 
                    woodlandOwnerId = model.WoodlandOwnerId,
                    agencyId = model.AgencyId
                });
            case CreationMethod.UploadAMap:
                return RedirectToAction(nameof(UploadMap), new { applicationId = model.ApplicationId });
            default:
                return RedirectToAction(nameof(CreateUsingMap), new
                {
                    compartmentId = model.CompartmentId,
                    propertyProfileId = model.PropertyProfileId,
                    applicationId = model.ApplicationId,
                    woodlandOwnerId = model.WoodlandOwnerId,
                    agencyId = model.AgencyId,
                    isForRestockingCompartmentSelection = model.IsForRestockingCompartmentSelection,
                    fellingCompartmentId = model.FellingCompartmentId,
                    fellingCompartmentName = model.FellingCompartmentName,
                    proposedFellingDetailsId = model.ProposedFellingDetailsId,
                    fellingOperationType = model.FellingOperationType
                });
                
        }
    }

    [HttpGet]
    public async Task<IActionResult> UploadMap(
        Guid applicationId,
        [FromServices] CreateFellingLicenceApplicationUseCase createFellingLicenceApplicationUseCase,
        CancellationToken cancellationToken
        )
    {
        var user = new ExternalApplicant(User);
        var result = await createFellingLicenceApplicationUseCase.RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (result.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        var model = new UploadMapModel
        {
            ApplicationId = applicationId,
            DocumentCount = result.Value.SupportingDocumentation.Documents.Count()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UploadMap(UploadMapModel model,
        FormFileCollection imageFiles,
        [FromServices] AddSupportingDocumentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!imageFiles.Any())
            return RedirectToAction(nameof(UploadMap), new { id = model.ApplicationId });

        var user = new ExternalApplicant(User);

        var documentModel = new AddSupportingDocumentModel
        {
            AvailableToConsultees = true,
            DocumentCount = imageFiles.Count,
            FellingLicenceApplicationId = model.ApplicationId
        };

        var saveDocumentsResult = await useCase.AddDocumentsToApplicationAsync(
            user,
            documentModel,
            imageFiles,
            ModelState,
            cancellationToken);

        if (saveDocumentsResult.IsSuccess && ModelState.IsValid)
            return RedirectToAction(nameof(FellingLicenceApplicationController.SelectCompartments), "FellingLicenceApplication", new { applicationId = model.ApplicationId });

        // save was unsuccessful

        return RedirectToAction(nameof(UploadMap), new { id = model.ApplicationId });
    }

    private ActionResult CreateViewWithBreadcrumbs(CompartmentModel model, string viewName)
    {
        SetBreadcrumbs(model);
        return View(viewName, model);
    }
    
    private async Task<Maybe<CompartmentModel>> FindCompartmentAsync(Guid compartmentId, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        return await _manageGeographicCompartmentUseCase.RetrieveCompartmentAsync(compartmentId, user, cancellationToken);
    }

    private async Task<UnitResult<ErrorDetails>> EditCompartmentAsync(CompartmentModel compartment, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        return await _manageGeographicCompartmentUseCase.EditCompartmentAsync(compartment, user, cancellationToken);
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

    private void SetBreadcrumbs(CompartmentModel model)
    {
        List<BreadCrumb> breadCrumbs = DetermineDefaultBreadCrumbs();
        breadCrumbs.Add(new BreadCrumb(model.PropertyProfileName, "PropertyProfile", "Index",
            model.PropertyProfileId.ToString()));

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = model.Id.Equals(Guid.Empty) ? "Create Compartment" : string.Concat( "Compartment ", model.DisplayName)
        };
    }

    [HttpGet]
    public ActionResult Help()
    {
        return View();
    }

    private void SetBreadcrumbs(CompartmentModel model, string customPage)
    {
        List<BreadCrumb> breadCrumbs = DetermineDefaultBreadCrumbs();
        breadCrumbs.Add(new BreadCrumb(model.PropertyProfileName, "PropertyProfile", "Index",
            model.PropertyProfileId.ToString()));

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = customPage
        };
    }

    private void SetBreadcrumbs(CompartmentDrawModel model)
    {
        List<BreadCrumb> breadCrumbs = DetermineDefaultBreadCrumbs();
        breadCrumbs.Add(new BreadCrumb(model.CompartmentModelOfInterest.PropertyProfileName, "PropertyProfile", "Index",
            model.CompartmentModelOfInterest.PropertyProfileId.ToString()));

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = string.IsNullOrWhiteSpace(model.CompartmentModelOfInterest.CompartmentNumber) 
                ? "Compartment" 
                : string.Concat("Compartment ", model.CompartmentModelOfInterest.DisplayName)
        };
    }
}