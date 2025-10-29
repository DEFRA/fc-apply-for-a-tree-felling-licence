using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Services.Notifications.Services;

/// <summary>
/// Defined contract for a service that manages notification history entries.
/// </summary>
public interface INotificationHistoryService
{
    /// <summary>
    /// Retrieve notification history entries for a given application reference and optionally
    /// filtered by notification type.
    /// </summary>
    /// <param name="applicationId">The application ID to locate notification history entries for.</param>
    /// <param name="notificationTypesFilter">An optional array of <see cref="NotificationType"/> to filter the list by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="NotificationHistoryModel"/> instances that match the provided parameters.</returns>
    Task<Result<List<NotificationHistoryModel>>> RetrieveNotificationHistoryAsync(
        Guid applicationId,
        NotificationType[]? notificationTypesFilter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a list of NotificationHistoryModel entries to the NotificationHistory table, skipping models with already existing ExternalId.
    /// </summary>
    /// <param name="models">The list of NotificationHistoryModel to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of successfully added notifications.</returns>
    Task<Result> AddNotificationHistoryListAsync(IEnumerable<NotificationHistoryModel> models, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a notification history item by the item ID.
    /// </summary>
    /// <param name="id">Notification history id</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result containing the notification history model or error message.</returns>
    Task<Result<NotificationHistoryModel>> GetNotificationHistoryByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates only the response and status of a notification history item by the item ID, and sets last updated fields.
    /// </summary>
    /// <param name="id">Notification history id</param>
    /// <param name="status">The new status</param>
    /// <param name="response">The new response text</param>
    /// <param name="lastUpdatedById">The user who updated the record</param>
    /// <param name="lastUpdatedDate">The date/time of update</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdateResponseStatusByIdAsync(Guid id, NotificationStatus status, string? response, Guid lastUpdatedById, DateTime lastUpdatedDate, CancellationToken cancellationToken);
}