using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public class AgentAuthorityFormController : Controller
{
    private readonly ViewAgentAuthorityFormUseCase _viewAgentAuthorityFormUseCase;

    public AgentAuthorityFormController(ViewAgentAuthorityFormUseCase viewAgentAuthorityFormUseCase)
    {
        ArgumentNullException.ThrowIfNull(viewAgentAuthorityFormUseCase);
        _viewAgentAuthorityFormUseCase = viewAgentAuthorityFormUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAgentAuthorityFormDocument(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId,
        CancellationToken cancellationToken)
    {
        var result = await _viewAgentAuthorityFormUseCase
            .GetAgentAuthorityFormDocumentsAsync(agentAuthorityId, agentAuthorityFormId, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return RedirectToAction("Error", "Home");  // todo better error handling than this
        }

        return result.Value;
    }
}