using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models; // added for UserAccessModel
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

public class LateAmendmentResponseWithdrawalUseCase : ILateAmendmentResponseWithdrawalUseCase
{
    private static readonly TimeSpan ReminderPeriod = TimeSpan.FromDays(7);
    private readonly ILateAmendmentResponseWithdrawalService _lateAmendmentService;
    private readonly IRetrieveUserAccountsService _externalAccountService;
    private readonly ISendNotifications _sendNotifications;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService;
    private readonly IClock _clock;
    private readonly RequestContext _requestContext;
    private readonly IAuditService<LateAmendmentResponseWithdrawalUseCase> _auditService;
    private readonly ExternalApplicantSiteOptions _externalApplicantSiteOptions;
    private readonly ILogger<LateAmendmentResponseWithdrawalUseCase> _logger;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationRepository;
    private readonly IUserAccountService _internalUserAccountService;

    public LateAmendmentResponseWithdrawalUseCase(
        ILateAmendmentResponseWithdrawalService lateAmendmentService,
        IRetrieveUserAccountsService externalAccountService,
        ISendNotifications sendNotifications,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IClock clock,
        RequestContext requestContext,
        IAuditService<LateAmendmentResponseWithdrawalUseCase> auditService,
        IOptions<ExternalApplicantSiteOptions> externalApplicantSiteOptions,
        ILogger<LateAmendmentResponseWithdrawalUseCase> logger,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationRepository,
        IUserAccountService internalUserAccountService)
    {
        _lateAmendmentService = Guard.Against.Null(lateAmendmentService);
        _externalAccountService = Guard.Against.Null(externalAccountService);
        _sendNotifications = Guard.Against.Null(sendNotifications);
        _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
        _clock = Guard.Against.Null(clock);
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _externalApplicantSiteOptions = Guard.Against.Null(externalApplicantSiteOptions).Value;
        _logger = Guard.Against.Null(logger);
        _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
        _internalUserAccountService = Guard.Against.Null(internalUserAccountService);
    }

    /// <summary>
    /// Sends reminder notifications for applications within the reminder window and returns the count successfully sent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of reminder notifications successfully sent (and persisted).</returns>
    public async Task<int> SendLateAmendmentResponseRemindersAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to send amendment response reminder notifications (period {Period})", ReminderPeriod);

        var result = await _lateAmendmentService
            .GetLateAmendmentResponseForReminderApplicationsAsync(ReminderPeriod, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed retrieving applications requiring amendment response reminder notifications. Error: {Error}", result.Error);
            await AuditFailure(null, cancellationToken);
            return 0;
        }

        var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();
        var successCount = 0;

