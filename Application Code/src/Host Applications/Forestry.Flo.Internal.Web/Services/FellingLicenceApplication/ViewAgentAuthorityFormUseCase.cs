using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

public class ViewAgentAuthorityFormUseCase : IViewAgentAuthorityFormUseCase
{
    private readonly IAgentAuthorityInternalService _agentAuthorityInternalService;
    private readonly ILogger<ViewAgentAuthorityFormUseCase> _logger;

    public ViewAgentAuthorityFormUseCase(
        IAgentAuthorityInternalService agentAuthorityInternalService,
        ILogger<ViewAgentAuthorityFormUseCase> logger)
    {
        ArgumentNullException.ThrowIfNull(agentAuthorityInternalService);
        _agentAuthorityInternalService = agentAuthorityInternalService;
        _logger = logger ?? new NullLogger<ViewAgentAuthorityFormUseCase>();
    }

    public async Task<Result<FileContentResult>> GetAgentAuthorityFormDocumentsAsync(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Received request to get agent authority documents with agent authority id {AgentAuthorityId} and agent authority form id {AgentAuthorityFormId}",
            agentAuthorityId, agentAuthorityFormId);

        var request = new GetAgentAuthorityFormDocumentsInternalRequest
        {
            AgentAuthorityId = agentAuthorityId,
            AgentAuthorityFormId = agentAuthorityFormId
        };

        var getDocsResult =
            await _agentAuthorityInternalService.GetAgentAuthorityFormDocumentsAsync(request, cancellationToken)
                .ConfigureAwait(false);

        if (getDocsResult.IsFailure)
        {
            _logger.LogWarning("Could not retrieve agent authority form documents, error: {Error}", getDocsResult.Error);
            return Result.Failure<FileContentResult>("Could not retrieve documents");
        }

        var fileContentResult = new FileContentResult(getDocsResult.Value.FileBytes, getDocsResult.Value.ContentType)
        {
            FileDownloadName = getDocsResult.Value.FileName
        };
        return Result.Success(fileContentResult);
    }
}