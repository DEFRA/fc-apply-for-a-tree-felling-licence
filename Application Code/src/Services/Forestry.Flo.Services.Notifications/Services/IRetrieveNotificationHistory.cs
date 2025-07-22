using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;

namespace Forestry.Flo.Services.Notifications.Services;

/// <summary>
/// Defined contract for a service that retrieves notification history entries.
/// </summary>
public interface IRetrieveNotificationHistory
{
    /// <summary>
    /// Retrieve notification history entries for a given application reference and optionally
    /// filtered by notification type.
    /// </summary>
    /// <param name="applicationReference">The application reference to locate notification history entries for.</param>
    /// <param name="notificationTypesFilter">An optional array of <see cref="NotificationType"/> to filter the list by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="NotificationHistoryModel"/> instances that match the provided parameters.</returns>
    Task<Result<List<NotificationHistoryModel>>> RetrieveNotificationHistoryAsync(
        string applicationReference,
        NotificationType[]? notificationTypesFilter,
        CancellationToken cancellationToken);
}