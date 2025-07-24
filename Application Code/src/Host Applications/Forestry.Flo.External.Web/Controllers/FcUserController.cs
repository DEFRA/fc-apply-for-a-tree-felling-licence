using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.FcUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize(Policy = AuthorizationPolicyConstants.FcUserPolicyName), RequireCompletedRegistration, AutoValidateAntiforgeryToken]
public class FcUserController : Controller
{
    private readonly ILogger<FcUserController> _logger;

    public FcUserController(ILogger<FcUserController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] GetDataForFcUserHomepageUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        _logger.LogDebug("Call made by user having account id {userId} and name of {userName} to view Fc User homepage", 
            user.UserAccountId, user.EmailAddress);

        //view model
        var result = await useCase.ExecuteAsync(user, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Unable to successfully execute use case required to build Fc user Dashboard, error : {error}", result.Error);
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }
        
        return View(result.Value);
    }
}
