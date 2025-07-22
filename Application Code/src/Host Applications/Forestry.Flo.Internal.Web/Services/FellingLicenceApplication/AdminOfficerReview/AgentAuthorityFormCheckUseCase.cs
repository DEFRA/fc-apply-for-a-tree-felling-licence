using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Result = CSharpFunctionalExtensions.Result;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

public class AgentAuthorityFormCheckUseCase : AdminOfficerReviewUseCaseBase
{
    private readonly IAgentAuthorityInternalService _agentAuthorityInternalService;
    private readonly IAgentAuthorityService _agentAuthorityService;

    public AgentAuthorityFormCheckUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        ILogger<AgentAuthorityFormCheckUseCase> logger,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IUpdateAdminOfficerReviewService updateAdminOfficerReviewService,
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
        IAgentAuthorityInternalService agentAuthorityInternalService,
        IAgentAuthorityService agentAuthorityService,
        IAuditService<AdminOfficerReviewUseCaseBase> auditService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        RequestContext requestContext) 
        : base(internalUserAccountService, 
            externalUserAccountService,
            logger,
            fellingLicenceApplicationInternalRepository,
            woodlandOwnerService,
            updateAdminOfficerReviewService,
            getFellingLicenceApplication,
            auditService,
            agentAuthorityService,
            getConfiguredFcAreasService,
            requestContext)
    {
        ArgumentNullException.ThrowIfNull(agentAuthorityService);
        ArgumentNullException.ThrowIfNull(agentAuthorityInternalService);

        _agentAuthorityInternalService = agentAuthorityInternalService;
        _agentAuthorityService = agentAuthorityService;
    }

    /// <summary>
    /// Gets a populated <see cref="AgentAuthorityFormCheckModel"/> for admin officers to verify an agent authority form for an application in review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="AgentAuthorityFormCheckModel"/> containing details of an application's agent authority form.</returns>
    public async Task<Result<AgentAuthorityFormCheckModel>> GetAgentAuthorityFormCheckModelAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var (_, licenceRetrievalFailure, fellingLicence) = await GetFellingLicenceApplication
            .GetApplicationByIdAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (licenceRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<AgentAuthorityFormCheckModel>("Unable to retrieve felling licence application");
        }

        // TODO we could potentially optimise here as we are retrieving the WO and Agency again in ExtractApplicationSummary below
        var (_, woodlandOwnerRetrievalFailure, woodlandOwner) = await WoodlandOwnerService
            .RetrieveWoodlandOwnerByIdAsync(fellingLicence.WoodlandOwnerId, cancellationToken)
            .ConfigureAwait(false);

        if (woodlandOwnerRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve woodland owner with id {WoodlandOwnerId}", fellingLicence.WoodlandOwnerId);
            return Result.Failure<AgentAuthorityFormCheckModel>("Unable to retrieve woodland owner");
        }

        var agencyForWoodlandOwner = await _agentAuthorityService
            .GetAgencyForWoodlandOwnerAsync(fellingLicence.WoodlandOwnerId, cancellationToken)
            .ConfigureAwait(false);

        if (agencyForWoodlandOwner.HasNoValue)
        {
            Logger.LogWarning("Application with ID {ApplicationId} is for woodland owner with ID {WoodlandOwnerId} which is not managed by an agency",
                applicationId, fellingLicence.WoodlandOwnerId);
            return Result.Failure<AgentAuthorityFormCheckModel>("Woodland owner for this application is not managed by an agency");
        }

        var latestSubmitted = fellingLicence.StatusHistories
            .Where(x => x.Status == FellingLicenceStatus.Submitted)
            .MaxBy(x => x.Created);

        var (_, agentAuthorityFormRetrievalFailure, agentAuthorityFormResponse) =
            await _agentAuthorityInternalService.GetAgentAuthorityFormAsync(new GetAgentAuthorityFormRequest
                {
                    AgencyId = agencyForWoodlandOwner.Value.AgencyId!.Value,
                    WoodlandOwnerId = fellingLicence.WoodlandOwnerId,
                    PointInTime = latestSubmitted?.Created ?? DateTime.UtcNow
                },
                cancellationToken);

        if (agentAuthorityFormRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve agent authority form for agency {AgencyId} and woodland owner {WoodlandOwnerId}", agencyForWoodlandOwner.Value.AgencyId, fellingLicence.WoodlandOwnerId);
            return Result.Failure<AgentAuthorityFormCheckModel>("Unable to retrieve agent authority form");
        }

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            Logger.LogError("Unable to retrieve application summary for application {id}", fellingLicence.Id);
            return Result.Failure<AgentAuthorityFormCheckModel>("Unable to retrieve application summary");
        }

        return new AgentAuthorityFormCheckModel
        {
            ApplicationId = applicationId,
            ApplicationOwner = new ApplicationOwnerModel
            {
                Agency = agencyForWoodlandOwner.Value,
                AgentAuthorityForm = new AgentAuthorityFormViewModel
                {
                    CouldRetrieveAgentAuthorityFormDetails = true,
                    AgentAuthorityId = agentAuthorityFormResponse.AgentAuthorityId,
                    CurrentAgentAuthorityForm = agentAuthorityFormResponse.CurrentAgentAuthorityForm,
                    SpecificTimestampAgentAuthorityForm = agentAuthorityFormResponse.SpecificTimestampAgentAuthorityForm,
                    // todo: replace this with the external agent authority form management URL for this application
                    AgentAuthorityFormManagementUrl = Maybe<string>.None
                },
                WoodlandOwner = ModelMapping.ToWoodlandOwnerModel(woodlandOwner)
            },
            CheckFailedReason = fellingLicence.AdminOfficerReview?.AgentAuthorityCheckFailureReason,
            CheckPassed = fellingLicence.AdminOfficerReview?.AgentAuthorityCheckPassed,
            FellingLicenceApplicationSummary = applicationSummary.Value
        };
    }

    /// <summary>
    /// Completes the agent authority form check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="isCheckPassed">A flag indicating whether the agent authority form check is successful.</param>
    /// <param name="failureReason">A textual reason why the agent authority form check has failed, if the check is unsuccessful.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the agent authority check has been updated successfully.</returns>
    public async Task<Result> CompleteAgentAuthorityCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed, 
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var result = await UpdateAdminOfficerReviewService.UpdateAgentAuthorityFormDetailsAsync(
            applicationId,
            performingUserId,
            isCheckPassed, 
            failureReason, 
            cancellationToken);

        if (result.IsFailure)
        {
            await AuditAdminOfficerReviewUpdateFailureAsync(
                applicationId,
                result.Error,
                performingUserId,
                cancellationToken);

            return result;
        }

        await AuditAdminOfficerReviewUpdateAsync(
            applicationId,
            true,
            performingUserId,
            cancellationToken);

        return result;
    }
}