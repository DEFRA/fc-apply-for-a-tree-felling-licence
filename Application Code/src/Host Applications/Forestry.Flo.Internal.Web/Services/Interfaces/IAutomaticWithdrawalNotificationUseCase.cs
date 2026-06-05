namespace Forestry.Flo.Internal.Web.Services.Interfaces;

public interface IAutomaticWithdrawalNotificationUseCase
{
    /// <summary>
    /// Processes applications that have exceeded the threshold for automatic withdrawal.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessApplicationsAsync(
        CancellationToken cancellationToken);
}
