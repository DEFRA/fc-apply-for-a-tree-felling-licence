using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

/// <summary>
/// Handles use case for extending an application
/// </summary>
public class ExtendApplicationsUseCase : IExtendApplicationsUseCase
{
    private readonly IExtendApplications _applicationExtensionService;
    private readonly IUserAccountService _internalAccountService;
    private readonly IRetrieveUserAccountsService _externalAccountService;
    private readonly ApplicationExtensionOptions _extensionOptions;
    private readonly ISendNotifications _sendNotifications;
    private readonly ILogger<ExtendApplicationsUseCase> _logger;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService;
    private readonly IClock _clock;
    private readonly RequestContext _requestContext;
    private readonly IAuditService<ExtendApplicationsUseCase> _auditService;
    private readonly ExternalApplicantSiteOptions _externalApplicantSiteOptions;
    private readonly IRetrieveWoodlandOwners _woodlandOwnersService;

    public ExtendApplicationsUseCase(
        IClock clock,
        IExtendApplications applicationExtensionService,
        IOptions<ApplicationExtensionOptions> extensionOptions,
        ILogger<ExtendApplicationsUseCase> logger,
        IUserAccountService internalAccountService,
        IRetrieveUserAccountsService externalAccountService,
        ISendNotifications sendNotifications,
        RequestContext requestContext,
        IAuditService<ExtendApplicationsUseCase> auditService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IOptions<ExternalApplicantSiteOptions> externalApplicantSiteOptions,
        IRetrieveWoodlandOwners woodlandOwnersService)
    {
        _applicationExtensionService = Guard.Against.Null(applicationExtensionService);
        _logger = logger;
        _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
        _extensionOptions = Guard.Against.Null(extensionOptions).Value;
        _clock = Guard.Against.Null(clock);
        _internalAccountService = Guard.Against.Null(internalAccountService);
        _externalAccountService = Guard.Against.Null(externalAccountService);
        _sendNotifications = Guard.Against.Null(sendNotifications);
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _externalApplicantSiteOptions = Guard.Against.Null(externalApplicantSiteOptions).Value;
        _woodlandOwnersService = Guard.Against.Null(woodlandOwnersService);
    }

    /// <summary>
    /// Extends final action date for applications still in a review state if the date has been reached and hasn't been previously extended.
    /// </summary>
    /// <param name="viewFLABaseURL">The base URL for viewing an application summary on the internal app.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>This method is automatically executed from an API controller.</remarks>

