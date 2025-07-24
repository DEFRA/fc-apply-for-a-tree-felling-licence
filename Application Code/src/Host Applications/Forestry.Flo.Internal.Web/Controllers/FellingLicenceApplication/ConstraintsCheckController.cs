using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication
{
    public class ConstraintsCheckController : Controller
    {
        [HttpGet]
        [Route("[controller]")]
        public async Task<IActionResult> Run(
            [FromQuery] Guid id,
            [FromServices] RunFcInternalUserConstraintCheckUseCase useCase,
            CancellationToken cancellationToken)
        {
            var user = new InternalUser(User);

            var result = await useCase.ExecuteConstraintsCheckAsync(user, id, cancellationToken);

            if (result.IsSuccess)
                return result.Value;

            return RedirectToAction("Error", "Home");
        }
    }
}
