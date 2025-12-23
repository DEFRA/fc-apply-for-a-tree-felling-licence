using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.ApproverReview;

/// <summary>
/// Use case for handling the Approved In Error process of felling licence applications.
/// Mirrors the patterns used in ApproverReviewUseCase.
/// </summary>
public class ApprovedInErrorUseCase : FellingLicenceApplicationUseCaseBase, IApprovedInErrorUseCase
{
    private readonly IAgentAuthorityInternalService _agentAuthorityInternalService;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly ILogger<FellingLicenceApplicationUseCase> _logger;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IApprovedInErrorService _approvedInErrorService;
    private readonly IAuditService<FellingLicenceApplicationUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly FellingLicenceApplicationOptions _fellingLicenceApplicationOptions;
    private readonly IClock _clock;
    private readonly IApproverReviewUseCase _approverReviewUseCase;
    private readonly IApproveRefuseOrReferApplicationUseCase _approvalRefusalUseCase;
    private readonly IGeneratePdfApplicationUseCase _generatePdfApplicationUseCase;
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceService;
    private readonly ISendNotifications _notificationsService;
    private readonly IRetrieveUserAccountsService _externalAccountService;
    private readonly IUserAccountService _internalAccountService;
    private readonly ExternalApplicantSiteOptions _externalApplicantSiteOptions;

    // Extract all Parameter 6 values from conditions and combine them
    private const int SupplementaryPointsParameterIndex = 6;

