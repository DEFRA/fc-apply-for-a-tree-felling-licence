using Forestry.Flo.Services.FellingLicenceApplications.Services;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

public interface IAutomaticWithdrawalNotificationUseCase
{
    /// <summary>
    /// Processes applications that have exceeded the threshold for automatic withdrawal.
    /// </summary>
    /// <param name="viewFlaBaseUrl">The base URL for viewing felling licence applications.</param>
    /// <param name="withdrawFellingLicenceService">The service used to withdraw felling licence applications.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessApplicationsAsync(
        string viewFlaBaseUrl,
        IWithdrawFellingLicenceService withdrawFellingLicenceService,
        CancellationToken cancellationToken);
}
