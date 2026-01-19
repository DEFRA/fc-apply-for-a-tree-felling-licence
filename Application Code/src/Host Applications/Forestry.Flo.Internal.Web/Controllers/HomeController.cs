using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.InternalUsers.Services;
using GovUk.OneLogin.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using AuthenticationOptions = Forestry.Flo.Services.Common.Infrastructure.AuthenticationOptions;
using FellingLicenceStatus = Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus;

namespace Forestry.Flo.Internal.Web.Controllers;

public class HomeController : Controller
{
    private readonly IFellingLicenceApplicationUseCase _fellingLicenceApplicationUseCase;
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<HomeController> _logger;
    private readonly List<BreadCrumb> _breadCrumbsRoot = new()
    {
        new ("Open applications", "Home", "Index", null)
    };

    // Pagination & sorting input constraints
    private const int MaxPageSize = 100;
    private static readonly HashSet<string> AllowedSortColumns = new(
        new[] { "Status", "Reference", "Property", "SubmittedDate", "CitizensCharterDate", "FinalActionDate" },
        StringComparer.OrdinalIgnoreCase);

    public HomeController(IFellingLicenceApplicationUseCase fellingLicenceApplicationUseCase, IUserAccountService userAccountService, ILogger<HomeController> logger)
    {
        _fellingLicenceApplicationUseCase = fellingLicenceApplicationUseCase;
        _userAccountService = userAccountService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        bool assignedToUserOnly,
        FellingLicenceStatus[] fellingLicenceStatusArray,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 12,
        string column = "FinalActionDate",
        string dir = "asc",
        string? search = null)
    {
        // Sanitize inputs
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 1 : Math.Min(pageSize, MaxPageSize);
        var safeColumn = AllowedSortColumns.Contains(column) ? column : "FinalActionDate";
        var safeDir = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        var safeSearch = string.IsNullOrWhiteSpace(search) ? null : search!.Trim();

        HomePageModel homePageModel = new HomePageModel();

        var internalUser = new InternalUser(User);

        if (internalUser.UserAccountId != null)
        {
            var listUserAssignedFellingLicenceApplicationsModelResult = await _fellingLicenceApplicationUseCase.GetFellingLicenceApplicationAssignmentListModelAsync(
                assignedToUserOnly,
                internalUser.UserAccountId.Value,
                (fellingLicenceStatusArray ?? Array.Empty<FellingLicenceStatus>()).ToList(),
                cancellationToken,
                pageNumber: safePage,
                pageSize: safePageSize,
                sortColumn: safeColumn,
                sortDirection: safeDir,
                searchText: safeSearch);

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

    public IActionResult Login()
    {
        return View();
    }


    [Authorize]
    public IActionResult SignIn()
    {
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Logout([FromServices] IOptions<AuthenticationOptions> options)
    {
        switch (options.Value.Provider)
        {
            case AuthenticationProvider.OneLogin:
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return SignOut(OneLoginDefaults.AuthenticationScheme);
            case AuthenticationProvider.Azure:
                await HttpContext.SignOutAsync();
                await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                HttpContext.Response.Headers.Add("Clear-Site-Data", "\"cookies\", \"storage\", \"cache\"");
                return SignOut();
            default:
                return SignOut();
        }
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

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Accessibility()
    {
        return View();
    }

    public IActionResult Cookies()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public async Task<IActionResult> UserManagement(CancellationToken cancellationToken)
    {
        var internalUser = new InternalUser(User);

        if (internalUser.UserAccountId != null)
        {

            var signedInUserAccount = await _userAccountService.GetUserAccountAsync(internalUser.UserAccountId!.Value, cancellationToken);

            if (signedInUserAccount.HasNoValue)
            {
                _logger.LogError("Could not retrieve current user account from user service");

                return RedirectToAction("Error", "Home");
            }

            var signedInUserRoles = RolesService.RolesListFromString(signedInUserAccount.Value.Roles);

            ViewData["SignedInUserRoles"]  = signedInUserRoles;
        }


        ViewBag.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = "User management"
        };
        return View();
    }
}