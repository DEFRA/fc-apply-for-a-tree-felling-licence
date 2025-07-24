using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;

namespace Forestry.Flo.External.Web.Services;

public class AgentAuthorityFormUseCase
{
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly ILogger<AgentAuthorityFormUseCase> _logger;
    private readonly IAuditService<AgentAuthorityFormUseCase> _audit;
    private readonly RequestContext _requestContext;

    public AgentAuthorityFormUseCase(
        IAgentAuthorityService agentAuthorityService,
        ILogger<AgentAuthorityFormUseCase> logger,
        IAuditService<AgentAuthorityFormUseCase> audit,
        RequestContext requestContext)
    {
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
        _audit = audit;
    }

    public async Task<Result<AddAgentAuthorityResponse>> HandleNewAgentAuthorityRequestAsync(
        AgentAuthorityFormModel model,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var authorityRequest = new AddAgentAuthorityRequest
        {
            CreatedByUser = user.UserAccountId!.Value,
            AgencyId = model.AgencyId,
            WoodlandOwner = new WoodlandOwnerModel
            {
                ContactAddress = ModelMapping.ToAddressEntity(model.ContactAddress),
                ContactEmail = model.ContactEmail,
                ContactName = model.ContactName,
                ContactTelephone = model.ContactTelephoneNumber,
                IsOrganisation = model.IsOrganisation,
                OrganisationAddress = ModelMapping.ToAddressEntity(model.OrganisationAddress!),
                OrganisationName = model.OrganisationName
            }
        };

        var (_, isFailure, response, error) = await _agentAuthorityService.AddAgentAuthorityAsync(authorityRequest, cancellationToken);

        if (isFailure)
        {
            // error is already logged in service
            
            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.CreateAgentAuthorityFormFailure,
                    model.AgencyId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        model.OrganisationName,
                        model.ContactName,
                        Error = error
                    }
                ),
                cancellationToken);

            return Result.Failure<AddAgentAuthorityResponse>("Failed to add new agent authority");
        }

        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.CreateAgentAuthorityForm,
                response.AgencyId,
                user.UserAccountId,
                _requestContext,
                new
                { 
                    response.AgencyName,
                    response.AgentAuthorityId
                }
            ),
            cancellationToken);
        
        return Result.Success<AddAgentAuthorityResponse>(response);
    }

    public async Task<Result<List<AgentAuthorityModel>>> GetAgentAuthorityFormsAsync(
        ExternalApplicant user,
        Guid agencyId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve all agent authority forms for user with id {UserId}", user.UserAccountId);
        var requests = await _agentAuthorityService
            .GetAgentAuthoritiesAsync(user.UserAccountId!.Value, agencyId,null, cancellationToken)
            .ConfigureAwait(false);

        if (requests.IsFailure)
        {
            _logger.LogError("Could not retrieve agent authority forms, error: {Error}", requests.Error);
            return requests.ConvertFailure<List<AgentAuthorityModel>>();
        }

        return Result.Success(requests.Value.AgentAuthorities);
    }
}
