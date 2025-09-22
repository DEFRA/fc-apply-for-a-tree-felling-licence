using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Internal.Web.Infrastructure;
using CSharpFunctionalExtensions;

namespace Forestry.Flo.Internal.Web.Controllers.Api;

/// <summary>
/// Controller class acting as the endpoint for polling new comments from the Consultation Public Register.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[RequiresValidApiKey]
public class PublicRegisterCommentsController : ControllerBase
{
    [Route("PullNewComments")]
    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PullNewComments(
        [FromServices] PublicRegisterCommentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.GetNewCommentsFromPublicRegisterAsync(cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        // Return a 500 with custom error text from the use case
        return StatusCode(StatusCodes.Status500InternalServerError, result.Error);
    }
}
