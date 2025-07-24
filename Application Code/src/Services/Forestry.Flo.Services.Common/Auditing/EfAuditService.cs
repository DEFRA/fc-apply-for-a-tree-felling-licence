using Forestry.Flo.Services.Common.Auditing.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Text.Json;

namespace Forestry.Flo.Services.Common.Auditing;

/// <summary>
/// An implementation of <see cref="IAuditService{T}"/> that stores audit events using EntityFramework.
/// </summary>
public class EfAuditService<T> : IAuditService<T>
{
    private readonly IDbContextFactory<AuditDataContext> _dbContextFactory;
    private readonly IClock _clock;

    /// <summary>
    /// Creates a new instance of <see cref="EfAuditService{T}"/>.
    /// </summary>
    /// <param name="dbContextFactory">The Ef database context factory with which to store the audit events.</param>
    /// <param name="clock">An implementation of <see cref="IClock"/> to get the current time.</param>
    public EfAuditService(IDbContextFactory<AuditDataContext> dbContextFactory, IClock clock)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task PublishAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var entity = new AuditEventEntity(auditEvent, typeof(T).Name)
        {
            EffectiveTime = _clock.GetCurrentInstant().ToDateTimeOffset()
        };
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.AuditEvents.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<object?> GetFirstEventAuditDataAsync(string eventName, Guid? sourceEntityId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var auditEvent = await dbContext.AuditEvents
            .Where(ae => ae.EventName == eventName && ae.SourceEntityId == sourceEntityId)
            .OrderBy(ae => ae.EffectiveTime)
            .FirstOrDefaultAsync(cancellationToken);
        return auditEvent?.AuditData != null ? JsonSerializer.Deserialize<object>(auditEvent.AuditData) : null;
    }
}