using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Handles use case for a field manager to return an application to WO or AO
/// </summary>
public class ReturnApplicationUseCase(
    ILogger<ApproveRefuseOrReferApplicationUseCase> logger,
    IGetFellingLicenceApplicationForInternalUsers getFellingLicenceService,
    IUpdateFellingLicenceApplication updateFellingLicenceService,
    IAuditService<ApproveRefuseOrReferApplicationUseCase> auditService,
    IApproverReviewService approverReviewService,
    RequestContext requestContext)
{
    private readonly ILogger<ApproveRefuseOrReferApplicationUseCase> _logger = Guard.Against.Null(logger);
    private readonly IGetFellingLicenceApplicationForInternalUsers _getFellingLicenceService = Guard.Against.Null(getFellingLicenceService);
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceService = Guard.Against.Null(updateFellingLicenceService);
    private readonly IAuditService<ApproveRefuseOrReferApplicationUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);


    /// <summary>
    /// Returns an application that has been sent for approval.
    /// </summary>
    /// <param name="user">The internal user making the request.</param>
    /// <param name="applicationId">The application id.</param>
    /// <param name="requestedStatus">The requested status for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<FinaliseFellingLicenceApplicationResult> ReturnApplication(
    InternalUser user,
    Guid applicationId,
    CancellationToken cancellationToken)
    {
        var applicationResult = await _getFellingLicenceService.GetApplicationByIdAsync(
            applicationId,
            cancellationToken);

        if (applicationResult.IsFailure)
        {
            return FinaliseFellingLicenceApplicationResult.CreateFailure(applicationResult.Error,
                FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotRetrieveApplication);
        }

        var application = applicationResult.Value;
        var (hasPreviousStatus, previousStatus) = application.GetNthStatus(1);
        if (hasPreviousStatus is false)
        {
            _logger.LogWarning("Application with id {ApplicationId} does not have a previous status to revert to", applicationId);
            return FinaliseFellingLicenceApplicationResult.CreateFailure("Application does not have a previous status to revert to", FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationState);
        }
        if (previousStatus is not (FellingLicenceStatus.WoodlandOfficerReview or FellingLicenceStatus.AdminOfficerReview))
        {
            _logger.LogWarning("Application with id {ApplicationId} does not have a previous AO/WO status to revert to", applicationId);
            return FinaliseFellingLicenceApplicationResult.CreateFailure("Application does not have a previous AO/WO status to revert to", FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationState);
        }

        // check the application has the sent for approval status
        if (application.StatusHistories.MaxBy(x => x.Created)?.Status is not FellingLicenceStatus.SentForApproval)
        {
            _logger.LogError("Application must have a status of {sentForApproval} to be {requested}", FellingLicenceStatus.SentForApproval.GetDisplayName(), previousStatus.GetDisplayName());
            return FinaliseFellingLicenceApplicationResult.CreateFailure($"Application must have a status of {FellingLicenceStatus.SentForApproval.GetDisplayName()} to be {previousStatus.GetDisplayName()}",
                FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationState);
        }

        // check user is authorised to approve/refuse the application
        if (application.AssigneeHistories.NotAny(x =>
                x.AssignedUserId == user.UserAccountId && x.Role is AssignedUserRole.FieldManager))
        {
            _logger.LogError("User {id} is not an assigned field manager for the application", user.UserAccountId);
            return FinaliseFellingLicenceApplicationResult.CreateFailure(
                $"User {user.UserAccountId} is not an assigned field manager for the application",
                FinaliseFellingLicenceApplicationProcessOutcomes.UserRoleNotAuthorised);
        }

        await _updateFellingLicenceService.AddStatusHistoryAsync(
            user.UserAccountId!.Value,
            applicationId,
            previousStatus,
            cancellationToken);

        var nonBlockingFailures = new List<FinaliseFellingLicenceApplicationProcessOutcomes>();

        switch (previousStatus)
        {
            case FellingLicenceStatus.WoodlandOfficerReview:

                var updateResult = await approverReviewService.DeleteApproverReviewAsync(applicationId, cancellationToken);

                if (updateResult.IsSuccess)
                {
                    await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.RevertApproveToWoodlandOfficerReview,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            application.WoodlandOwnerId,
                            ApplicationAuthorId = application.CreatedById,
                            ApprovedByName = user.FullName,
                        }), cancellationToken);
                }
                break;
            case FellingLicenceStatus.AdminOfficerReview:
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.RevertApproveToAdminOfficerReview,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        application.WoodlandOwnerId,
                        ApplicationAuthorId = application.CreatedById,
                        RefusedByName = user.FullName,
                    }), cancellationToken);
                break;
        }

        return FinaliseFellingLicenceApplicationResult.CreateSuccess(nonBlockingFailures);
    }

}