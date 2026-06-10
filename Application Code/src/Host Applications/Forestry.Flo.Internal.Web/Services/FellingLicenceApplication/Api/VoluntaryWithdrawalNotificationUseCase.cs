using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.HostApplicationsCommon.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

/// <summary>
/// Handles use case for sending a Notification to outstanding 'with applicant' application contacts
/// </summary>
public class VoluntaryWithdrawalNotificationUseCase : IVoluntaryWithdrawalNotificationUseCase
{
    private readonly IVoluntaryWithdrawalNotificationService _voluntaryWithdrawalNotificationService;
    private readonly IRetrieveUserAccountsService _externalAccountService;
    private readonly VoluntaryWithdrawalNotificationOptions _notificationOptions;
    private readonly ISendNotifications _sendNotifications;
    private readonly ILogger<VoluntaryWithdrawalNotificationUseCase> _logger;
    private readonly IGetPropertyProfiles _getPropertyProfilesService;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService;
    private readonly IClock _clock;
    private readonly RequestContext _requestContext;
    private readonly IAuditService<VoluntaryWithdrawalNotificationUseCase> _auditService;
    private readonly ExternalApplicantSiteOptions _externalApplicantSiteOptions;
    private readonly IRetrieveWoodlandOwners _woodlandOwnersService;

    /// <summary>
    /// Creates a new instance of the <see cref="VoluntaryWithdrawalNotificationUseCase"/>.
    /// </summary>
    /// <param name="clock">A clock to get the current date and time.</param>
    /// <param name="voluntaryWithdrawalNotificationService">A service to perform voluntary withdrawal notification operations.</param>
    /// <param name="notificationOptions">A configuration class for notifications.</param>
    /// <param name="logger">A logging instance.</param>
    /// <param name="externalAccountService">A service to retrieve external user accounts.</param>
    /// <param name="sendNotifications">A service to send notifications.</param>
    /// <param name="requestContext">The request context.</param>
    /// <param name="auditService">An auditing service.</param>
    /// <param name="externalApplicantSiteOptions">Configuration options for the external applicant interface.</param>
    /// <param name="woodlandOwnersService">A service to retrieve woodland owner details.</param>
    /// <param name="getPropertyProfilesService">A service to retrieve property profiles.</param>
    /// <param name="getConfiguredFcAreasService">A service to retrieve admin hub details.</param>
    public VoluntaryWithdrawalNotificationUseCase(
        IClock clock,
        IVoluntaryWithdrawalNotificationService voluntaryWithdrawalNotificationService,
        IOptions<VoluntaryWithdrawalNotificationOptions> notificationOptions,
        ILogger<VoluntaryWithdrawalNotificationUseCase> logger,
        IRetrieveUserAccountsService externalAccountService,
        ISendNotifications sendNotifications,
        RequestContext requestContext,
        IAuditService<VoluntaryWithdrawalNotificationUseCase> auditService,
        IOptions<ExternalApplicantSiteOptions> externalApplicantSiteOptions,
        IRetrieveWoodlandOwners woodlandOwnersService,
        IGetPropertyProfiles getPropertyProfilesService,
        IGetConfiguredFcAreas getConfiguredFcAreasService)
    {
        _voluntaryWithdrawalNotificationService = Guard.Against.Null(voluntaryWithdrawalNotificationService);
        _logger = logger;
        _getPropertyProfilesService = Guard.Against.Null(getPropertyProfilesService);
        _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
        _notificationOptions = Guard.Against.Null(notificationOptions).Value;
        _clock = Guard.Against.Null(clock);
        _externalAccountService = Guard.Against.Null(externalAccountService);
        _sendNotifications = Guard.Against.Null(sendNotifications);
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _externalApplicantSiteOptions = Guard.Against.Null(externalApplicantSiteOptions).Value;
        _woodlandOwnersService = Guard.Against.Null(woodlandOwnersService);
    }

	/// <inheritdoc/>
	public async Task SendNotificationForWithdrawalAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to extend final action date for applications that have exceeded this date, are still in review and have not been previously extended");

        var (_, isFailure, relevantApplications, error) = 
            await _voluntaryWithdrawalNotificationService.GetApplicationsAfterThresholdForWithdrawalAsync(
                _notificationOptions.ThresholdAfterWithApplicantStatusDate,
        cancellationToken);

