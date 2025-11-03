using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Usecase class for handling agent authorities to work on behalf of a woodland owner.
/// </summary>
public class AgentAuthorityFormUseCase
{
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly ILogger<AgentAuthorityFormUseCase> _logger;
    private readonly IAuditService<AgentAuthorityFormUseCase> _audit;
    private readonly RequestContext _requestContext;

    /// <summary>
    /// Creates a new instance of the <see cref="AgentAuthorityFormUseCase"/> class.
    /// </summary>
    /// <param name="agentAuthorityService">A <see cref="IAgentAuthorityService"/> to handle agent authorities.</param>
    /// <param name="logger">A logging instance.</param>
    /// <param name="audit">An auditing service.</param>
    /// <param name="requestContext">A request context.</param>
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

    /// <summary>
    /// Handles the use case for creating a new agent authority.
    /// </summary>
    /// <param name="model">A model of the agent authority entered by the user.</param>
    /// <param name="user">The user entering the data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure and an <see cref="AddAgentAuthorityResponse"/>
    /// model with details of the created authority if successful.</returns>
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

    /// <summary>
    /// Get all agent authority forms for a specific agency.
    /// </summary>
    /// <param name="user">The user making the request.</param>
    /// <param name="agencyId">The id of the agency to retrieve agent authority forms for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure and a list of
    /// <see cref="AgentAuthorityModel"/> models if successful.</returns>
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

    /// <summary>
    /// Attempts to find the current Agent Authority Id for a given agency and woodland owner.
    /// </summary>
    /// <param name="agencyId">The id of the agency to query.</param>
    /// <param name="woodlandOwnerId">The id of the woodland owner to query.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    public async Task<Maybe<Guid>> TryGetAgentAuthorityIdAsync(
        Guid agencyId,
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        return await _agentAuthorityService.FindAgentAuthorityIdAsync(agencyId, woodlandOwnerId, cancellationToken);
    }
}
