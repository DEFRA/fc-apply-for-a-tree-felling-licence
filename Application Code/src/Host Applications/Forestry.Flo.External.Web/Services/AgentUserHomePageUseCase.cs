using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Services;

namespace Forestry.Flo.External.Web.Services;

public class AgentUserHomePageUseCase
{
    private readonly ILogger<AgentUserHomePageUseCase> _logger;
    private readonly IAgentAuthorityService _agentAuthorityService;

    public AgentUserHomePageUseCase(
        IAgentAuthorityService agentAuthorityService,
        ILogger<AgentUserHomePageUseCase> logger)
    {
        _logger = Guard.Against.Null(logger);
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
    }

    public async Task<Maybe<IReadOnlyCollection<WoodlandOwnerSummary>>> GetWoodlandOwnersForAgencyAsync(
        ExternalApplicant user, 
        Guid agencyId,
        CancellationToken cancellationToken)
    {
        var userId = Guard.Against.Null(user.UserAccountId);
        List<WoodlandOwnerSummary> result = new();

        var getAgentAuthorities = await _agentAuthorityService
            .GetAgentAuthoritiesAsync(userId, agencyId, new [] { AgentAuthorityStatus.FormUploaded, AgentAuthorityStatus.Created }, cancellationToken)
            .ConfigureAwait(false);

        if (getAgentAuthorities.IsFailure)
        {
            _logger.LogError("Could not retrieve agent authority entries for current user");
            return Maybe<IReadOnlyCollection<WoodlandOwnerSummary>>.None;
        }

        result = getAgentAuthorities.Value.AgentAuthorities.Select(a => 
                new WoodlandOwnerSummary(
                    a.WoodlandOwner.Id!.Value, 
                    agencyId,
                    a.WoodlandOwner.ContactName, 
                    a.WoodlandOwner.ContactEmail!, 
                    a.WoodlandOwner.OrganisationName)).ToList();
        
        return !result.Any()
            ? Maybe<IReadOnlyCollection<WoodlandOwnerSummary>>.None
            : Maybe<IReadOnlyCollection<WoodlandOwnerSummary>>.From(result);
    }
}