        var currentDate = _clock.GetCurrentInstant().ToDateTimeUtc();

        if (isFailure)
        {
            _logger.LogError("Unable to update applications' Voluntary Withdrawal Notification TimeStamp , error: {Error}",  error);
            return;
        }

        foreach (var application in relevantApplications)
        {
			await InformApplicantOfApplicationVoluntaryWithdrawalOption(
                application,
                currentDate,
                cancellationToken);

			await
                _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.ApplicationVoluntaryWithdrawalNotification,
                        application.ApplicationId,
                        null,
                        _requestContext,
                        new
                        {
                            application.ApplicationId,
                            DaysInWithApplicant = currentDate.Subtract(application.WithApplicantDate).Days,
							NotificationSentDate = currentDate.Date
						}),
                    cancellationToken);
        }
    }

    private async Task InformApplicantOfApplicationVoluntaryWithdrawalOption(
        VoluntaryWithdrawalNotificationModel application,
        DateTime currentDate,
        CancellationToken cancellationToken)
    {
        var (_, createdByUserFailure, createdByUser, error) = await _externalAccountService.RetrieveUserAccountByIdAsync(
            application.CreatedById,
            cancellationToken);

        var (woodlandOwnerSuccess, _, woodlandOwner) =
            await _woodlandOwnersService.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, UserAccessModel.SystemUserAccessModel, cancellationToken);

        if (createdByUserFailure)
        {
            _logger.LogError("Unable to retrieve applicant user account of id {ApplicantId}, error: {Error}", application.CreatedById, error);
            return;
        }
        
        var adminHubFooter = string.IsNullOrWhiteSpace(application.AdministrativeRegion)
            ? string.Empty
            : await _getConfiguredFcAreasService
                .TryGetAdminHubAddress(application.AdministrativeRegion, cancellationToken)
                .ConfigureAwait(false);

        var property = await _getPropertyProfilesService
            .GetPropertyByIdAsync(application.LinkedPropertyProfileId, UserAccessModel.SystemUserAccessModel, cancellationToken)
            .ConfigureAwait(false);
        if (property.IsFailure)
        {
            _logger.LogError("Could not retrieve property {PropertyProfileId} for application {ApplicationId}",
                application.LinkedPropertyProfileId, application.ApplicationId);
        }

        var propertyName = property.IsSuccess
            ? property.Value.Name
            : null;

        var applicantNotificationModel = new InformApplicantOfApplicationVoluntaryWithdrawOptionDataModel
		{
            ApplicationReference = application.ApplicationReference,
            PropertyName = propertyName,
            DaysInWithApplicantStatus = currentDate.Subtract(application.WithApplicantDate).Days,
            WithApplicantDateTime = DateTimeDisplay.GetDateDisplayString(application.WithApplicantDate),
            ResubmissionDeadline = DateTimeDisplay.GetDateDisplayString(application.WithApplicantDate.Add(_notificationOptions.ThresholdAutomaticWithdrawal)),
            Name = createdByUser.FullName,
            ViewApplicationURL = $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={application.ApplicationId}",
            WithdrawApplicationURL = $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ConfirmWithdrawFellingLicenceApplication?applicationId={application.ApplicationId}",
            AdminHubFooter = adminHubFooter,
            ApplicationId = application.ApplicationId
        };

        var applicantRecipient =
            new NotificationRecipient(createdByUser.Email, applicantNotificationModel.Name);

        var notificationResult = await _sendNotifications.SendNotificationAsync(
            applicantNotificationModel,
            NotificationType.InformApplicantOfApplicationVoluntaryWithdrawOption,
            applicantRecipient,
            woodlandOwnerSuccess && woodlandOwner.ContactEmail != createdByUser.Email 
                ? [new NotificationRecipient(woodlandOwner.ContactEmail!, woodlandOwner.ContactName)]
                : null,
            cancellationToken: cancellationToken);

        if (notificationResult.IsFailure)
        {
            _logger.LogError("Unable to send voluntary withdrawal notification to {Recipient}, error: {Error}",
                applicantRecipient.Address,
                notificationResult.Error);
        }
    }
}