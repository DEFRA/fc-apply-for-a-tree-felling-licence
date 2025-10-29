using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.Api;

/// <summary>
/// API controller endpoint for triggering reminder notifications to applicants who have not yet
/// responded to requested amendments and are within the configured reminder window prior to automatic withdrawal.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[RequiresValidApiKey]
public class AmendmentResponseController : ControllerBase
{
    /// <summary>
    /// Sends reminder notifications for applications approaching the amendment response deadline.
    /// </summary>
    [Route("SendLateAmendmentResponseReminders")]
    public async Task<IActionResult> SendLateAmendmentResponseReminders(
        [FromServices] ILateAmendmentResponseWithdrawalUseCase useCase,
        CancellationToken cancellationToken)
    {
        var count = await useCase.SendLateAmendmentResponseRemindersAsync(cancellationToken);
        return Ok(new { remindersSent = count });
    }

    /// <summary>
    /// Withdraws late amendment applications that have not been responded to within the configured
    /// reminder window.
    /// </summary>
    [Route("WithdrawLateAmendmentApplications")]
    public async Task<IActionResult> WithdrawLateAmendmentApplications(
        [FromServices] ILateAmendmentResponseWithdrawalUseCase useCase,
        [FromServices] IWithdrawFellingLicenceService withdrawFellingLicenceService,
        CancellationToken cancellationToken)
    {
        var withdrawn = await useCase.WithdrawLateAmendmentApplicationsAsync(
            withdrawFellingLicenceService,
            cancellationToken);

        return Ok(new { withdrawn });
    }
}