    // ReSharper disable once InconsistentNaming
    public async Task ExtendApplicationFinalActionDatesAsync(string viewFLABaseURL, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to extend final action date for applications that have exceeded this date, are still in review and have not been previously extended");

        var (_, isFailure, relevantApplications, error) = 
            await _applicationExtensionService.ApplyApplicationExtensionsAsync(
                _extensionOptions.ExtensionLength,
                _extensionOptions.ThresholdBeforeFinalActionDate,
                cancellationToken);

        var currentDate = _clock.GetCurrentInstant().ToDateTimeUtc();

        if (isFailure)
        {
            _logger.LogError("Unable to update applications' final action dates, error: {Error}",  error);
        }

        foreach (var application in relevantApplications)
        {
            await InformApplicantOfExtensionAsync(
                application,
                cancellationToken);

            var notificationsSent = await InformFcStaffMembersOfDeadlineExceededAsync(
                application,
                viewFLABaseURL,
                currentDate,
                cancellationToken);

            await
                _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.ApplicationExtensionNotification,
                        application.ApplicationId,
                        null,
                        _requestContext,
                        new
                        {
                            application.FinalActionDate,
                            PreviousActionDate = application.FinalActionDate.Subtract(application.ExtensionLength ?? TimeSpan.Zero),
                            ApplicationExtended = application.FinalActionDate > currentDate,
                            NumberOfFcStaffNotificationRecipients = notificationsSent
                        }),
                    cancellationToken);
        }
    }

    private async Task InformApplicantOfExtensionAsync(
        ApplicationExtensionModel application,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.FellingLicenceApplicationFinalActionDateUpdated,
                application.ApplicationId,
                null,
                _requestContext,
                new
                {
                    SubmittedDate = application.SubmissionDate,
                    application.FinalActionDate
                }),
            cancellationToken);

        var (_, createdByUserFailure, createdByUser, error) = await _externalAccountService.RetrieveUserAccountByIdAsync(
            application.CreatedById,
            cancellationToken);

        var (woodlandOwnerSuccess, _, woodlandOwner) =
            await _woodlandOwnersService.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, cancellationToken);

        if (createdByUserFailure)
        {
            _logger.LogError("Unable to retrieve applicant user account of id {ApplicantId}, error: {Error}", application.CreatedById, error);
            return;
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(application.AdminHubName)
            ? string.Empty
            : await _getConfiguredFcAreasService
                .TryGetAdminHubAddress(application.AdminHubName, cancellationToken)
                .ConfigureAwait(false);

        var applicantNotificationModel = new InformApplicantOfApplicationExtensionDataModel
        {
            ApplicationReference = application.ApplicationReference,
            ExtensionLength = (application.ExtensionLength ?? TimeSpan.Zero).Days,
            FinalActionDate = DateTimeDisplay.GetDateDisplayString(application.FinalActionDate),
            Name = createdByUser.FullName,
            ViewApplicationURL = $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={application.ApplicationId}",
            AdminHubFooter = adminHubFooter,
            ApplicationId = application.ApplicationId
        };

        var applicantRecipient =
            new NotificationRecipient(createdByUser.Email, applicantNotificationModel.Name);

        var notificationResult = await _sendNotifications.SendNotificationAsync(
            applicantNotificationModel,
            NotificationType.InformApplicantOfApplicationExtension,
            applicantRecipient,
            woodlandOwnerSuccess && woodlandOwner.ContactEmail != createdByUser.Email 
                ? new []{new NotificationRecipient(woodlandOwner.ContactEmail!, woodlandOwner.ContactName)}
                : null,
            cancellationToken: cancellationToken);

        if (notificationResult.IsFailure)
        {
            _logger.LogError("Unable to send notification of extension to {Recipient}, error: {Error}",
                applicantRecipient.Address,
                notificationResult.Error);
        }
    }

    // ReSharper disable once InconsistentNaming
    private async Task<int> InformFcStaffMembersOfDeadlineExceededAsync(
        ApplicationExtensionModel application,
        string viewFLABaseURL,
        DateTime currentDate,
        CancellationToken cancellationToken)
    {
        var (_, fcStaffUserFailure, assignedUsers, error) = await _internalAccountService.RetrieveUserAccountsByIdsAsync(
            application.AssignedFCUserIds.ToList(),
            cancellationToken);

        if (fcStaffUserFailure)
        {
            _logger.LogError("Unable to retrieve FC staff user accounts with ids {FcIds}, error: {Error}", string.Join(", ", application.AssignedFCUserIds.Select(x => x.ToString())), error);
            return 0;
        }

        var adminHubFooter = string.IsNullOrWhiteSpace(application.AdminHubName)
            ? string.Empty
            : await _getConfiguredFcAreasService
                .TryGetAdminHubAddress(application.AdminHubName, cancellationToken)
                .ConfigureAwait(false);

        var notificationsSent = 0;

        foreach (var assignedUser in assignedUsers)
        {
            var fcNotificationModel = new InformFCStaffOfFinalActionDateReachedDataModel
            {
                ApplicationReference = application.ApplicationReference!,
                ExtensionLength = application.ExtensionLength?.Days,
                FinalActionDate = DateTimeDisplay.GetDateDisplayString(application.FinalActionDate),
                Name = assignedUser.FullName,
                ViewApplicationURL = $"{viewFLABaseURL}/{application.ApplicationId}",
                DaysUntilFinalActionDate = (currentDate - application.FinalActionDate).Days,
                AdminHubFooter = adminHubFooter,
                ApplicationId = application.ApplicationId
            };

            var fcRecipient = new NotificationRecipient(assignedUser.Email, fcNotificationModel.Name);

            var notificationResult = 
                await _sendNotifications.SendNotificationAsync(
                        fcNotificationModel,
                        NotificationType.InformFCStaffOfFinalActionDateReached,
                        fcRecipient,
                        cancellationToken: cancellationToken)
                    .Map(() => notificationsSent++);

            if (notificationResult.IsFailure)
            {
                _logger.LogError("Unable to send notification of final action date exceeded to {Recipient} with role {Role}, error: {Error}",
                    fcRecipient.Address,
                    assignedUser.AccountType,
                    notificationResult.Error);
            }
        }

        return notificationsSent;
    }
}