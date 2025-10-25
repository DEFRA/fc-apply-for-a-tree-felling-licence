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

public class MappingCheckUseCase : AdminOfficerReviewUseCaseBase, IMappingCheckUseCase
{
    public MappingCheckUseCase(
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

    /// <inheritdoc />
    public async Task<Result<MappingCheckModel>> GetMappingCheckModelAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var (_, licenceRetrievalFailure, fellingLicence) = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);

        if (licenceRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<MappingCheckModel>("Unable to retrieve felling licence application");
        }

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            Logger.LogError("Unable to retrieve application summary for application {id}", fellingLicence.Id);
            return Result.Failure<MappingCheckModel>("Unable to retrieve application summary");
        }

        return new MappingCheckModel
        {
            ApplicationId = applicationId,
            FellingAndRestockingDetails = ModelMapping.RetrieveFellingAndRestockingDetails(fellingLicence),
            CheckFailedReason = fellingLicence.AdminOfficerReview?.MappingCheckFailureReason,
            CheckPassed = fellingLicence.AdminOfficerReview?.MappingCheckPassed,
            FellingLicenceApplicationSummary = applicationSummary.Value
        };
    }

    /// <inheritdoc />
    public async Task<Result> CompleteMappingCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var result = await UpdateAdminOfficerReviewService.UpdateMappingCheckDetailsAsync(
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