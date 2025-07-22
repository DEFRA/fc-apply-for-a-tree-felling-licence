using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Common.Auditing;

/// <summary>
/// Defines the properties of an audit event capturing pertinent information during runtime operations.
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Gets the name of the event.
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// Gets the id of the user that triggered this event, if it is not a system-initiated event.
    /// </summary>
    public Guid? UserId { get; }

    /// <summary>
    /// Gets the type of the actor that triggered this event.
    /// </summary>
    public ActorType ActorType { get; }

    /// <summary>
    /// Gets the correlation id for the event.
    /// </summary>
    public string CorrelationId { get; }

    /// <summary>
    /// Gets the type of the domain entity that the audit event is pertinent to.
    /// </summary>
    public SourceEntityType? SourceEntityType { get; }

    /// <summary>
    /// Gets the id of the domain entity that the audit event is pertinent to.
    /// </summary>
    public Guid? SourceEntityId { get; }

    /// <summary>
    /// Gets any pertinent data that should be associated with the event.
    /// </summary>
    public object? AuditData { get; protected set; }

    /// <summary>
    /// Creates a new instance of an <see cref="AuditEvent"/>.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="sourceEntityId">The id of the domain entity that is pertinent to the current event.</param>
    /// <param name="userId">The identifier of the user that triggered this event.</param>
    /// <param name="auditData">Any pertinent data that should be associated with the event.</param>
    /// <param name="requestContext"></param>
    public AuditEvent(string eventName,
        Guid? sourceEntityId,
        Guid? userId,
        RequestContext requestContext,
        object? auditData = null)
    {
        EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
        UserId = userId;
        ActorType = requestContext.ActorType;
        CorrelationId = requestContext.RequestId;
        SourceEntityType = AuditEvents.GetEventSourceEntityType(eventName);
        SourceEntityId = sourceEntityId;
        AuditData = auditData;
    }
}