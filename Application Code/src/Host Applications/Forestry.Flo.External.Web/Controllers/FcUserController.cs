using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FcUser;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.FcUser;
using Forestry.Flo.Services.Applicants.Entities;
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
        [FromQuery] string? searchTerm,
        [FromQuery] string sortColumn = nameof(Applicant.Name),
        [FromQuery] bool sortAscending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var user = new ExternalApplicant(User);

        _logger.LogDebug(
            "Call made by user having account id {userId} and name of {userName} to view Fc User homepage", 
            user.UserAccountId, user.EmailAddress);

        var searchModel = new FcUserHomePageSearchAndSortModel
        {
            SearchTerm = searchTerm,
            SortColumn = sortColumn,
            SortAscending = sortAscending,
            PageNumber = pageNumber < 1 ? 1 : pageNumber,
            PageSize = pageSize < 1 ? 10 : pageSize
        };

        //view model
        var result = await useCase.ExecuteAsync(user, searchModel, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Unable to successfully execute use case required to build Fc user Dashboard, error : {error}", result.Error);
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }
        
        return View(result.Value);
    }

    [HttpPost]
    public IActionResult Search(FcUserHomePageViewModel viewModel)
    {
        return RedirectToAction(nameof(Index), new
        {
            searchTerm = viewModel.SearchAndSortModel.SearchTerm,
            sortColumn = viewModel.SearchAndSortModel.SortColumn,
            sortAscending = viewModel.SearchAndSortModel.SortAscending,
            pageNumber = 1, //reset to first page on new search
            pageSize = viewModel.SearchAndSortModel.PageSize
        });
    }
}
