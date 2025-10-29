namespace Forestry.Flo.Internal.Web.Services.Interfaces
{
    /// <summary>
    /// Contract for sending voluntary withdrawal notifications for Felling Licence Applications.
    /// </summary>
    public interface IVoluntaryWithdrawalNotificationUseCase
    {
        /// <summary>
        /// Sends notifications to applicants for applications at "with applicant" status beyond the threshold.
        /// </summary>
        /// <param name="viewFLABaseURL">The base URL for viewing an application summary.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task SendNotificationForWithdrawalAsync(string viewFLABaseURL, CancellationToken cancellationToken);
    }
}
