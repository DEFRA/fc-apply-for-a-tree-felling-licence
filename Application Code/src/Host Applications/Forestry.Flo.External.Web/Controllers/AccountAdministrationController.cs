using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.AccountAdministration;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.AccountAdministration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[TypeFilter(typeof(ApplicationExceptionFilter))]
public class AccountAdministrationController : Controller
{
    private readonly List<BreadCrumb> _breadCrumbsRoot;
    private readonly ValidationProvider _validationProvider;
    private const string ExternalListTitle = "User Account List";
    private const string AmendUserTitle = "Amend User Account";
    public AccountAdministrationController(ValidationProvider validationProvider)
    {
        _validationProvider = Guard.Against.Null(validationProvider);

        _breadCrumbsRoot = new List<BreadCrumb>
        {
            new("Home", "Home", "Index", null),
        };
    }

    [HttpGet]
    public async Task<IActionResult> WoodlandOwnerList(
        [FromServices] ListWoodlandOwnerUsersUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (user.AccountType is not AccountTypeExternal.WoodlandOwnerAdministrator)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var woodlandOwnerId = Guard.Against.NullOrEmpty(user.WoodlandOwnerId);

        var users = await useCase.RetrieveListOfWoodlandOwnerUsersAsync(user, Guid.Parse(woodlandOwnerId), cancellationToken);

        if (users.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        return ExternalUserList(
            users.Value.WoodlandOwnerUsers,
            Url.Action(nameof(HomeController.Index), "Home")!);
    }

    [HttpGet]
    private IActionResult ExternalUserList(
        IEnumerable<UserAccountModel> userAccounts,
        string returnUrl)
    {
        var model = new ExternalUserListModel
        {
            ExternalUserList = userAccounts,
            ReturnUrl = returnUrl
        };

        SetBreadcrumbs(model, ExternalListTitle);

        return View("ExternalUserList", model);
    }

    [HttpPost]
    public IActionResult AmendUserAccount(
        ExternalUserListModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ExternalUserList(model.ExternalUserList!, model.ReturnUrl);
        }

        return RedirectToAction(
            nameof(AmendExternalUserAccount),
            new
            {
                userId = model.SelectedUserAccountId
            });
    }

    [HttpGet]
    public async Task<IActionResult> AmendExternalUserAccount(
        Guid userId,
        [FromServices] AmendExternalUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var externalUser = new ExternalApplicant(User);

        if (externalUser.AccountType is not (AccountTypeExternal.WoodlandOwnerAdministrator or AccountTypeExternal.AgentAdministrator))
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var (_, isFailure, userAccountModel) = await useCase.RetrieveExternalUserAccountAsync(userId, cancellationToken);

        var action = externalUser.AccountType is AccountTypeExternal.WoodlandOwnerAdministrator
            ? "WoodlandOwnerList"
            : "AgentAccountList";

        if (isFailure)
        {
            return RedirectToAction(action);
        }

        if (!VerifyUserAuthority(externalUser, userAccountModel.AccountType))
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        SetBreadcrumbs(userAccountModel, AmendUserTitle);

        userAccountModel.Breadcrumbs!.Breadcrumbs.Add(new BreadCrumb(ExternalListTitle, "AccountAdministration", action, null));

        return View(userAccountModel);
    }

    [HttpPost]
    public async Task<IActionResult> AmendExternalUserAccount(
        AmendExternalUserAccountModel model,
        [FromServices] AmendExternalUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        ApplySectionValidationModelErrors(model, nameof(AmendExternalUserAccountModel));

        var action = model.AccountType is (AccountTypeExternal.WoodlandOwnerAdministrator or AccountTypeExternal.WoodlandOwner)
            ? "WoodlandOwnerList"
            : "AgentAccountList";

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, AmendUserTitle);

            model.Breadcrumbs!.Breadcrumbs.Add(new BreadCrumb(ExternalListTitle, "AccountAdministration", action, null));

            return View(model);
        }

        var externalUser = new ExternalApplicant(User);

        if (!VerifyUserAuthority(externalUser, model.AccountType))
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var result = await useCase.UpdateExternalAccountDetailsAsync(externalUser, model, cancellationToken);

        if (result.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        this.AddConfirmationMessage("User account successfully amended");

        return RedirectToAction(action);
    }

    /// <summary>
    /// Verifies the logged in user is authorised to amend a user account.
    /// </summary>
    /// <param name="externalUser">The logged in <see cref="ExternalApplicant"/>.</param>
    /// <param name="userType">The <see cref="AccountTypeExternal"/> of the user to amend.</param>
    /// <returns>A bool indicating the user is authorised to amend the account.</returns>
    private bool VerifyUserAuthority(ExternalApplicant externalUser, AccountTypeExternal userType)
    {
        var woodlandOwnerAuthority = 
            externalUser.AccountType is AccountTypeExternal.AgentAdministrator 
            && userType is AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator;

        var agentAuthority = 
            externalUser.AccountType is AccountTypeExternal.WoodlandOwnerAdministrator 
            && userType is AccountTypeExternal.WoodlandOwner or AccountTypeExternal.WoodlandOwnerAdministrator;

        return woodlandOwnerAuthority || agentAuthority;
    }

    private void ApplySectionValidationModelErrors(AmendExternalUserAccountModel model, string modelPart)
    {
        var sectionValidationErrors = _validationProvider.ValidateAmendUserAccountSection(model, modelPart);

        if (!sectionValidationErrors.Any()) return;

        foreach (var validationFailure in sectionValidationErrors)
        {
            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }
    
    private void SetBreadcrumbs(PageWithBreadcrumbsViewModel model, string currentPage)
    {
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = currentPage
        };
    }
}