        foreach (var app in result.Value)
        {
            await using var transaction = await _fellingLicenceApplicationRepository.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var updateResult = await _lateAmendmentService.UpdateReminderNotificationTimeStampAsync(app.ApplicationId, app.AmendmentReviewId, cancellationToken);
            if (updateResult.IsFailure)
            {
                _logger.LogError("Failed to persist reminder timestamp for application {ApplicationId} review {ReviewId}: {Error}", app.ApplicationId, app.AmendmentReviewId, updateResult.Error);
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                await AuditFailure(app, cancellationToken);
                continue;
            }

            var notifyResult = await NotifyApplicantAsync(app, cancellationToken);
            if (notifyResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                await AuditFailure(app, cancellationToken);
                continue;
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            successCount++;

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.LateAmendmentResponseNotification,
                    app.ApplicationId,
                    null,
                    _requestContext,
                    new
                    {
                        app.ApplicationId,
                        app.ResponseDeadline
                    }),
                cancellationToken);
        }

        _logger.LogInformation("Sent {SuccessCount} amendment response reminder notification(s)", successCount);
        return successCount;
    }

    /// <summary>
    /// Withdraws applications whose amendment response deadlines have passed and remain WithApplicant / ReturnedToApplicant.
    /// </summary>
    /// <param name="withdrawFellingLicenceService">Withdrawal service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of successfully withdrawn applications.</returns>
    public async Task<int> WithdrawLateAmendmentApplicationsAsync(
        IWithdrawFellingLicenceService withdrawFellingLicenceService,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting withdrawal of late amendment applications");

        var result = await _lateAmendmentService.GetLateAmendmentResponseForWithdrawalAsync(cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError("Failed retrieving applications for late amendment withdrawal. Error: {Error}", result.Error);
            await AuditFailure(null, cancellationToken);
            return 0;
        }

        var applications = result.Value;
        var successCount = 0;
        var userAccess = new UserAccessModel { IsFcUser = true };
        var currentDate = _clock.GetCurrentInstant().ToDateTimeUtc();

        foreach (var app in applications)
        {
            await using var transaction = await _fellingLicenceApplicationRepository.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var withdrawalResult = await withdrawFellingLicenceService.WithdrawApplication(app.ApplicationId, userAccess, cancellationToken).ConfigureAwait(false);
                if (withdrawalResult.IsFailure)
                {
                    _logger.LogError("Unable to withdraw application {ApplicationId} (Late amendment). Error: {Error}", app.ApplicationId, withdrawalResult.Error);
                    await _auditService.PublishAuditEventAsync(new AuditEvent(
                            AuditEvents.WithdrawFellingLicenceApplicationFailure,
                            app.ApplicationId,
                            null,
                            _requestContext,
                            new { Section = "Late Amendment Withdrawal", withdrawalResult.Error }),
                        cancellationToken);
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Mark the active amendment review as completed
                var reviewCompleteResult = await _fellingLicenceApplicationRepository.SetAmendmentReviewCompletedAsync(
                    app.AmendmentReviewId,
                    true,
                    cancellationToken);
                if (reviewCompleteResult.IsFailure)
                {
                    _logger.LogError("Failed to set amendment review {ReviewId} completed for application {ApplicationId}. Error: {Error}", app.AmendmentReviewId, app.ApplicationId, reviewCompleteResult.Error);
                    await _auditService.PublishAuditEventAsync(new AuditEvent(
                            AuditEvents.WithdrawFellingLicenceApplicationFailure,
                            app.ApplicationId,
                            null,
                            _requestContext,
                            new { Section = "Late Amendment Withdrawal - Complete Review", reviewCompleteResult.Error }),
                        cancellationToken);
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.WithdrawFellingLicenceApplication,
                        app.ApplicationId,
                        null,
                        _requestContext,
                        new { app.ApplicationId, WithdrawalDate = currentDate.Date }),
                    cancellationToken);

                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception withdrawing late amendment application {ApplicationId}", app.ApplicationId);
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.WithdrawFellingLicenceApplicationFailure,
                        app.ApplicationId,
                        null,
                        _requestContext,
                        new { Section = "Late Amendment Withdrawal", Error = ex.Message }),
                    cancellationToken);
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Withdrawn {SuccessCount} late amendment application(s)", successCount);
        return successCount;
    }

    public async Task<Result> NotifyApplicantAsync(
        LateAmendmentResponseWithdrawalModel app,
        CancellationToken cancellationToken)
    {
        var (_, applicantFailure, applicant, applicantError) = await _externalAccountService
            .RetrieveUserAccountByIdAsync(app.CreatedById, cancellationToken);

        if (applicantFailure)
        {
            _logger.LogError("Unable to retrieve applicant user account {ApplicantId} for amendment response reminder. Error: {Error}", app.CreatedById, applicantError);
            return Result.Failure("Applicant retrieval failed");
        }

        string woName = string.Empty;
        if (app.WoodlandOfficerReviewLastUpdatedById != Guid.Empty)
        {
            var internalWo = await _internalUserAccountService.GetUserAccountAsync(app.WoodlandOfficerReviewLastUpdatedById, cancellationToken);
            if (internalWo.HasValue)
            {
                woName = internalWo.Value.FullName(false);
            }
            else
            {
                _logger.LogWarning("Could not retrieve internal WO user {WoId} for amendment reminder on application {AppId}", app.WoodlandOfficerReviewLastUpdatedById, app.ApplicationId);
            }
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(app.AdministrativeRegion)
            ? string.Empty
            : await _getConfiguredFcAreasService.TryGetAdminHubAddress(app.AdministrativeRegion, cancellationToken)
                .ConfigureAwait(false);

        var model = new AmendmentsSentToApplicantDataModel
        {
            Name = applicant.FullName,
            ApplicationReference = app.ApplicationReference ?? string.Empty,
            PropertyName = app.PropertyName ?? "n/a",
            ApplicationId = app.ApplicationId,
            ResponseDeadline = DateTimeDisplay.GetDateDisplayString(app.ResponseDeadline),
            WoName = woName,
            ViewApplicationURL = $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={app.ApplicationId}",
            AdminHubFooter = adminHubFooter
        };

        var recipient = new NotificationRecipient(applicant.Email, model.Name);

        var sendResult = await _sendNotifications.SendNotificationAsync(
            model,
            NotificationType.ReminderForApplicantToRespondToAmendments,
            recipient,
            cancellationToken: cancellationToken);

        if (sendResult.IsFailure)
        {
            _logger.LogError("Failed to send amendment response reminder for application {ApplicationId} to {Recipient}. Error: {Error}", app.ApplicationId, recipient.Address, sendResult.Error);
            return Result.Failure(sendResult.Error);
        }

        return Result.Success();
    }
    private async Task AuditFailure(LateAmendmentResponseWithdrawalModel? app, CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.LateAmendmentResponseNotificationFailure,
            app?.ApplicationId,
            null,
            _requestContext,
            new
            {
                app?.ApplicationId,
                app?.ResponseDeadline
            }),
        cancellationToken);
    }

}