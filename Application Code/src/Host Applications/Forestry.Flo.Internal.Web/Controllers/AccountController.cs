using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class AccountController : Controller
{
    private readonly ValidationProvider _validationProvider;
    private readonly List<BreadCrumb> _breadCrumbsRoot;

    public AccountController(ValidationProvider validationProvider)
    {
        _validationProvider = validationProvider;

        _breadCrumbsRoot = new List<BreadCrumb>
        {
            new("Home", "Home", "Index", null)
        };
    }

    public async Task<IActionResult> RegisterAccountDetails(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);

        var userAccountModel = await useCase.GetUserAccountModelAsync(internalUser, cancellationToken);

        if (userAccountModel.HasNoValue)
        {
            return RedirectToAction("Index", "Home");
        }

        userAccountModel.Value.DisallowedRoles.Add(AccountTypeInternal.AccountAdministrator);
        userAccountModel.Value.AllowRoleChange = true;
        SetBreadcrumbs(userAccountModel.Value, "Register Account Details");

        return View(userAccountModel.Value);
    }

    [HttpPost]
    public async Task<IActionResult> RegisterAccountDetails(
        UserRegistrationDetailsModel userRegistrationDetailsModel,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        ApplySectionValidationModelErrors(userRegistrationDetailsModel, nameof(UserRegistrationDetailsModel));

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(userRegistrationDetailsModel, "Register Account Details");
            userRegistrationDetailsModel.AllowRoleChange = true;
            userRegistrationDetailsModel.DisallowedRoles.Add(AccountTypeInternal.AccountAdministrator);
            return View(userRegistrationDetailsModel);
        }

        InternalUser internalUser = new InternalUser(User);
        var urlForNotification = Url.AbsoluteAction("ReviewUnconfirmedUserAccount", "AdminAccount");

        await useCase.UpdateAccountRegistrationDetailsAsync(internalUser, userRegistrationDetailsModel, urlForNotification!, cancellationToken);

        SetBreadcrumbs(userRegistrationDetailsModel, "Register Account Details");

        // User is redirected to home where they will see awaiting account confirmation screen

        return RedirectToAction("Index", "Home");
    }


    public IActionResult AccessDenied()
    {
        return View();
    }

    public IActionResult UserAccountTypeNotValid()
    {
        return View();
    }

    public async Task<IActionResult> UserAccountAwaitingConfirmation()
    {
        return View();
    }

    private void SetBreadcrumbs(PageWithBreadcrumbsViewModel model, string currentPage)
    {
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = currentPage
        };
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