using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers;

[Authorize(Policy = AuthorizationPolicyNameConstants.HasFcAdministratorRole)]
[AutoValidateAntiforgeryToken]
public class AdminAccountController : Controller
{
    private readonly List<BreadCrumb> _breadCrumbsRoot = new()
    {
        new ("Home", "Home", "Index", null)
    };

    /// <summary>
    /// List user accounts which have not yet been confirmed or denied 
    /// </summary>
    /// <param name="userAccountService">A service providing access to accounts.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    public async Task<IActionResult> UnconfirmedUserAccounts(
        [FromServices] IUserAccountService userAccountService,
        CancellationToken cancellationToken)
    {
        var unconfirmedUserAccounts = await userAccountService.ListNonConfirmedUserAccountsAsync(cancellationToken);

        var unconfirmedUserAccountModels = ModelMapping.ToUserAccountModels(unconfirmedUserAccounts);

        ViewBag.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "Unconfirmed User Accounts"
        };

        return View(unconfirmedUserAccountModels);
    }

    /// <summary>
    /// Review the unconfirmed user account
    /// </summary>
    /// <param name="userAccountId">The user account identifier.</param>
    /// <param name="userAccountService">A service providing access to accounts.</param>
    /// <param name="azureAdService">A service providing access to AD accounts.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    public async Task<IActionResult> ReviewUnconfirmedUserAccount(
        Guid userAccountId,
        [FromServices] IUserAccountService userAccountService,
        [FromServices] IAzureAdService azureAdService,
        CancellationToken cancellationToken)
    {
        var unconfirmedUserAccount = await userAccountService.GetUserAccountAsync(userAccountId, cancellationToken);
        if (unconfirmedUserAccount.HasNoValue)
        {
            //todo logging of error
            return RedirectToAction("Error", "Home");
        }

        switch (unconfirmedUserAccount.Value.Status)
        {
            case Status.Closed:
                this.AddErrorMessage("This account has been closed.  This action cannot be undone.");
                break;
            case Status.Confirmed:
                this.AddUserGuide("This new account request has already been confirmed", "You may optionally select Deny if this was done in error");
                break;
            case Status.Denied:
                this.AddUserGuide("This account request has already been denied", "You may optionally select Approve if this was done in error.");
                break;
            case Status.Requested:
                break;
        }
        
        var unconfirmedUserAccountModel = ModelMapping.ToUserAccountModel(unconfirmedUserAccount.Value);

        ViewBag.UserIsInActiveDirectory = await azureAdService.UserIsInDirectoryAsync(unconfirmedUserAccountModel.Email!, cancellationToken);

        _breadCrumbsRoot.Add(new BreadCrumb("Unconfirmed User Accounts", "AdminAccount", "UnconfirmedUserAccounts", null));

        ViewBag.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "Confirm User Account"
        };

        return View(unconfirmedUserAccountModel);
    }

    /// <summary>
    /// Sets the status of an internal user account to confirmed.
    /// </summary>
    /// <param name="id">The identifier for the user account.</param>
    /// <param name="userAccountUseCase">A service for interacting with internal user accounts.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    [HttpPost]
    public async Task<IActionResult> ConfirmUserAccount(
        Guid id,
        bool canApproveApplications,
        [FromServices] IRegisterUserAccountUseCase userAccountUseCase,
        CancellationToken cancellationToken)
    {
        var loginUrl = Url.AbsoluteAction("Index", "Home")!;
        var internalUser = new InternalUser(User);
        var result = await userAccountUseCase.UpdateUserAccountStatusAsync(
            internalUser,
            id, 
            Status.Confirmed, 
            canApproveApplications,
            loginUrl, 
            cancellationToken);

        if (result.IsSuccess)
        {
            this.AddConfirmationMessage("User account confirmation has been updated as confirmed", "An accompanying account in the external applicant site is also being created for this user.");
        }

        return RedirectToAction("UnconfirmedUserAccounts");
    }

    /// <summary>
    /// Sets the status of an internal user account to denied.
    /// </summary>
    /// <param name="id">The identifier for the user account.</param>
    /// <param name="userAccountUseCase">A service for interacting with internal user accounts.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    [HttpPost]
    public async Task<IActionResult> DenyUserAccount(
        Guid id,
        [FromServices] IRegisterUserAccountUseCase userAccountUseCase,
        CancellationToken cancellationToken)
    {
        var loginUrl = Url.AbsoluteAction("Index", "Home")!;
        var internalUser = new InternalUser(User);
        var result = await userAccountUseCase.UpdateUserAccountStatusAsync(
            internalUser,
            id,
            Status.Denied,
            null,
            loginUrl,
            cancellationToken);

        if (result.IsSuccess)
        {
            this.AddConfirmationMessage("User account confirmation has been updated as denied");
        }

        return RedirectToAction("UnconfirmedUserAccounts");
    }
}