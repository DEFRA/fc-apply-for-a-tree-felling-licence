namespace Forestry.Flo.Internal.Web.Services.Interfaces;

public interface ILateAmendmentResponseWithdrawalUseCase
{
    /// <summary>
    /// Sends reminder notifications for applications within the reminder window and returns the count successfully sent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of reminder notifications successfully sent (and persisted).</returns>
    Task<int> SendLateAmendmentResponseRemindersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Withdraws applications whose amendment response deadlines have passed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of successfully withdrawn applications.</returns>
    Task<int> WithdrawLateAmendmentApplicationsAsync(
        CancellationToken cancellationToken);
}