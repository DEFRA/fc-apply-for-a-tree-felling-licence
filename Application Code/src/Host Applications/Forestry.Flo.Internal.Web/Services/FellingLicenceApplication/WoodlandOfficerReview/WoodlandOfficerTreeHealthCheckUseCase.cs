using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
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
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class WoodlandOfficerTreeHealthCheckUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IAgentAuthorityService agentAuthorityService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
    IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
    IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
    IAuditService<WoodlandOfficerTreeHealthCheckUseCase> auditService,
    RequestContext requestContext,
    ILogger<WoodlandOfficerTreeHealthCheckUseCase> logger)
    : FellingLicenceApplicationUseCaseBase(
        internalUserAccountService, 
        externalUserAccountService,
        fellingLicenceApplicationInternalRepository, 
        woodlandOwnerService, 
        agentAuthorityService,
        getConfiguredFcAreasService, 
        woodlandOfficerReviewSubStatusService), IWoodlandOfficerTreeHealthCheckUseCase
{
    private readonly IGetFellingLicenceApplicationForInternalUsers _getFellingLicenceApplication = Guard.Against.Null(getFellingLicenceApplication);
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
    private readonly ILogger<WoodlandOfficerTreeHealthCheckUseCase> _logger = logger ?? new NullLogger<WoodlandOfficerTreeHealthCheckUseCase>();
    private readonly IAuditService<WoodlandOfficerTreeHealthCheckUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);

    /// <inheritdoc />
    public async Task<Result<ConfirmTreeHealthIssuesViewModel>> GetTreeHealthCheckWoodlandOfficerViewModelAsync(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve conditions and conditional status for application with id {ApplicationId}", applicationId);

        var application = await _getFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);
        
        if (application.IsFailure)
        {
            _logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<ConfirmTreeHealthIssuesViewModel>("Unable to retrieve felling licence application");
        }

        var summaryResult = await ExtractApplicationSummaryAsync(application.Value, cancellationToken);
        if (summaryResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve application summary for application {ApplicationId}", applicationId);
            return Result.Failure<ConfirmTreeHealthIssuesViewModel>("Unable to retrieve application summary");
        }

        var result = new ConfirmTreeHealthIssuesViewModel
        {
            ApplicationId = applicationId,
            Confirmed = application.Value.WoodlandOfficerReview?.IsApplicantTreeHealthAnswersConfirmed,
            FellingLicenceApplicationSummary = summaryResult.Value,
            TreeHealthIssuesViewModel = new TreeHealthIssuesViewModel
            {
                TreeHealthIssues = new TreeHealthIssuesModel
                {
                    NoTreeHealthIssues = application.Value.IsTreeHealthIssue is not true,
                    TreeHealthIssueSelections = application.Value.TreeHealthIssues.Distinct().ToDictionary(item => item, _ => true),
                    OtherTreeHealthIssue = application.Value.TreeHealthIssueOther is true,
                    OtherTreeHealthIssueDetails = application.Value.TreeHealthIssueOtherDetails
                },
                TreeHealthDocuments = ModelMapping.ToDocumentModelList(
                    application.Value!.Documents?.Where(x => x.Purpose == DocumentPurpose.TreeHealthAttachment 
                                                             && x.DeletionTimestamp.HasNoValue()).ToList() ?? [])
            }
        };

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmTreeHealthIssuesAsync(
        Guid applicationId, 
        InternalUser user, 
        bool isConfirmed,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting woodland officer review tree health check status to {Confirmed} for application id {ApplicationId}", isConfirmed, applicationId);

        var result = await _updateWoodlandOfficerReviewService
            .ConfirmTreeHealthCheckAsync(applicationId, user.UserAccountId!.Value, isConfirmed, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Updating woodland officer review failed, error: {Error}", result.Error);

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TreeHealthWoCheckCompletedFailure,
                applicationId,
                user.UserAccountId!.Value,
                _requestContext,
                new
                {
                    error = result.Error
                }), cancellationToken);

            return result;
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.TreeHealthWoCheckCompleted,
            applicationId,
            user.UserAccountId!.Value,
            _requestContext,
            new { }), cancellationToken);

        return result;

    }
}