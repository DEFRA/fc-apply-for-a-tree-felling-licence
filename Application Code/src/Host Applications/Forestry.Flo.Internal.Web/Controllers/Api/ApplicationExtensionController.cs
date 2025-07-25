﻿using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure.Display;

namespace Forestry.Flo.Internal.Web.Controllers.Api;

/// <summary>
/// Controller class acting as the endpoint for routinely extending applications.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[RequiresValidApiKey]
public class ApplicationExtensionController : ControllerBase
{
    private string GetFellingLicenceUrlLink() => Url.AbsoluteAction("ApplicationSummary", "FellingLicenceApplication")!;

    [Route("ExtendApplications")]
    public async Task<IActionResult> ExtendApplicationFinalActionDates(
        [FromServices] ExtendApplicationsUseCase extendApplications,
        CancellationToken cancellationToken)
    {
        await extendApplications.ExtendApplicationFinalActionDatesAsync(GetFellingLicenceUrlLink(), cancellationToken);
        return Ok();
    }
}