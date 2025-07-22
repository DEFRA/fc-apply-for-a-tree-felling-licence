using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

public partial class HomeController
{
    // landing page
    public IActionResult Index()
    {
        var user = new ExternalApplicant(User);

        if (user.IsNotLoggedIn)
        {
            return View();
        }

        if (user.IsDeactivatedAccount)
        {
            return RedirectToAction(nameof(AccountError));
        }

        if (user.HasCompletedAccountRegistration is false)
        {
            return RedirectToAction(nameof(AccountController.RegisterAccountType), "Account");
        }

        return user.AccountType switch
        {
            AccountTypeExternal.WoodlandOwner or AccountTypeExternal.WoodlandOwnerAdministrator =>
                RedirectToAction(nameof(WoodlandOwner), new { woodlandOwnerId = user.WoodlandOwnerId }),
            AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator =>
                RedirectToAction(nameof(AgentUser), new { agencyId = user.AgencyId }),
            AccountTypeExternal.FcUser => RedirectToAction(nameof(FcUserController.Index), "FcUser"),
            _ => View()
        };
    }

    public IActionResult WhenYouNeedALicence() => View();
    public IActionResult WhenYouDoNotNeedALicence() => View();
    public IActionResult LicenceConditions() => View();
    public IActionResult ApplyForALicence() => View();
    public IActionResult FellingAndRestockingDefinitions() => View();
}