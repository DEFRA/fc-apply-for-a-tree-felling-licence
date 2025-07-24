using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services.AgentAuthority;

/// <summary>
/// Handles the use case for downloading a specific agent authority form file.
/// </summary>
public class DownloadAgentAuthorityFormDocumentUseCase
{
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly ILogger<DownloadAgentAuthorityFormDocumentUseCase> _logger;
    private readonly IAuditService<DownloadAgentAuthorityFormDocumentUseCase> _audit;
    private readonly RequestContext _requestContext;

    public DownloadAgentAuthorityFormDocumentUseCase(
        IAgentAuthorityService agentAuthorityService,
        ILogger<DownloadAgentAuthorityFormDocumentUseCase> logger,
        IAuditService<DownloadAgentAuthorityFormDocumentUseCase> audit,
        RequestContext requestContext)
    {
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
        _audit = Guard.Against.Null(audit);
    }

    /// <summary>
    /// Download a specific agent authority form.
    /// </summary>
    /// <param name="user">The user performing the request.</param>
    /// <param name="agentAuthorityId">The id of the agent authority associated with the agent authority form to be downloaded.</param>
    /// <param name="agentAuthorityFormId">The id of the agent authority form to be downloaded.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns a <see cref="Result"/> object indicating success or failure
    /// of this action, upon success it will contain a <see cref="FileContentResult"/>.</returns>
    public async Task<Result<FileContentResult>> DownloadAgentAuthorityFormDocumentAsync(
        ExternalApplicant user,
        Guid agentAuthorityId,
        Guid agentAuthorityFormId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to download agent authority form document files for user with id {UserId} and agent authority form id of {agentAuthorityFormId}," +
                         " agent authority of {agentAuthorityId} for agency id {agency}",
            user.UserAccountId, agentAuthorityFormId, agentAuthorityId, user.AgencyId);

        var request = new GetAgentAuthorityFormDocumentsRequest
        {
            AgentAuthorityId = agentAuthorityId,
            AgentAuthorityFormId = agentAuthorityFormId, 
            AccessedByUser = user.UserAccountId!.Value,
        };

        var getAgentAuthorityFormFiles = await _agentAuthorityService.GetAgentAuthorityFormDocumentsAsync(
                    request, 
                    cancellationToken)
                .ConfigureAwait(false);

        if (getAgentAuthorityFormFiles.IsFailure)
        {
            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.DownloadAgentAuthorityFormFilesFailure,
                    new Guid(user.AgencyId!),
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        user.WoodlandOwnerName,
                        user.FullName,
                        request,
                        getAgentAuthorityFormFiles.Error
                    }
                ),
                cancellationToken);

            _logger.LogWarning("Could not retrieve agent authority form document files, error: {Error}", getAgentAuthorityFormFiles.Error);
            return Result.Failure<FileContentResult>("Could not retrieve agent authority form document files");
        }

        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.DownloadAgentAuthorityFormFiles,
                new Guid(user.AgencyId!),
                user.UserAccountId,
                _requestContext,
                new
                {
                    user.WoodlandOwnerName,
                    user.FullName,
                    request,
                    result = new
                    {
                        contentType = getAgentAuthorityFormFiles.Value.ContentType,
                        fileName = getAgentAuthorityFormFiles.Value.FileName,
                        fileSizeBytes = getAgentAuthorityFormFiles.Value.FileBytes.Length
                    }
                }
            ),
            cancellationToken);

        var fileContentResult = new FileContentResult(getAgentAuthorityFormFiles.Value.FileBytes, getAgentAuthorityFormFiles.Value.ContentType)
        {
            FileDownloadName = getAgentAuthorityFormFiles.Value.FileName
        };

        return Result.Success(fileContentResult);
    }
}
