using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.AccountAdministration;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;

namespace Forestry.Flo.Internal.Web.Controllers.AccountAdministration;

[Authorize(Policy = AuthorizationPolicyNameConstants.HasFcAdministratorRole)]
[AutoValidateAntiforgeryToken]
public class ExternalAccountAdministrationController : Controller
{
    private readonly IValidationProvider _validationProvider;
    private const string ExternalListTitle = "External User Account List";

    public ExternalAccountAdministrationController(IValidationProvider validationProvider)
    {
        _validationProvider = Guard.Against.Null(validationProvider);
    }

    private readonly List<BreadCrumb> _breadCrumbsRoot = new()
    {
        new ("Open applications", "Home", "Index", null)
    };

    [HttpGet]
    public async Task<IActionResult> ExternalUserList(
        [FromServices] IGetApplicantUsersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (user.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = 
            await useCase.RetrieveListOfActiveExternalUsersAsync(
                user,
                Url.Action(nameof(HomeController.UserManagement), "Home")!,
                cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        model.Value.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = ExternalListTitle
        };

        return View(model.Value);
    }


    [HttpPost]
    public async Task<IActionResult> CloseUserAccount(
        ExternalUserListModel model,
        [FromServices] IGetApplicantUsersUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedirectToExternalList(model, useCase, cancellationToken);
        }

        return RedirectToAction(
            "CloseExternalUserAccount",
            new
            {
                userId = model.SelectedUserAccountId
            });
    }

    [HttpPost]
    public async Task<IActionResult> AmendUserAccount(
        ExternalUserListModel model,
        [FromServices] IGetApplicantUsersUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedirectToExternalList(model, useCase, cancellationToken);
        }

        return RedirectToAction(
            "AmendExternalUserAccount",
            new
            {
                userId = model.SelectedUserAccountId
            });
    }
    public async Task<IActionResult> RedirectToExternalList(
        ExternalUserListModel model,
        IGetApplicantUsersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var reloadModel = await useCase.RetrieveListOfActiveExternalUsersAsync(
            new InternalUser(User),
            model.ReturnUrl,
            cancellationToken);

        if (reloadModel.IsFailure)
        {
            return RedirectToAction("ExternalUserList");
        }

        reloadModel.Value.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = ExternalListTitle
        };

        return View("ExternalUserList", reloadModel.Value);
    }

    [HttpGet]
    public async Task<IActionResult> AmendExternalUserAccount(
        Guid userId,
        [FromServices] IAmendExternalUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);

        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        var (_, isFailure, userAccountModel) = await useCase.RetrieveExternalUserAccountAsync(userId, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("ExternalUserList");
        }

        userAccountModel.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "Amend External User Account"
        };

        userAccountModel.Breadcrumbs.Breadcrumbs.Add(new BreadCrumb(ExternalListTitle, "ExternalAccountAdministration", "ExternalUserList", null));

        return View(userAccountModel);
    }

    [HttpPost]
    public async Task<IActionResult> AmendExternalUserAccount(
        AmendExternalUserAccountModel model,
        [FromServices] IAmendExternalUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        ApplySectionValidationModelErrors(model, nameof(AmendExternalUserAccountModel));

        if (!ModelState.IsValid)
        {
            model.Breadcrumbs = new BreadcrumbsModel
            {
                Breadcrumbs = _breadCrumbsRoot,
                CurrentPage = "Amend External User Account"
            };

            model.Breadcrumbs.Breadcrumbs.Add(new BreadCrumb(ExternalListTitle, "ExternalAccountAdministration", "ExternalUserList", null));

            return View(model);
        }

        var internalUser = new InternalUser(User);

        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        var result = await useCase.UpdateExternalAccountDetailsAsync(internalUser, model, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        this.AddConfirmationMessage("User account successfully amended");

        return RedirectToAction("ExternalUserList");
    }


    [HttpGet]
    public async Task<IActionResult> CloseExternalUserAccount(
        Guid userId,
        [FromServices] IAmendExternalUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);

        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator || userId == internalUser.UserAccountId)
        {
            return RedirectToAction("Index", "Home");
        }

        var (_, isFailure, closeAccountModel) = await useCase.RetrieveCloseExternalUserModelAsync(userId, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("ExternalUserList");
        }

        closeAccountModel.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "Close User Account"
        };

        closeAccountModel.Breadcrumbs.Breadcrumbs.Add(new BreadCrumb(ExternalListTitle, "ExternalAccountAdministration", "ExternalUserList", null));

        return View(closeAccountModel);
    }

    [HttpPost]
    public async Task<IActionResult> CloseExternalUserAccount(
        CloseExternalUserModel model,
        [FromServices] IAmendExternalUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);

        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        switch (model.AccountToClose.ExternalUser.AccountType)
        {
            case AccountTypeExternal.AgentAdministrator:
            case AccountTypeExternal.Agent:

                if (model.AccountToClose.AgencyModel is not null)
                {
                    var validateAgentCloseRequest =
                        await useCase.VerifyAgentCanBeClosedAsync(model.AccountToClose.ExternalUser.Id, model.AccountToClose.AgencyModel.AgencyId!.Value, cancellationToken);

                    if (validateAgentCloseRequest.IsFailure)
                    {
                        this.AddErrorMessage(validateAgentCloseRequest.Error);
                        return RedirectToAction("ExternalUserList");
                    }
                }

                break;

            case AccountTypeExternal.WoodlandOwner:

                if (model.AccountToClose.ExternalUser.WoodlandOwnerId is null)
                {
                    return RedirectToAction("Error", "Home");
                }
                
                var validateWoCloseRequest =
                    await useCase.VerifyWoodlandOwnerCanBeClosedAsync(model.AccountToClose.ExternalUser.Id, model.AccountToClose.ExternalUser.WoodlandOwnerId.Value, cancellationToken);

                if (!validateWoCloseRequest)
                {
                    this.AddErrorMessage("Woodland owner user account cannot be closed");
                    return RedirectToAction("ExternalUserList");
                }

                break;
        }

        var closeAccountResult = await useCase.CloseExternalUserAccountAsync(model.AccountToClose.ExternalUser.Id, internalUser, cancellationToken);

        if (closeAccountResult.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        this.AddConfirmationMessage("User account successfully closed");

        return RedirectToAction("ExternalUserList");
    }


    private void ApplySectionValidationModelErrors(AmendExternalUserAccountModel userRegistrationDetailsModel, string modelPart)
    {
        var sectionValidationErrors = _validationProvider.ValidateExternalUserAccountSection(userRegistrationDetailsModel, modelPart, ModelState);

        if (!sectionValidationErrors.Any()) return;

        foreach (var validationFailure in sectionValidationErrors)
        {
            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }
}