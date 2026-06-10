using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.HostApplicationsCommon.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

/// <summary>
/// Handles use case for automatic withdrawal and notifications to outstanding 'with applicant' applications 
/// </summary>
public class AutomaticWithdrawalNotificationUseCase(
    IWithdrawalNotificationService withdrawalNotificationService,
    IOptions<VoluntaryWithdrawalNotificationOptions> notificationOptions,
    IOptions<InternalUserSiteOptions> internalUserSiteOptions,
    ILogger<AutomaticWithdrawalNotificationUseCase> logger,
    IWithdrawApplicationInternalUseCase withdrawApplicationInternalUseCase) : IAutomaticWithdrawalNotificationUseCase
{
    private readonly IWithdrawalNotificationService _withdrawalNotificationService = Guard.Against.Null(withdrawalNotificationService);
    private readonly VoluntaryWithdrawalNotificationOptions _notificationOptions = Guard.Against.Null(notificationOptions).Value;
    private readonly IWithdrawApplicationInternalUseCase _withdrawApplicationInternalUseCase = Guard.Against.Null(withdrawApplicationInternalUseCase);
    private readonly InternalUserSiteOptions _internalUserSiteOptions = Guard.Against.Null(internalUserSiteOptions).Value;

    /// <inheritdoc/>
    public async Task ProcessApplicationsAsync(
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Attempting automatic withdrawal of applications that have exceeded ThresholdAutomaticWithdrawal: {ThresholdAutomaticWithdrawal}",
            _notificationOptions.ThresholdAutomaticWithdrawal);

        var (_, isFailure, relevantApplications, error) =
            await _withdrawalNotificationService.GetApplicationsAfterThresholdForWithdrawalAsync(
                _notificationOptions.ThresholdAutomaticWithdrawal,
                cancellationToken);

        if (isFailure)
        {
            logger.LogError("Unable to retrieve applications for withdrawal, error: {Error}", error);
            return;
        }

        logger.LogDebug("Automatic withdrawal of {WithdrawnApplications} applications", relevantApplications.Count);

        foreach (var application in relevantApplications)
        {
            var linkToApplication = $"{_internalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{application.ApplicationId}";
            var withdrawApplicationResult = await _withdrawApplicationInternalUseCase.WithdrawApplicationAsync(
                    application.ApplicationId, WithdrawalReason.ExceededResubmitDeadline, linkToApplication,
                    cancellationToken)
                .ConfigureAwait(false);

            if (withdrawApplicationResult.IsFailure)
            {
                logger.LogError("Failed to withdraw application {ApplicationId}, error: {Error}", 
                    application.ApplicationId, withdrawApplicationResult.Error);
            }
        }

    }
}