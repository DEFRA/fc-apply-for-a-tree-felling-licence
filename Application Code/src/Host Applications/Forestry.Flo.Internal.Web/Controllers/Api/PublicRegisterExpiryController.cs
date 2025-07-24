using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure.Display;

namespace Forestry.Flo.Internal.Web.Controllers.Api;

/// <summary>
/// Controller class acting as the endpoint for routinely notifying assigned FC staff of applications' public registry periods expiring.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[RequiresValidApiKey]
public class PublicRegisterExpiryController : ControllerBase
{
    private string GetFellingLicenceUrlLink() => Url.AbsoluteAction("ApplicationSummary", "FellingLicenceApplication")!;

    [Route("PublicRegisterExpiryNotification")]
    public async Task<IActionResult> RemoveApplicationsFromConsultationPublicRegisterWhenEndDateReached(
        [FromServices] PublicRegisterExpiryUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(
            GetFellingLicenceUrlLink(),
            cancellationToken);

        return Ok();
    }

    //Removes the application from the Decision public register once the end date is reached.
    [Route("RemoveApplicationsFromDecisionPublicRegisterWhenEndDateReached")]
    public async Task<IActionResult> RemoveApplicationsFromDecisionPublicRegisterWhenEndDateReached(
        [FromServices] RemoveApplicationsFromDecisionPublicRegisterUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(
            GetFellingLicenceUrlLink(),
            cancellationToken);

        return Ok();
    }
}