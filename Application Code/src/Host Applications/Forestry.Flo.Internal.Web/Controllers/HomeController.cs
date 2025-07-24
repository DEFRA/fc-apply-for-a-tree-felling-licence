using Forestry.Flo.Internal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

using FellingLicenceStatus = Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus;

namespace Forestry.Flo.Internal.Web.Controllers;

public class HomeController : Controller
{
    private readonly FellingLicenceApplicationUseCase _fellingLicenceApplicationUseCase;
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(FellingLicenceApplicationUseCase fellingLicenceApplicationUseCase, IUserAccountService userAccountService, ILogger<HomeController> logger)
    {
        _fellingLicenceApplicationUseCase = fellingLicenceApplicationUseCase;
        _userAccountService = userAccountService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        bool assignedToUserOnly,
        FellingLicenceStatus[] fellingLicenceStatusArray,
        CancellationToken cancellationToken)
    {
        HomePageModel homePageModel = new HomePageModel();

        var internalUser = new InternalUser(User);

        if (internalUser.UserAccountId != null)
        {
            var listUserAssignedFellingLicenceApplicationsModelResult = await _fellingLicenceApplicationUseCase.GetFellingLicenceApplicationAssignmentListModelAsync(
                assignedToUserOnly,
                internalUser.UserAccountId.Value,
                fellingLicenceStatusArray.ToList(),
                cancellationToken);

            if (listUserAssignedFellingLicenceApplicationsModelResult.IsFailure)
            {
                return RedirectToAction("Error", "Home");
            }

            var fellingLicenceApplicationAssignmentListModel = listUserAssignedFellingLicenceApplicationsModelResult.Value;

            var signedInUserAccount = await _userAccountService.GetUserAccountAsync(internalUser.UserAccountId!.Value, cancellationToken);

            if (signedInUserAccount.HasNoValue)
            {
                _logger.LogError("Could not retrieve current user account from user service");

                return RedirectToAction("Error", "Home");
            }

            var signedInUserRoles = RolesService.RolesListFromString(signedInUserAccount.Value.Roles);

            homePageModel.FellingLicenceApplicationAssignmentListModel = fellingLicenceApplicationAssignmentListModel;
            homePageModel.SignedInUserRoles = signedInUserRoles;
        }

        return View(homePageModel);
    }

    private void SetBreadcrumbs(PageWithBreadcrumbsViewModel model, string currentPage)
    {
        var breadCrumbsRoot = new List<BreadCrumb>
        {
            new("Home", "Home", "Index", null)
        };

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbsRoot,
            CurrentPage = currentPage
        };
    }

    public IActionResult Login()
    {
        return View();
    }


    [Authorize]
    public IActionResult SignIn()
    {
        return RedirectToAction("Index");
    }

    public async Task Logout()
    {
        await HttpContext.SignOutAsync();
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        HttpContext.Response.Headers.Add("Clear-Site-Data", "\"cookies\", \"storage\", \"cache\"");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public async Task<IActionResult> AccountError()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}