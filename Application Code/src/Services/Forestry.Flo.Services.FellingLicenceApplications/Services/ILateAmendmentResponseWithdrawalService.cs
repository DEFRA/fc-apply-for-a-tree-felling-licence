using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Service for retrieving applications that are approaching (or have reached) the amendment response deadline
/// and require a reminder notification prior to possible automatic withdrawal.
/// </summary>
public interface ILateAmendmentResponseWithdrawalService
{
    /// <summary>
    /// Retrieves applications that have an active amendment review where the current time is greater than or equal to
    /// (ResponseDeadline - reminderPeriod) and a reminder notification has not yet been sent.
    /// Does NOT mutate state; callers must explicitly update the reminder timestamp once a notification is successfully sent.
    /// </summary>
    /// <param name="reminderPeriod">The period before the response deadline at which a reminder should be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A result containing the list of <see cref="LateAmendmentResponseWithdrawalModel"/> items prepared for notification.
    /// </returns>
    Task<Result<IList<LateAmendmentResponseWithdrawalModel>>> GetLateAmendmentResponseForReminderApplicationsAsync(
        TimeSpan reminderPeriod,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves applications whose active amendment review response deadline has passed (no response received / review not completed)
    /// and which are still WithApplicant / ReturnedToApplicant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of applications whose amendment response deadline has expired.</returns>
    Task<Result<IList<LateAmendmentResponseWithdrawalModel>>> GetLateAmendmentResponseForWithdrawalAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the ReminderNotificationTimeStamp on the specified amendment review (if not already set) and persists the change.
    /// </summary>
    /// <param name="applicationId">The application id owning the amendment review.</param>
    /// <param name="amendmentReviewId">The amendment review id to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> UpdateReminderNotificationTimeStampAsync(
        Guid applicationId,
        Guid amendmentReviewId,
        CancellationToken cancellationToken);
}