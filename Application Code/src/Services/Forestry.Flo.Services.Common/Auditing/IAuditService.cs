namespace Forestry.Flo.Services.Common.Auditing;

/// <summary>
/// Defines the contract for a service that implements auditing.
/// </summary>
public interface IAuditService<T>
{
    /// <summary>
    /// Stores an audit event with the given details.
    /// </summary>
    /// <param name="auditEvent">An instance encapsulating data to be saved within the system audit event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An awaitable <see cref="Task"/>.</returns>
    Task PublishAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the deserialized audit data of the first event with the given name.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="sourceEntityId">The id of the domain entity that the audit event is pertinent to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized audit data of the first event, or null if no event is found.</returns>
    Task<object?> GetFirstEventAuditDataAsync(string eventName, Guid? sourceEntityId, CancellationToken cancellationToken = default);
}
