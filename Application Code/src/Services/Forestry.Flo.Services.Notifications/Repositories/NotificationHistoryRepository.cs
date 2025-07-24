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
        string applicationReference, 
        NotificationType[]? notificationTypesFilter,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrEmpty(applicationReference);

        if (notificationTypesFilter == null || notificationTypesFilter.Length == 0)
        {
            notificationTypesFilter = (NotificationType[])typeof(NotificationType).GetEnumValues();
        }

        var result = await _context
            .NotificationHistories
            .Where(x => x.ApplicationReference == applicationReference
                        && notificationTypesFilter.Contains(x.NotificationType))
            .ToListAsync(cancellationToken);

        return Result.Success(result);
    }
}