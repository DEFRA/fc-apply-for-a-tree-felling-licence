using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.Home;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.User;
using GovUk.OneLogin.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Microsoft.Extensions.Options;
using AuthenticationOptions = Forestry.Flo.Services.Common.Infrastructure.AuthenticationOptions;

namespace Forestry.Flo.External.Web.Controllers;

public partial class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AuthenticationOptions _authenticationOptions;

    public HomeController(
        ILogger<HomeController> logger,
        IOptions<AuthenticationOptions> authOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions.Value);

        _logger = logger ?? new NullLogger<HomeController>();;
        _authenticationOptions = authOptions.Value;
    }
    
    // GET Home/AcceptInvitation?email=some@email.com&token=sometoken
    [HttpGet]
    public async Task<IActionResult> AcceptInvitation([FromQuery] string email, [FromQuery] string token,
        [FromServices] InviteWoodlandOwnerToOrganisationUseCase useCase, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (user.IsLoggedIn && user.HasCompletedAccountRegistration)
        {
            return RedirectToAction(nameof(WoodlandOwner));
        }

        var tokenCheckResult = await  useCase.VerifyInvitedUserAccountAsync(email, token, cancellationToken);
        if (!tokenCheckResult.IsSuccess)
        {
            this.AddErrorMessage(tokenCheckResult.Error);
            return View();
        }

        return View("AcceptInvitationConfirmed",  tokenCheckResult.Value);
    }
    
    // GET Home/AcceptAgentInvitation?email=some@email.com&token=sometoken
    [HttpGet]
    public async Task<IActionResult> AcceptAgentInvitation([FromQuery] string email, [FromQuery] string token,
        [FromServices] InviteAgentToOrganisationUseCase useCase, CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (user.IsLoggedIn && user.HasCompletedAccountRegistration)
        {
            return RedirectToAction(nameof(WoodlandOwner));
        }

        var tokenCheckResult = await  useCase.VerifyInvitedUserAccountAsync(email, token, cancellationToken);
        if (!tokenCheckResult.IsSuccess)
        {
            this.AddErrorMessage(tokenCheckResult.Error);
            return View("AcceptInvitation");
        }

        return View("AcceptInvitationConfirmed",  tokenCheckResult.Value);
    }
      
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult CustomSignUp()
    {
        return View();
    }

    public IActionResult ForgottenPassword()
    {
        return View();
    }

    public IActionResult SignIn()
    {
        var scheme = _authenticationOptions.Provider switch
        {
            AuthenticationProvider.Azure => FloAuthenticationScheme.SignIn,
            AuthenticationProvider.OneLogin => OneLoginDefaults.AuthenticationScheme,
            _ => throw new ArgumentOutOfRangeException()
        };

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Index)),
        }, scheme);
    }

    public IActionResult SignUp()
    {
        var scheme = _authenticationOptions.Provider switch
        {
            AuthenticationProvider.Azure => FloAuthenticationScheme.SignUp,
            AuthenticationProvider.OneLogin => OneLoginDefaults.AuthenticationScheme,
            _ => throw new ArgumentOutOfRangeException()
        };

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Index)),
        }, scheme);
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
                await HttpContext.SignOutAsync("SignIn");
                await HttpContext.SignOutAsync("SignUp");
                HttpContext.Response.Headers.Add("Clear-Site-Data", "\"cookies\", \"storage\", \"cache\"");
                return SignOut();
            default:
                return SignOut();
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult TermsAndConditions()
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

    [Authorize, RequireCompletedRegistration]
    public async Task<IActionResult> WoodlandOwner(
        Guid woodlandOwnerId,
        [FromServices] WoodlandOwnerHomePageUseCase useCase,
        [FromServices] CreateFellingLicenceApplicationUseCase applicationUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var fellingLicenceApplications = await applicationUseCase
            .GetWoodlandOwnerApplicationsAsync(woodlandOwnerId, user, cancellationToken)
            .ConfigureAwait(false);

        //Access failure
        if (fellingLicenceApplications.IsFailure)
        {
            return RedirectToAction(nameof(Error), "Home");
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = woodlandOwnerId;

        var viewModel = new WoodlandOwnerHomePageModel(fellingLicenceApplications.Value.ToList(), woodlandOwnerId);

        return View(viewModel);
    }

    [Authorize, RequireCompletedRegistration, UserIsAgentOrAgentAdministrator]
    public async Task<IActionResult> AgentUser(
        Guid agencyId,
        [FromServices] AgentUserHomePageUseCase agentUserHomePageUseCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (user.AccountType != AccountTypeExternal.FcUser && user.AgencyId != agencyId.ToString())
        {
            return RedirectToAction("Index");
        }

        var availableWoodlandOwners = await agentUserHomePageUseCase
            .GetWoodlandOwnersForAgencyAsync(user, agencyId, cancellationToken)
            .ConfigureAwait(false);

        var viewModel = new AgentUserHomePageModel(availableWoodlandOwners, agencyId);

        return View(viewModel);
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

    [HttpGet]
    public IActionResult AccountHolderConfirmation()
    {
        return View(new AccountHolderConfirmationPageModel());
    }

    [HttpPost]
    public IActionResult AccountHolderConfirmation(AccountHolderConfirmationPageModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return RedirectToAction(model.IsAccountHolder!.Value 
            ? nameof(SignIn) 
            : nameof(AccountJustification));
    }

    [HttpGet]
    public IActionResult AccountJustification()
    {
        return View();
    }

    [HttpGet]
    public IActionResult AboutTheNewService()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ServiceContactDetails() => View();
}