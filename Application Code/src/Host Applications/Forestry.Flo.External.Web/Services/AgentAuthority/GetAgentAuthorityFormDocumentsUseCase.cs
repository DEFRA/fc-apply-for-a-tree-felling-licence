using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;

namespace Forestry.Flo.External.Web.Services.AgentAuthority;

/// <summary>
/// Handles the use case for getting agent authority forms for a specific agent authority.
/// </summary>
public class GetAgentAuthorityFormDocumentsUseCase
{
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly ILogger<GetAgentAuthorityFormDocumentsUseCase> _logger;

    public GetAgentAuthorityFormDocumentsUseCase(
        IAgentAuthorityService agentAuthorityService,
        ILogger<GetAgentAuthorityFormDocumentsUseCase> logger)
    {
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
        _logger = logger;
    }

    /// <summary>
    /// Get all agent authority forms as an <see cref="AgentAuthorityFormDocumentModel"/> which are associated to a specific agent authority.
    /// </summary>
    /// <param name="user">The user performing the request.</param>
    /// <param name="agentAuthorityId">The id of the agent authority to be queried to retrieve the agent authority forms.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns a <see cref="Result"/> object indicating success or failure
    /// of this action, upon success it will contain a <see cref="AgentAuthorityFormDocumentModel"/>.</returns>
    public async Task<Result<AgentAuthorityFormDocumentModel>> GetAgentAuthorityFormDocumentsAsync(
        ExternalApplicant user,
        Guid agentAuthorityId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve all agent authority form documents for user with id {UserId} for agent authority of {agentAuthority}",
            user.UserAccountId, agentAuthorityId);

        var result = await _agentAuthorityService.GetAgentAuthorityFormDocumentsByAuthorityIdAsync(
            user.UserAccountId!.Value, 
            agentAuthorityId, 
            cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            _logger.LogWarning("Unable to get agent authority forms");
            return Result.Failure<AgentAuthorityFormDocumentModel>("Unable to get agent authority forms");
        }

        var viewModel = AddResultToViewModel(agentAuthorityId, result.Value);

        return Result.Success(viewModel);
    }

    private AgentAuthorityFormDocumentModel AddResultToViewModel(
        Guid agentAuthorityId, 
        AgentAuthorityFormsWithWoodlandOwnerResponseModel result)
    {
        var viewModel = new AgentAuthorityFormDocumentModel
        {
            AgentAuthorityId = agentAuthorityId,
            AgencyId = result.AgencyId,
            WoodlandOwnerId = result.WoodlandOwnerModel.Id,
            WoodlandOwnerOrOrganisationName = GetWoodlandOwnerOrOrganisationForDisplay(result.WoodlandOwnerModel)
        };

        var forms = result.AgentAuthorityFormResponseModels;

        if (!forms.Any())
        {
            _logger.LogDebug("No forms found.");
            return viewModel;
        }
        
        var currentForm = forms
            .Where(x => x.ValidToDate == null)
            .MaxBy(x => x.ValidFromDate);

        if (currentForm != null)
        {
            viewModel.CurrentAuthorityForm = new AgentAuthorityFormDocumentItemModel
            {
                ValidToDate = currentForm.ValidToDate,
                ValidFromDate = currentForm.ValidFromDate,
                AafDocuments = currentForm.AafDocuments,
                Id = currentForm.Id
            };
        }

        viewModel.HistoricAuthorityForms = forms
            .Where(x => x.ValidToDate != null)
            .OrderByDescending(x => x.ValidFromDate)
            .ToList()
            .Select(x => new AgentAuthorityFormDocumentItemModel
            {
                ValidToDate = x.ValidToDate,
                ValidFromDate = x.ValidFromDate,
                AafDocuments = x.AafDocuments,
                Id = x.Id
            })
            .ToList();

        return viewModel;
    }

    private static string? GetWoodlandOwnerOrOrganisationForDisplay(WoodlandOwnerModel woodlandOwnerModel)
    {
        return woodlandOwnerModel.IsOrganisation ? woodlandOwnerModel.OrganisationName : woodlandOwnerModel.ContactName;
    }
}
