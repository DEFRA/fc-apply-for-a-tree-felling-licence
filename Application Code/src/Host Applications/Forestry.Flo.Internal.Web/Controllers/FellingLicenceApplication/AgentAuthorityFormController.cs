using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

public class AgentAuthorityFormController : Controller
{
    private readonly IViewAgentAuthorityFormUseCase _viewAgentAuthorityFormUseCase;

    public AgentAuthorityFormController(IViewAgentAuthorityFormUseCase viewAgentAuthorityFormUseCase)
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