    public ApprovedInErrorUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IAuditService<FellingLicenceApplicationUseCase> auditService,
    IActivityFeedItemProvider activityFeedItemProvider,
    IAgentAuthorityService agentAuthorityService,
    IAgentAuthorityInternalService agentAuthorityInternalService,
    IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
    IApprovedInErrorService approvedInErrorService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    RequestContext requestContext,
    IOptions<FellingLicenceApplicationOptions> fellingLicenceApplicationOptions,
    IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
    IClock clock,
    ILogger<FellingLicenceApplicationUseCase> logger,
    IApproverReviewUseCase approverReviewUseCase,
    IApproveRefuseOrReferApplicationUseCase approvalRefusalUseCase,
    IGeneratePdfApplicationUseCase generatePdfApplicationUseCase,
    IUpdateFellingLicenceApplication updateFellingLicenceService,
    ISendNotifications notificationsService,
    IOptions<ExternalApplicantSiteOptions> externalApplicantSiteOptions)
    : base(internalUserAccountService,
    externalUserAccountService,
    fellingLicenceApplicationInternalRepository,
    woodlandOwnerService,
    agentAuthorityService,
    getConfiguredFcAreasService,
    woodlandOfficerReviewSubStatusService)
    {
        _agentAuthorityInternalService = Guard.Against.Null(agentAuthorityInternalService);
        _internalAccountService = Guard.Against.Null(internalUserAccountService);
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = Guard.Against.Null(logger);
        _activityFeedItemProvider = Guard.Against.Null(activityFeedItemProvider);
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _approvedInErrorService = Guard.Against.Null(approvedInErrorService);
        _fellingLicenceApplicationOptions = fellingLicenceApplicationOptions.Value;
        _clock = Guard.Against.Null(clock);
        _approverReviewUseCase = Guard.Against.Null(approverReviewUseCase);
        _approvalRefusalUseCase = Guard.Against.Null(approvalRefusalUseCase);
        _generatePdfApplicationUseCase = Guard.Against.Null(generatePdfApplicationUseCase);
        _updateFellingLicenceService = Guard.Against.Null(updateFellingLicenceService);
        _notificationsService = Guard.Against.Null(notificationsService);
        _externalAccountService = Guard.Against.Null(externalUserAccountService);
        _externalApplicantSiteOptions = Guard.Against.Null(externalApplicantSiteOptions).Value;
    }

    /// <inheritdoc />
    public async Task<Maybe<ApprovedInErrorViewModel>> RetrieveApprovedInErrorAsync(
    Guid applicationId,
    InternalUser viewingUser,
    CancellationToken cancellationToken)
    {
        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
            return Maybe<ApprovedInErrorViewModel>.None;
        }

        var approvedInError = await _approvedInErrorService.GetApprovedInErrorAsync(applicationId, cancellationToken);

        var model = new ApprovedInErrorViewModel
        {
            Id = application.Value.Id,
            ViewingUser = viewingUser,
            ApplicationId = application.Value.Id,
            PreviousReference = application.Value.ApplicationReference,
            ReasonExpiryDate = approvedInError.HasValue && approvedInError.Value.ReasonExpiryDate,
            ReasonSupplementaryPoints = approvedInError.HasValue && approvedInError.Value.ReasonSupplementaryPoints,
            ReasonOther = approvedInError.HasValue && approvedInError.Value.ReasonOther,
            ReasonExpiryDateText = approvedInError.HasValue ? approvedInError.Value.ReasonExpiryDateText : null,
        };

        var summary = await ExtractApplicationSummaryAsync(application.Value, cancellationToken);
        if (summary.IsFailure)
        {
            _logger.LogError("Application summary cannot be extracted, application id: {ApplicationId}, error {Error}", application.Value.Id, summary.Error);
            return Maybe<ApprovedInErrorViewModel>.None;
        }
        model.FellingLicenceApplicationSummary = summary.Value;

        var creator = await GetSubmittingUserAsync(application.Value.CreatedById, cancellationToken);
        if (creator.IsFailure)
        {
            _logger.LogError("Unable to retrieve the details of the external user who submitted the application, application id: {ApplicationId}, external user: {createdById} , error {Error}", application.Value.Id, application.Value.CreatedById, creator.Error);
            return Maybe<ApprovedInErrorViewModel>.None;
        }

        return Maybe<ApprovedInErrorViewModel>.From(model);
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmApprovedInErrorAsync(
    ApprovedInErrorModel model,
    InternalUser user,
    CancellationToken cancellationToken)
    {
        if (user.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            _logger.LogError("User {UserAccountId} is not an account administrator and cannot mark an application as approved in error.", user.UserAccountId);
            return Result.Failure("You do not have permission to mark an application as approved in error.");
        }

        _logger.LogDebug("Attempting to store Approved In Error details for application with id {ApplicationId}", model.ApplicationId);

        var updateResult = await _approvedInErrorService.SetToApprovedInErrorAsync(
            model.ApplicationId,
            model,
            user.UserAccountId!.Value,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ConfirmApprovedInError,
                model.ApplicationId,
                user.UserAccountId,
                _requestContext,
                new { model.ReasonExpiryDate, model.ReasonSupplementaryPoints, model.ReasonOther, model.ReasonExpiryDateText, model.PreviousReference }),
                cancellationToken);

            // Send notifications to assigned internal users
            var notificationResult = await SendInternalApprovedInErrorNotifications(
                model.ApplicationId,
                model.PreviousReference,
                user,
                cancellationToken);

            if (notificationResult.IsFailure)
            {
                _logger.LogWarning("Failed to send approved in error notifications with error {Error}", notificationResult.Error);
                // Note: This is non-blocking - we don't fail the entire operation if notification fails
            }

            // Retrieve application for sending applicant notification
            var application = await _fellingLicenceApplicationInternalRepository.GetAsync(model.ApplicationId, cancellationToken);
            if (application.HasValue)
            {
                // Send notification to applicant informing them a new licence is required
                _logger.LogDebug("Sending notification to applicant for application marked as approved in error {ApplicationId}", model.ApplicationId);
                var applicantNotificationResult = await SendApplicantNotificationApprovedInErrorAsync(
                    application.Value,
                    NotificationType.InformApplicantOfAIENewLicenceRequired,
                    cancellationToken);

                if (applicantNotificationResult.IsFailure)
                {
                    _logger.LogWarning("Failed to send applicant notification for approved in error with error {Error}", applicantNotificationResult.Error);
                    // Note: This is non-blocking - we don't fail the entire operation if notification fails
                }
            }
            else
            {
                _logger.LogWarning("Could not retrieve application {ApplicationId} to send applicant notification", model.ApplicationId);
            }

            return Result.Success();
        }

        _logger.LogError("Failed to update Approved In Error with error {Error}", updateResult.Error);

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmApprovedInErrorFailure,
            model.ApplicationId,
            user.UserAccountId,
            _requestContext,
            new { updateResult.Error, model.ReasonExpiryDate, model.ReasonSupplementaryPoints, model.ReasonOther, model.ReasonExpiryDateText, model.PreviousReference }),
            cancellationToken);

        return Result.Failure(updateResult.Error);
    }

    /// <inheritdoc />
    public async Task<Maybe<ReApproveInErrorViewModel>> RetrieveReApproveInErrorAsync(Guid applicationId, InternalUser viewingUser, CancellationToken cancellationToken)
    {
        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
            return Maybe<ReApproveInErrorViewModel>.None;
        }
        var approvedInError = await _approvedInErrorService.GetApprovedInErrorAsync(applicationId, cancellationToken);

        var woodlandOfficerReviewStatus = await _getWoodlandOfficerReviewService.GetWoodlandOfficerReviewStatusAsync(
                applicationId, cancellationToken);

        if (woodlandOfficerReviewStatus.IsFailure)
        {
            _logger.LogError("Failed to retrieve woodland officer review details with error {Error}", woodlandOfficerReviewStatus.Error);
            return Maybe<ReApproveInErrorViewModel>.None;
        }

        var approvedInErrorViewModel = new ApprovedInErrorViewModel
        {
            Id = application.Value.Id,
            ViewingUser = viewingUser,
            ApplicationId = application.Value.Id,
            PreviousReference = application.Value.ApplicationReference,
            ReasonExpiryDate = approvedInError.HasValue && approvedInError.Value.ReasonExpiryDate,
            ReasonSupplementaryPoints = approvedInError.HasValue && approvedInError.Value.ReasonSupplementaryPoints,
            ReasonOther = approvedInError.HasValue && approvedInError.Value.ReasonOther,
            ReasonExpiryDateText = approvedInError.HasValue ? approvedInError.Value.ReasonExpiryDateText : null,
            SupplementaryPointsText = approvedInError.HasValue ? approvedInError.Value.SupplementaryPointsText : woodlandOfficerReviewStatus.Value.SupplementaryPoints
        };

        var model = new ReApproveInErrorViewModel
        {
            Id = application.Value.Id,
            NewLicenceExpiryDate = approvedInError.HasValue && approvedInError.Value.LicenceExpiryDate.HasValue ? new DatePart(approvedInError.Value.LicenceExpiryDate.Value, "expiry-date") : null,
            CurrentLicenceExpiryDate = application.Value.LicenceExpiryDate.HasValue ? DateOnly.FromDateTime(application.Value.LicenceExpiryDate.Value) : null,
            CurrentSupplementaryPoints = woodlandOfficerReviewStatus.Value.SupplementaryPoints,
            ApprovedInErrorViewModel = approvedInErrorViewModel
        };

        var summary = await ExtractApplicationSummaryAsync(application.Value, cancellationToken);
        if (summary.IsFailure)
        {
            _logger.LogError("Application summary cannot be extracted, application id: {ApplicationId}, error {Error}", application.Value.Id, summary.Error);
            return Maybe<ReApproveInErrorViewModel>.None;
        }
        model.FellingLicenceApplicationSummary = summary.Value;

        return Maybe<ReApproveInErrorViewModel>.From(model);
    }

    /// <inheritdoc />
    public async Task<Result<Document>> ReApprovedInErrorAsync(
        Guid applicationId,
        InternalUser user,
        ApprovedInErrorModel model,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Re-approving application {ApplicationId} after correcting errors", applicationId);

        // Retrieve application for notification
        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            _logger.LogError("Failed to retrieve application {ApplicationId} for re-approval", applicationId);
            return Result.Failure<Document>("Unable to retrieve the application");
        }

        // Begin transaction for atomic operation
        await using var transaction = await _approverReviewUseCase.BeginTransactionAsync(cancellationToken);

        try
        {
            // Update the Approved In Error record with the corrected details
            var updateApprovedInErrorResult = await _approvedInErrorService.UpdateApprovedInErrorAsync(
                applicationId,
                model,
                user.UserAccountId!.Value,
                cancellationToken);

            if (updateApprovedInErrorResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update Approved In Error record for application with ID {ApplicationId}. Error: {Error}", applicationId, updateApprovedInErrorResult.Error);
                return Result.Failure<Document>($"Unable to update the approved in error record: {updateApprovedInErrorResult.Error}");
            }

            // Change the status to Approved after successful PDF generation
            await _updateFellingLicenceService.AddStatusHistoryAsync(
                user.UserAccountId.Value,
                applicationId,
                FellingLicenceStatus.Approved,
                cancellationToken);

            // Regenerate a final licence document for the approved application
            // This cannot be asynchronous, as the approval should not complete if the document generation fails
            var resultPdfGenerated = await _generatePdfApplicationUseCase.GeneratePdfApplicationAsync(
                user.UserAccountId.Value,
                applicationId,
                cancellationToken);

            if (resultPdfGenerated.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to generate licence document for application with ID {ApplicationId}. Error: {Error}", applicationId, resultPdfGenerated.Error);
                return Result.Failure<Document>($"Unable to generate the licence document for the application: {resultPdfGenerated.Error}");
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully re-approved application {ApplicationId} with updated licence expiry date", applicationId);

            // Send notification to applicant after successful re-approval
            _logger.LogDebug("Sending notification for status update of application with id {ApplicationId} to {RequestedStatus} to applicant", applicationId, FellingLicenceStatus.Approved);
            var applicantNotificationResult = await SendApplicantNotificationApprovedInErrorAsync(
                application.Value,
                NotificationType.InformApplicantOfAIENewLicenceApproved,
                cancellationToken);

            if (applicantNotificationResult.IsFailure)
            {
                _logger.LogError("Unable to send notification informing applicant of re-approval to {newStatus}, error {error}", FellingLicenceStatus.Approved, applicantNotificationResult.Error);
                // Note: This is non-blocking - we don't fail the entire operation if notification fails
            }

            // Audit the re-approval
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ReApprovedInError,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { model.LicenceExpiryDate, model.ReasonExpiryDate, model.ReasonSupplementaryPoints, model.ReasonOther, NotificationSent = applicantNotificationResult.IsSuccess }),
                cancellationToken);

            return Result.Success(resultPdfGenerated.Value);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Unexpected error while re-approving application {ApplicationId}", applicationId);
            return Result.Failure<Document>($"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task<Result> SendInternalApprovedInErrorNotifications(
        Guid applicationId,
        string applicationReference,
        InternalUser performingUser,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending approved in error notifications for application {ApplicationId}", applicationId);

        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            _logger.LogError("Failed to retrieve application {ApplicationId} for notifications", applicationId);
            return Result.Failure("Unable to retrieve application for sending notifications");
        }

        // Get the field manager (approver) who approved the application
        var approverId = application.Value.AssigneeHistories
            .Where(x => x.Role is AssignedUserRole.FieldManager)
            .MaxBy(x => x.TimestampAssigned)?.AssignedUserId;

        if (approverId == null)
        {
            _logger.LogWarning("No field manager found for application {ApplicationId}", applicationId);
            return Result.Failure("No field manager found for the application");
        }

        var approver = await _internalAccountService.GetUserAccountAsync(approverId.Value, cancellationToken);
        if (approver.HasNoValue)
        {
            _logger.LogError("Unable to retrieve approver user account id {id}", approverId.Value);
            return Result.Failure($"Unable to retrieve approver user account id {approverId.Value}");
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(application.Value.AdministrativeRegion)
            ? string.Empty
            : await GetConfiguredFcAreasService.TryGetAdminHubAddress(application.Value.AdministrativeRegion, cancellationToken);

        var approvedInError = await _approvedInErrorService.GetApprovedInErrorAsync(applicationId, cancellationToken);
        
        var reasons = new List<string>();
        if (approvedInError.HasValue)
        {
            if (approvedInError.Value.ReasonExpiryDate)
                reasons.Add("Expiry date incorrect");
            if (approvedInError.Value.ReasonSupplementaryPoints)
                reasons.Add("Supplementary points incorrect");
            if (approvedInError.Value.ReasonOther)
                reasons.Add("Other reason");
        }

        var reasonsText = reasons.Any() ? string.Join(", ", reasons) : "Not specified";

        var notificationModel = new InformFcStaffOfApplicationApprovedInErrorDataModel
        {
            PropertyName = application.Value.SubmittedFlaPropertyDetail?.Name ?? string.Empty,
            ApplicationReference = applicationReference,
            PreviousAssignedUserName = performingUser.FullName,
            PreviousAssignedEmailAddress = performingUser.EmailAddress,
            MarkedByName = performingUser.FullName,
            Reasons = reasonsText,
            CaseNote = approvedInError.HasValue ? approvedInError.Value.ReasonExpiryDateText : null,
            Name = approver.Value.FullName(false),
            ViewApplicationURL = $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{applicationId}",
            AdminHubFooter = adminHubFooter,
            ApplicationId = applicationId
        };

        var notificationResult = await _notificationsService.SendNotificationAsync(
            notificationModel,
            NotificationType.InformFcStaffOfApplicationApprovedInError,
            new NotificationRecipient(approver.Value.Email, approver.Value.FullName(false)),
            cancellationToken: cancellationToken);

        if (notificationResult.IsFailure)
        {
            _logger.LogError("Could not send approved in error notification to {Name}", approver.Value.FullName(false));
            return Result.Failure($"Could not send notification to {approver.Value.FullName(false)}");
        }

        _logger.LogDebug("Successfully sent approved in error notification for application {ApplicationId}", applicationId);
        return Result.Success();
    }

    private async Task<Result> SendApplicantNotificationApprovedInErrorAsync(
        Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application,
        NotificationType notificationType,
        CancellationToken cancellationToken)
    {
        var (_, applicantFailure, applicantUser) = await
            _externalAccountService.RetrieveUserAccountByIdAsync(application.CreatedById, cancellationToken);

        var (woodlandOwnerSuccess, _, woodlandOwner) = await
            WoodlandOwnerService.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, UserAccessModel.SystemUserAccessModel, cancellationToken);

        if (applicantFailure)
        {
            _logger.LogError("Unable to retrieve user account id {id}", application.CreatedById);
            return Result.Failure($"Unable to retrieve user account id {application.CreatedById}");
        }

        var applicantRecipient = new NotificationRecipient(applicantUser.Email, applicantUser.FullName);

        var internalUsers = await
            _internalAccountService.RetrieveUserAccountsByIdsAsync(application.AssigneeHistories
                    .Where(x => x.Role is not AssignedUserRole.Applicant && x.TimestampUnassigned == null)
                    .Select(x => x.AssignedUserId).ToList(),
                cancellationToken);

        if (internalUsers.IsFailure)
        {
            _logger.LogError("Unable to retrieve internal user accounts, error: {error}", internalUsers.Error);
            return Result.Failure($"Unable to internal user accounts for application with id {application.Id}");
        }

        var copyToRecipients = internalUsers.Value
            .Select(x => new NotificationRecipient(x.Email, x.FullName))
            .ToList();

        if (woodlandOwnerSuccess && woodlandOwner.ContactEmail != applicantUser.Email)
        {
            copyToRecipients.Add(new NotificationRecipient(woodlandOwner.ContactEmail!, woodlandOwner.ContactName));
        }

        string? approverName = null;
        string? approverEmail = null;

        var approverId = application.AssigneeHistories
            .Where(x => x.Role is AssignedUserRole.FieldManager)
            .MaxBy(x => x.TimestampAssigned)?.AssignedUserId;

        if (approverId != null)
        {
            var approver = await _internalAccountService
                .GetUserAccountAsync(approverId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (approver.HasNoValue)
            {
                _logger.LogError("Unable to retrieve approver user account id {id}", approverId.Value);
                return Result.Failure($"Unable to retrieve approver user account id {approverId.Value}");
            }

            approverName = approver.Value.FullName(false);
            approverEmail = approver.Value.Email;
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(application.AdministrativeRegion)
            ? string.Empty
            : await GetConfiguredFcAreasService
                .TryGetAdminHubAddress(application.AdministrativeRegion, cancellationToken)
                .ConfigureAwait(false);

        // Get the approved in error details to include reasons
        var approvedInError = await _approvedInErrorService.GetApprovedInErrorAsync(application.Id, cancellationToken);
        
        string? previousReference = null;
        string? approvedInErrorReasons = null;

        if (approvedInError.HasValue)
        {
            previousReference = approvedInError.Value.PreviousReference;
            
            var reasons = new List<string>();
            if (approvedInError.Value.ReasonExpiryDate)
                reasons.Add("the expiry date was incorrect");
            if (approvedInError.Value.ReasonSupplementaryPoints)
                reasons.Add("the supplementary points were incorrect");
            if (approvedInError.Value.ReasonOther)
                reasons.Add("other reasons");
            
            approvedInErrorReasons = reasons.Any() 
                ? string.Join(", ", reasons)
                : "administrative reasons";
        }

        // Find the latest approval date from StatusHistory
        string? approvedDate = null;
        var latestApprovalStatus = application.StatusHistories
            .Where(x => x.Status == FellingLicenceStatus.Approved)
            .OrderByDescending(x => x.Created)
            .FirstOrDefault();

        if (latestApprovalStatus != null)
        {
            approvedDate = latestApprovalStatus.Created.CreateFormattedDate();
        }

        var approvalModel = new InformApplicantOfAIEDataModel
        {
            ApplicationReference = previousReference,
            NewApplicationReference = application.ApplicationReference,
            ApprovedInErrorReasons = approvedInErrorReasons,
            Name = applicantUser.FullName,
            PropertyName = application.SubmittedFlaPropertyDetail?.Name,
            ApproverName = approverName,
            ViewApplicationURL = $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/SupportingDocumentation?applicationId={application.Id}",
            AdminHubFooter = adminHubFooter,
            ApplicationId = application.Id,
            ApprovedDate = approvedDate
        };

        return await _notificationsService.SendNotificationAsync(
            approvalModel,
            notificationType,
            applicantRecipient,
            copyToRecipients.ToArray(),
            cancellationToken: cancellationToken);
    }
}