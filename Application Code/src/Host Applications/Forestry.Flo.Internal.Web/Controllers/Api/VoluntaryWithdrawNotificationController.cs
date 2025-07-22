
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.Api;

/// <summary>
/// Controller class acting as the endpoint for routinely sending notifications for application withdrawal, if the application has been sat with user for more than 14 days and the withdrawal notification was not already sent.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[RequiresValidApiKey]
public class VoluntaryWithdrawNotificationController : ControllerBase
{
    private string GetFellingLicenceUrlLink() => Url.AbsoluteAction("ApplicationSummary", "FellingLicenceApplication")!;

    [Route("VoluntaryWithdrawalNotificationFla")]
    public async Task<IActionResult> SendVoluntaryWithdrawalNotificatons(
        [FromServices] VoluntaryWithdrawalNotificationUseCase sendNotificationForWithdrawnApplications,
        [FromServices] AutomaticWithdrawalNotificationUseCase automaticWithdrawalNotificationUseCase,
        [FromServices] IWithdrawFellingLicenceService _withdrawFellingLicenceService,
        CancellationToken cancellationToken)
    {
        await automaticWithdrawalNotificationUseCase.ProcessApplicationsAsync(GetFellingLicenceUrlLink(), _withdrawFellingLicenceService, cancellationToken);

        await sendNotificationForWithdrawnApplications.SendNotificationForWithdrawalAsync(GetFellingLicenceUrlLink(), cancellationToken);

        return Ok();
    }
}