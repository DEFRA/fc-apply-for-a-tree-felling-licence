using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;

namespace Forestry.Flo.External.Web.Services.AgentAuthority;

/// <summary>
/// Handles the use case for removing a specific agent authority form.
/// </summary>
public class RemoveAgentAuthorityFormDocumentUseCase
{
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly ILogger<RemoveAgentAuthorityFormDocumentUseCase> _logger;
    private readonly IAuditService<RemoveAgentAuthorityFormDocumentUseCase> _audit;
    private readonly RequestContext _requestContext;

    public RemoveAgentAuthorityFormDocumentUseCase(
        IAgentAuthorityService agentAuthorityService,
        ILogger<RemoveAgentAuthorityFormDocumentUseCase> logger,
        IAuditService<RemoveAgentAuthorityFormDocumentUseCase> audit,
        RequestContext requestContext)
    {
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
        _audit = Guard.Against.Null(audit);
    }

    /// <summary>
    /// Remove a specific agent authority form.
    /// </summary>
    /// <remarks>
    /// Note: Current <see cref="AgentAuthorityService"/> implementation does not delete/remove the form, it provides an effective end date on its
    /// validity of the current instant, thus making it a non-current AAF / Historical AAF.
    /// </remarks>
    /// <param name="user">The user performing the request.</param>
    /// <param name="agentAuthorityId">The id of the agent authority associated with the agent authority form to be removed.</param>
    /// <param name="agentAuthorityFormId">The id of the agent authority form to be removed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns a <see cref="Result"/> object indicating success or failure.</returns>
    public async Task<Result> RemoveAgentAuthorityDocumentAsync(
        ExternalApplicant user,
        Guid agentAuthorityId,
        Guid agentAuthorityFormId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to remove agent authority form document for user with id {UserId} and agent authority form id of {agentAuthorityFormId}," +
                         " agent authority of {agentAuthorityId} for agency id {agency}",
            user.UserAccountId, agentAuthorityFormId, agentAuthorityId, user.AgencyId);

        var request = new RemoveAgentAuthorityFormRequest
        {
            AgentAuthorityId = agentAuthorityId,
            AgentAuthorityFormId = agentAuthorityFormId,
            RemovedByUser = user.UserAccountId!.Value,

        };

        var (_, isFailure, error) = await _agentAuthorityService.RemoveAgentAuthorityFormAsync(
                request, 
                cancellationToken)
            .ConfigureAwait(false);

        if (isFailure)
        {
            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.RemoveAgentAuthorityFormFailure,
                    new Guid(user.AgencyId!),
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        user.WoodlandOwnerName,
                        user.FullName,
                        request,
                        Error = error
                    }
            ),
            cancellationToken);

            _logger.LogError("Could not remove the agent authority form, error: {Error}", error);
            return Result.Failure("Failed to remove the agent authority form");
        }

        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.RemoveAgentAuthorityForm,
                new Guid(user.AgencyId!),
                user.UserAccountId,
                _requestContext,
                new
                {
                    user.WoodlandOwnerName,
                    user.FullName,
                    request
                }
            ),
            cancellationToken);

        return Result.Success();
    }
}
