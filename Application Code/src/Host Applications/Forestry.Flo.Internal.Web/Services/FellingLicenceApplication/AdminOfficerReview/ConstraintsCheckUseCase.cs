using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Result = CSharpFunctionalExtensions.Result;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

public class ConstraintsCheckUseCase : AdminOfficerReviewUseCaseBase, IConstraintsCheckUseCase
{
    public ConstraintsCheckUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        ILogger<ConstraintsCheckUseCase> logger,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IUpdateAdminOfficerReviewService updateAdminOfficerReviewService,
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
        IAuditService<AdminOfficerReviewUseCaseBase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
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
            woodlandOfficerReviewSubStatusService,
            requestContext)
    {

    }
    
    /// <summary>
    /// Gets a populated <see cref="ConstraintsCheckModel"/> for admin officers to verify mapping for an application in review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ConstraintsCheckModel"/> containing details of an application's mapping.</returns>
    public async Task<Result<ConstraintsCheckModel>> GetConstraintsCheckModel(Guid applicationId, CancellationToken cancellationToken)
    {
        var (_, licenceRetrievalFailure, fellingLicence) = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);

        if (licenceRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<ConstraintsCheckModel>("Unable to retrieve felling licence application");
        }

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            Logger.LogError("Unable to retrieve application summary for application {id}", fellingLicence.Id);
            return Result.Failure<ConstraintsCheckModel>("Unable to retrieve application summary");
        }

        return new ConstraintsCheckModel
        {
            FellingLicenceApplicationSummary = applicationSummary.Value,
            ApplicationId = applicationId,
            IsComplete = fellingLicence.AdminOfficerReview?.ConstraintsChecked ?? false
        };
    }

    /// <summary>
    /// Completes the constraints check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="isAgencyApplication">A flag indicating if the application is an Agency application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="isCheckComplete">A flag indicating whether the constraints check is complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the admin officer review has been updated successfully.</returns>
    public async Task<Result> CompleteConstraintsCheckAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool isCheckComplete,
        CancellationToken cancellationToken)
    {
        var result = await UpdateAdminOfficerReviewService.SetConstraintsCheckCompletionAsync(
            applicationId,
            isAgencyApplication,
            performingUserId,
            isCheckComplete,
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