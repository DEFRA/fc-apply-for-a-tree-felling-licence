using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.AgentAuthority;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[TypeFilter(typeof(ApplicationExceptionFilter))]
[UserIsInRoleMultiple(new[] { AccountTypeExternal.FcUser, AccountTypeExternal.Agent, AccountTypeExternal.AgentAdministrator, AccountTypeExternal.WoodlandOwnerAdministrator })]
[RequireCompletedRegistration]
public class WoodlandOwnerController : Controller
{
    private readonly List<BreadCrumb> _breadCrumbsRoot;
    private readonly IValidator<ManageWoodlandOwnerDetailsModel> _validator;

    public WoodlandOwnerController(
        IValidator<ManageWoodlandOwnerDetailsModel> validator)
    {
        _validator = Guard.Against.Null(validator);

        _breadCrumbsRoot = new List<BreadCrumb>
        {
            new("Home", "Home", "Index", null)
        };
    }

    [HttpGet]
    public async Task<IActionResult> ManagedClientSummary(
        Guid woodlandOwnerId,
        Guid? agencyId,
        [FromServices] ManageWoodlandOwnerDetailsUseCase useCase,
        [FromServices] ManagePropertyProfileUseCase propertyProfileUseCase,
        [FromServices] AgentAuthorityFormUseCase agentAuthorityFormUseCase,
        [FromServices] GetAgentAuthorityFormDocumentsUseCase agentAuthorityFormDocumentsUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var model = new ManagedClientSummaryModel();

        var (_, isFailure, woodlandOwnermodel) = await useCase.GetWoodlandOwnerModelAsync(woodlandOwnerId, user, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        model.ManageWoodlandOwnerDetails = woodlandOwnermodel;

        var propertyProfiles = await propertyProfileUseCase
            .RetrievePropertyProfilesAsync(woodlandOwnerId, user, cancellationToken)
            .ConfigureAwait(false);

        if (propertyProfiles.HasNoValue)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var propertySummary = new List<ManagedPropertySummary>();

        foreach (var propertyProfile in propertyProfiles.Value)
        {
            var compartments = await propertyProfileUseCase.RetrievePropertyProfileCompartments(propertyProfile.Id, user, cancellationToken);

            if (compartments.HasNoValue)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            propertySummary.Add(new ManagedPropertySummary { Name = propertyProfile.Name, PropertyId = propertyProfile.Id, NoOfCompartments = compartments.Value.Compartments.Count() });
        }

        Guid? agencyIdToUse = agencyId.HasValue ? agencyId.Value : (Guid.TryParse(user.AgencyId, out Guid userAgencyId) ? userAgencyId : null);

        if (agencyIdToUse.HasValue)
        {
            model.AgencyId = agencyIdToUse;

            var agentAuthority = await agentAuthorityFormUseCase.GetAgentAuthorityFormsAsync(user, agencyIdToUse.Value, cancellationToken);

            if (agentAuthority.IsSuccess)
            {
                var thisAuthority = agentAuthority.Value.Find(aa => aa.WoodlandOwner.Id == woodlandOwnerId);

                if (thisAuthority != null)
                {
                    model.AgentAuthorityId = thisAuthority.Id;

                    var docModel = await agentAuthorityFormDocumentsUseCase.GetAgentAuthorityFormDocumentsAsync(user, thisAuthority.Id, cancellationToken);

                    if (docModel.IsSuccess)
                    {
                        model.CurrentAgentAuthorityForm = docModel.Value.CurrentAuthorityForm;
                    }
                }
            }
        }

        model.Properties = propertySummary;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ManageWoodlandOwnerDetails(
        Guid id, 
        [FromServices] ManageWoodlandOwnerDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        // confirm logged in user has permission to amend woodland owner
        if (!user.IsFcUser)
        {
            if (user.AccountType is AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator)
            {
                // check agent has authority

                if (user.AgencyId is null)
                {
                    return RedirectToAction(nameof(HomeController.Error), "Home");
                }

                var result = await
                    useCase.VerifyAgentCanManageWoodlandOwnerAsync(
                        id,
                        Guid.Parse(user.AgencyId),
                        cancellationToken);

                if (!result.Value)
                {
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
            }
            else
            {
                if (user.WoodlandOwnerId is null || Guid.Parse(user.WoodlandOwnerId) != id)
                {
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
            }
        }


        var (_, isFailure, model) = await useCase.GetWoodlandOwnerModelAsync(id, user, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        SetBreadcrumbs(model, "Manage Woodland Owner");
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ManageWoodlandOwnerDetails(
        ManageWoodlandOwnerDetailsModel model,
        [FromServices] ManageWoodlandOwnerDetailsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        // confirm logged in user has permission to amend woodland owner

        ApplyValidationModelErrors(model);

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Manage Woodland Owner");

            return View(model);
        }

        if (!await VerifyUserAuthorityAsync(useCase, user, model.Id, cancellationToken))
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var result = await useCase.UpdateWoodlandOwnerEntityAsync(model, user, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        if (user.AccountType is AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator)
        {
            return RedirectToAction(nameof(ManagedClientSummary), "WoodlandOwner", new { woodlandOwnerId = model.Id });
        }
        else
        {
                return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }

    private async Task<bool> VerifyUserAuthorityAsync(
        ManageWoodlandOwnerDetailsUseCase useCase,
        ExternalApplicant user,
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        if (user.AccountType is AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator)
        {
            // check agent has authority

            if (user.AgencyId is null)
            {
                return false;
            }

            var result = await
                useCase.VerifyAgentCanManageWoodlandOwnerAsync(
                    woodlandOwnerId,
                    Guid.Parse(user.AgencyId),
                    cancellationToken);

            if (!result.Value && !user.IsFcUser)
            {
                return false;
            }
        }
        else
        {
            if (user.WoodlandOwnerId is null || Guid.Parse(user.WoodlandOwnerId) != woodlandOwnerId)
            {
                return false;
            }
        }

        return true;
    }

    private void SetBreadcrumbs(PageWithBreadcrumbsViewModel model, string currentPage)
    {
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = currentPage
        };
    }
    private void ApplyValidationModelErrors(ManageWoodlandOwnerDetailsModel model)
    {
        var validationErrors = _validator.Validate(model).Errors;

        if (!validationErrors.Any()) return;

        foreach (var validationFailure in validationErrors)
        {
            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }
}