
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
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
    [Route("VoluntaryWithdrawalNotificationFla")]
    public async Task<IActionResult> SendVoluntaryWithdrawalNotificatons(
        [FromServices] IVoluntaryWithdrawalNotificationUseCase sendNotificationForWithdrawnApplications,
        [FromServices] IAutomaticWithdrawalNotificationUseCase automaticWithdrawalNotificationUseCase,
        CancellationToken cancellationToken)
    {
        await automaticWithdrawalNotificationUseCase.ProcessApplicationsAsync(cancellationToken);

        await sendNotificationForWithdrawnApplications.SendNotificationForWithdrawalAsync(cancellationToken);

        return Ok();
    }
}