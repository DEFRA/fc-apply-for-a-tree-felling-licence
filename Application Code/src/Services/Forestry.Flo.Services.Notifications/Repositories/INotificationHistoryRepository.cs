using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Notifications.Entities;

namespace Forestry.Flo.Services.Notifications.Repositories;

public interface INotificationHistoryRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Adds a notification history record and saves it in the database
    /// </summary>
    /// <param name="notificationHistory"></param>
    /// <returns></returns>
    NotificationHistory Add(NotificationHistory notificationHistory);

    /// <summary>
    /// Gets a notification history item by the item ID
    /// </summary>
    /// <param name="id">Notification history id</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    Task<Result<NotificationHistory, UserDbErrorReason>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a set of notification history entries for a specific application reference and
    /// an optional set of notification types.
    /// </summary>
    /// <param name="applicationId">The application ID to retrieve notification history entries for.</param>
    /// <param name="notificationTypesFilter">An optional array of <see cref="NotificationType"/> to filter by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of matching notification history entries.</returns>
    Task<Result<List<NotificationHistory>>> GetNotificationHistoryForApplicationAsync(
        Guid applicationId,
        NotificationType[]? notificationTypesFilter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the external IDs that already exist in the notification history for the provided collection of external IDs.
    /// </summary>
    /// <param name="externalIds">The external IDs to check for existing records.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of existing external IDs.</returns>
    Task<List<Guid>> GetExistingExternalIdsAsync(IEnumerable<Guid> externalIds, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a notification history item by the item ID.
    /// </summary>
    /// <param name="id">Notification history id</param>
    /// <param name="update">Action to update the entity</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>Result with updated entity or error reason</returns>
    Task<Result<NotificationHistory, UserDbErrorReason>> UpdateByIdAsync(Guid id, Action<NotificationHistory> update, CancellationToken cancellationToken);
}