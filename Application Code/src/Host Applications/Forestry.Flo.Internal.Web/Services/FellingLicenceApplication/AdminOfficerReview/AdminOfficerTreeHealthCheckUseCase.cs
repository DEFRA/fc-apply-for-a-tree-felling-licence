using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

/// <summary>
/// Implementation of <see cref="IAdminOfficerTreeHealthCheckUseCase"/>
/// </summary>
/// <param name="internalUserAccountService">A <see cref="IUserAccountService"/> instance.</param>
/// <param name="externalUserAccountService">A <see cref="IRetrieveUserAccountsService"/> instance.</param>
/// <param name="logger">A logging instance.</param>
/// <param name="fellingLicenceApplicationInternalRepository">A <see cref="IFellingLicenceApplicationInternalRepository"/> instance.</param>
/// <param name="woodlandOwnerService">A <see cref="IRetrieveWoodlandOwners"/> instance.</param>
/// <param name="updateAdminOfficerReviewService">A <see cref="IUpdateAdminOfficerReviewService"/> instance.</param>
/// <param name="getFellingLicenceApplication">A <see cref="IGetFellingLicenceApplicationForInternalUsers"/> instance.</param>
/// <param name="auditService">An auditing instance.</param>
/// <param name="agentAuthorityService">A <see cref="IAgentAuthorityService"/> instance.</param>
/// <param name="getConfiguredFcAreasService">A <see cref="IGetConfiguredFcAreas"/> instance.</param>
/// <param name="woodlandOfficerReviewSubStatusService">A <see cref="IWoodlandOfficerReviewSubStatusService"/> instance.</param>
/// <param name="requestContext">A <see cref="RequestContext"/> instance.</param>
public class AdminOfficerTreeHealthCheckUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    ILogger<AdminOfficerTreeHealthCheckUseCase> logger,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IUpdateAdminOfficerReviewService updateAdminOfficerReviewService,
    IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
    IAuditService<AdminOfficerReviewUseCaseBase> auditService,
    IAgentAuthorityService agentAuthorityService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
    RequestContext requestContext)
    : AdminOfficerReviewUseCaseBase(internalUserAccountService,
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
        requestContext), IAdminOfficerTreeHealthCheckUseCase
{

    /// <inheritdoc />
    public async Task<Result<CheckTreeHealthIssuesViewModel>> GetTreeHealthCheckAdminOfficerViewModelAsync(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        var (_, licenceRetrievalFailure, fellingLicence) = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);

        if (licenceRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<CheckTreeHealthIssuesViewModel>("Unable to retrieve felling licence application");
        }

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            Logger.LogError("Unable to retrieve application summary for application {id}", fellingLicence.Id);
            return Result.Failure<CheckTreeHealthIssuesViewModel>("Unable to retrieve application summary");
        }

        var result = new CheckTreeHealthIssuesViewModel
        {
            FellingLicenceApplicationSummary = applicationSummary.Value,
            ApplicationId = applicationId,
            Confirmed = fellingLicence.AdminOfficerReview?.IsTreeHealthAnswersChecked is true,
            TreeHealthIssuesViewModel = new TreeHealthIssuesViewModel
            {
                TreeHealthIssues = new TreeHealthIssuesModel
                {
                    NoTreeHealthIssues = fellingLicence.IsTreeHealthIssue is not true,
                    TreeHealthIssueSelections = fellingLicence.TreeHealthIssues.Distinct().ToDictionary(item => item, _ => true),
                    OtherTreeHealthIssue = fellingLicence.TreeHealthIssueOther is true,
                    OtherTreeHealthIssueDetails = fellingLicence.TreeHealthIssueOtherDetails
                },
                TreeHealthDocuments = ModelMapping.ToDocumentModelList(
                    fellingLicence!.Documents?.Where(x => x.Purpose == DocumentPurpose.TreeHealthAttachment 
                                                          && x.DeletionTimestamp.HasNoValue()).ToList() ?? [])
            }
        };

        SetBreadcrumbs(result, "Review tree health", true);

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmTreeHealthCheckedAsync(Guid applicationId, InternalUser user, CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting admin officer review tree health check status to confirmed for application id {ApplicationId}", applicationId);

        var result = await UpdateAdminOfficerReviewService
            .ConfirmTreeHealthCheckAsync(applicationId, user.UserAccountId!.Value, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Updating admin officer review failed, error: {Error}", result.Error);

            await AuditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TreeHealthOaoCheckCompletedFailure,
                applicationId,
                user.UserAccountId!.Value,
                RequestContext,
                new
                {
                    error = result.Error
                }), cancellationToken);

            return result;
        }

        await AuditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.TreeHealthOaoCheckCompleted,
            applicationId,
            user.UserAccountId!.Value,
            RequestContext,
            new { }), cancellationToken);

        return result;
    }
}