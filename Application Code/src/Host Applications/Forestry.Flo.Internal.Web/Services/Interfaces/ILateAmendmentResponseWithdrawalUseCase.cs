using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;

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
    /// Withdraws applications whose amendment response deadlines have passed and remain WithApplicant / ReturnedToApplicant.
    /// </summary>
    /// <param name="withdrawFellingLicenceService">Withdrawal service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of successfully withdrawn applications.</returns>
    Task<int> WithdrawLateAmendmentApplicationsAsync(
        IWithdrawFellingLicenceService withdrawFellingLicenceService,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a notification to the applicant for a late amendment response.
    /// </summary>
    /// <param name="app">The application model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<CSharpFunctionalExtensions.Result> NotifyApplicantAsync(
        LateAmendmentResponseWithdrawalModel app,
        CancellationToken cancellationToken);
}