using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services;

namespace Forestry.Flo.External.Web.Services.AgentAuthority;

/// <summary>
/// Handles the use case for getting agent authority forms for a specific agent authority.
/// </summary>
public class GetAgentAuthorityFormDocumentsUseCase
{
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly IUpdateFellingLicenceApplicationForExternalUsers _updateFellingLicenceApplicationService;
    private readonly ILogger<GetAgentAuthorityFormDocumentsUseCase> _logger;

    public GetAgentAuthorityFormDocumentsUseCase(
        IAgentAuthorityService agentAuthorityService,
        IRetrieveUserAccountsService retrieveUserAccountsService,
        IUpdateFellingLicenceApplicationForExternalUsers updateFellingLicenceApplicationService,
        ILogger<GetAgentAuthorityFormDocumentsUseCase> logger)
    {
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);
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

    /// <summary>
    /// Attempts to find the current Agent Authority Form document filename for a given agency and woodland owner.
    /// </summary>
    /// <param name="agencyId">The agency id.</param>
    /// <param name="woodlandOwnerId">The woodland owner id.</param>
    /// <param name="user">The external user performing the request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Maybe containing the first current AAF document filename and the AgentAuthorityId, if present.</returns>
    public async Task<Maybe<(string FileName, Guid? AgentAuthorityId)>> GetCurrentAafDocumentAsync(
        Guid agencyId,
        Guid woodlandOwnerId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        // Find the AgentAuthority id for the agency/woodland owner pair
        var maybeAgentAuthorityId = await _agentAuthorityService
            .FindAgentAuthorityIdAsync(agencyId, woodlandOwnerId, cancellationToken)
            .ConfigureAwait(false);

        if (maybeAgentAuthorityId.HasNoValue)
        {
            _logger.LogWarning("No AgentAuthority found for agency {AgencyId} and woodland owner {WoodlandOwnerId}", agencyId, woodlandOwnerId);
            return Maybe<(string FileName, Guid? AgentAuthorityId)>.None;
        }

        // Retrieve the documents model for this authority
        var docsResult = await GetAgentAuthorityFormDocumentsAsync(user, maybeAgentAuthorityId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (docsResult.IsFailure)
        {
            _logger.LogWarning("Unable to retrieve AgentAuthority documents for authority {AuthorityId}", maybeAgentAuthorityId.Value);
            return Maybe<(string FileName, Guid? AgentAuthorityId)>.None;
        }

        var fileName = docsResult.Value.CurrentAuthorityForm?.AafDocuments?.FirstOrDefault()?.FileName;
        return maybeAgentAuthorityId.HasNoValue
            ? Maybe<(string FileName, Guid? AgentAuthorityId)>.None
            : Maybe<(string FileName, Guid? AgentAuthorityId)>.From((fileName!, maybeAgentAuthorityId.Value));
    }

    /// <summary>
    /// Updates the AAF step status for the given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="aafStepStatus">The status to set for the AAF step (true = complete, false = incomplete).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure.</returns>
    public async Task<Result> CompleteAafStepAsync(
        Guid applicationId,
        ExternalApplicant user,
        bool aafStepStatus,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to complete AAF step status for application id {ApplicationId}", applicationId);

        var uam = await _retrieveUserAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);

        if (uam.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return Result.Failure(uam.Error);
        }

        var completeResult = await _updateFellingLicenceApplicationService.UpdateAafStepAsync(
            applicationId, uam.Value, aafStepStatus, cancellationToken).ConfigureAwait(false);

        if (completeResult.IsSuccess)
        {
            _logger.LogDebug("Successfully completed AAF step status for application id {ApplicationId}", applicationId);
            return completeResult;
        }

        _logger.LogError("Failed to complete AAF step status for application with id {ApplicationId}. Error: {Error}",
            applicationId, completeResult.Error);

        return completeResult;
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
