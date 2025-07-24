using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for services that returns applications that require a notification that the application has been with them for more than a threshold timespan.
/// </summary>
public interface IVoluntaryWithdrawalNotificationService
{
    /// <summary>
    /// Provides list of applications for sending the notification to the applicants and woodland owner that the threshold has passed and they can withdraw the application
    /// </summary> 
    /// <param name="ThresholdAfterStatusCreatedDate">The time prior to the status updated to 'with applicant' status that notification be sent to applicant and woodland owner.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing a list of all applications that have passed the threshold after application set to 'with application' status and not already sent a notification, for sending of notification.</returns>
    Task<Result<IList<VoluntaryWithdrawalNotificationModel>>> GetApplicationsAfterThresholdForWithdrawalAsync(
        TimeSpan ThresholdAfterStatusCreatedDate,
        CancellationToken cancellationToken);
}