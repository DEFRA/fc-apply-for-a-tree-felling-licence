using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using InternalUser = Forestry.Flo.Internal.Web.Services.InternalUser;

namespace Forestry.Flo.Internal.Web.Controllers.AccountAdministration;

[Authorize(Policy = AuthorizationPolicyNameConstants.HasFcAdministratorRole)]
[AutoValidateAntiforgeryToken]
public class AccountAdministrationController : Controller
{
    private readonly IValidationProvider _validationProvider;

    public AccountAdministrationController(IValidationProvider validationProvider)
    {
        _validationProvider = Guard.Against.Null(validationProvider);
    }

    private readonly List<BreadCrumb> _breadCrumbsRoot = new()
    {
        new ("Open applications", "Home", "Index", null)
    };

    [HttpGet]
    public async Task<IActionResult> FcStaffList(
        [FromServices] IGetFcStaffMembersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new InternalUser(User);

        if (user.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var model = 
            await useCase.GetAllFcStaffMembersAsync(
                user,
                Url.Action(nameof(HomeController.UserManagement), "Home")!,
                true,
                cancellationToken);

        if (model.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        model.Value.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "User Account List"
        };

        return View(model.Value);
    }

    [HttpPost]
    public IActionResult CloseInternalUserAccount(
        UpdateUserRegistrationDetailsModel model)
    {
        var user = new InternalUser(User);

        if (model.Id == user.UserAccountId)
        {
            this.AddErrorMessage("You cannot close your own account");
            return RedirectToAction("AmendUserAccount", new { userId = model.Id });
        }

        return RedirectToAction(
            "CloseUserAccount",
            new
            {
                userId = model.Id
            });
    }

    [HttpPost]
    public async Task<IActionResult> AmendInternalUserAccount(
        FcStaffListModel model,
        [FromServices] IGetFcStaffMembersUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await RedirectToStaffList(model, useCase, cancellationToken);
        }

        return RedirectToAction(
            "AmendUserAccount",
            new
            {
                userId = model.SelectedUserAccountId
            });
    }

    public async Task<IActionResult> RedirectToStaffList(
        FcStaffListModel model,
        IGetFcStaffMembersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var reloadModel = await useCase.GetAllFcStaffMembersAsync(
            new InternalUser(User),
            model.ReturnUrl,
            true,
            cancellationToken);

        if (reloadModel.IsFailure)
        {
            return RedirectToAction("FcStaffList");
        }

        reloadModel.Value.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "User Account List"
        };

        return View("FcStaffList", reloadModel.Value);
    }

    [HttpGet]
    public async Task<IActionResult> AmendUserAccount(
        Guid userId,
        [FromServices] IRegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);

        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        var (_, isFailure, userAccountModel) = await useCase.GetUserAccountModelByIdAsync(userId, internalUser, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("FcStaffList");
        }

        userAccountModel.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "Amend User"
        };

        userAccountModel.Breadcrumbs.Breadcrumbs.Add(new BreadCrumb("User Account List", "AccountAdministration", "AmendAccountUserList", null));

        return View(userAccountModel);
    }

    [HttpPost]
    public async Task<IActionResult> AmendUserAccount(
        UpdateUserRegistrationDetailsModel model,
        [FromServices] IRegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        ApplySectionValidationModelErrors(model, nameof(UserRegistrationDetailsModel));

        if (!ModelState.IsValid)
        {
            model.Breadcrumbs = new BreadcrumbsModel
            {
                Breadcrumbs = _breadCrumbsRoot,
                CurrentPage = "Amend User"
            };

            model.Breadcrumbs.Breadcrumbs.Add(new BreadCrumb("User Account List", "AccountAdministration", "AmendAccountUserList", null));

            return View(model);
        }

        var internalUser = new InternalUser(User);

        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        // do not allow account administrators to change their role (ensure at least one account admin always exists)
        if (model.Id == internalUser.UserAccountId)
        {
            model.RequestedAccountType = AccountTypeInternal.AccountAdministrator;
        }

        var result = await useCase.UpdateAccountRegistrationDetailsByIdAsync(internalUser, model, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        this.AddConfirmationMessage("User account successfully amended");

        return RedirectToAction("FcStaffList");
    }

    [HttpGet]
    public async Task<IActionResult> CloseUserAccount(
        Guid userId,
        [FromServices] ICloseFcStaffAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);

        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        if (userId == internalUser.UserAccountId)
        {
            return RedirectToAction("FcStaffList");
        }

        var (_, isFailure, closeAccountModel) = await useCase.RetrieveUserAccountDetailsAsync(userId, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("FcStaffList");
        }

        closeAccountModel.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "Close User Account"
        };

        closeAccountModel.Breadcrumbs.Breadcrumbs.Add(new BreadCrumb("User Account List", "AccountAdministration", "CloseAccountUserList", null));

        return View(closeAccountModel);
    }

    [HttpPost]
    public async Task<IActionResult> CloseUserAccount(
        CloseUserAccountModel model,
        [FromServices] ICloseFcStaffAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);

        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            return RedirectToAction("Index", "Home");
        }

        if (model.AccountToClose.Id == internalUser.UserAccountId)
        {
            return RedirectToAction("FcStaffList");
        }

        var closeAccountResult = await useCase.CloseFcStaffAccountAsync(model.AccountToClose.Id, internalUser, cancellationToken);

        if (closeAccountResult.IsFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        this.AddConfirmationMessage("User account successfully closed");

        return RedirectToAction("FcStaffList");

    }

    private void ApplySectionValidationModelErrors(UserRegistrationDetailsModel userRegistrationDetailsModel, string modelPart)
    {
        var sectionValidationErrors = _validationProvider.ValidateSection(userRegistrationDetailsModel, modelPart, ModelState);

        if (!sectionValidationErrors.Any()) return;

        foreach (var validationFailure in sectionValidationErrors)
        {
            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }
}