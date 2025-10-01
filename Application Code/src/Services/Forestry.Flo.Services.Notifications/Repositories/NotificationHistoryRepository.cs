using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Notifications.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.Notifications.Repositories;

public class NotificationHistoryRepository : INotificationHistoryRepository
{
    private readonly NotificationsContext _context;

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    public NotificationHistoryRepository(NotificationsContext context) =>
        _context = Guard.Against.Null(context);

    ///<inheritdoc />
    public NotificationHistory Add(NotificationHistory notificationHistory) => _context.NotificationHistories.Add(notificationHistory).Entity;

    ///<inheritdoc />
    public async Task<Result<NotificationHistory, UserDbErrorReason>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var notificationHistory = await _context
            .NotificationHistories
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);

        return notificationHistory == null
            ? Result.Failure<NotificationHistory, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<NotificationHistory, UserDbErrorReason>(notificationHistory);
    }

    ///<inheritdoc />
    public async Task<Result<List<NotificationHistory>>> GetNotificationHistoryForApplicationAsync(
        Guid applicationId, 
        NotificationType[]? notificationTypesFilter,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrEmpty(applicationId);

        if (notificationTypesFilter == null || notificationTypesFilter.Length == 0)
        {
            notificationTypesFilter = (NotificationType[])typeof(NotificationType).GetEnumValues();
        }

        var result = await _context
            .NotificationHistories
            .Where(x => x.ApplicationId == applicationId
                        && notificationTypesFilter.Contains(x.NotificationType))
            .ToListAsync(cancellationToken);

        return Result.Success(result);
    }

    /// <summary>
    /// Gets all existing ExternalIds from the NotificationHistory table for the provided list.
    /// </summary>
    /// <param name="externalIds">The list of external identifiers to check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of existing ExternalIds.</returns>
    public async Task<List<Guid>> GetExistingExternalIdsAsync(IEnumerable<Guid> externalIds, CancellationToken cancellationToken)
    {
        if (externalIds == null || !externalIds.Any()) return new List<Guid>();
        return await _context.NotificationHistories
            .Where(x => x.ExternalId.HasValue && externalIds.Contains(x.ExternalId.Value))
            .Select(x => x.ExternalId!.Value)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<NotificationHistory, UserDbErrorReason>> UpdateByIdAsync(Guid id, Action<NotificationHistory> update, CancellationToken cancellationToken)
    {
        var entity = await _context.NotificationHistories.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity == null)
            return Result.Failure<NotificationHistory, UserDbErrorReason>(UserDbErrorReason.NotFound);
        update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success<NotificationHistory, UserDbErrorReason>(entity);
    }
}