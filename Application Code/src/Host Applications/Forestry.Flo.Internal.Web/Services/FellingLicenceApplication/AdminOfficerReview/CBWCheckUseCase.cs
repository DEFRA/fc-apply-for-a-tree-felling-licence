using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Result = CSharpFunctionalExtensions.Result;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

public class CBWCheckUseCase : AdminOfficerReviewUseCaseBase
{
    public CBWCheckUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        ILogger<MappingCheckUseCase> logger,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IUpdateAdminOfficerReviewService updateAdminOfficerReviewService,
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
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

    }
    
    /// <summary>
    /// Gets a populated <see cref="CBWCheckModel"/> for admin officers to verify mapping for an application in review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="CBWCheckModel"/> containing details of an application's mapping.</returns>
    public async Task<Result<CBWCheckModel>> GetCBWCheckModelAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var (_, licenceRetrievalFailure, fellingLicence) = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);

        if (licenceRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<CBWCheckModel>("Unable to retrieve felling licence application");
        }

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            Logger.LogError("Unable to retrieve application summary for application {id}", fellingLicence.Id);
            return Result.Failure<CBWCheckModel>("Unable to retrieve application summary");
        }

        return new CBWCheckModel
        {
            ApplicationId = applicationId,
            FellingAndRestockingDetails = ModelMapping.RetrieveFellingAndRestockingDetails(fellingLicence),
            CheckFailedReason = fellingLicence.AdminOfficerReview?.MappingCheckFailureReason,
            CheckPassed = fellingLicence.AdminOfficerReview?.CBWChecked,
            FellingLicenceApplicationSummary = applicationSummary.Value
        };
    }

    /// <summary>
    /// Completes the mapping check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="isCheckPassed">A flag indicating whether the mapping check is successful.</param>
    /// <param name="failureReason">A textual reason why the mapping check has failed, if the check is unsuccessful.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the mapping check has been updated successfully.</returns>
    public async Task<Result> CompleteCBWCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var result = await UpdateAdminOfficerReviewService.SetCBWCheckCompletionAsync(
            applicationId,
            false,
            performingUserId,
            isCheckPassed,
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