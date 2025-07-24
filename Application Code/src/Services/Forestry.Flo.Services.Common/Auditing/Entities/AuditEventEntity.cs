using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Common.Auditing.Entities;

public class AuditEventEntity
{
    /// <summary>
    /// Gets the unique internal identifier for the audit event.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets the id of a user that triggered this event, if it is not a system-initiated event.
    /// </summary>
    public Guid? UserId { get; protected set; }

    /// <summary>
    /// Gets the type of the actor that triggered this event.
    /// </summary>
    public ActorType ActorType { get; protected set; }

    /// <summary>
    /// Gets the point in time at which the audit event was created.
    /// </summary>
    [Required]
    public DateTimeOffset EffectiveTime { get; internal set; }

    /// <summary>
    /// Gets the correlation id for the event.
    /// </summary>
    [Required]

    public string CorrelationId { get; protected set; }
    
    /// <summary>
    /// Gets the name of the use case of the event.
    /// </summary>
    [Required]
    public string Source { get; protected set; }

    /// <summary>
    /// Gets the type of the domain entity that the audit event is pertinent to.
    /// </summary>
    public SourceEntityType? SourceEntityType { get; protected set; }

    /// <summary>
    /// Gets the id of the domain entity that the audit event is pertinent to.
    /// </summary>
    public Guid? SourceEntityId { get; protected set; }

    /// <summary>
    /// Gets the name of the event.
    /// </summary>
    [Required]
    public string EventName { get; protected set; }

    /// <summary>
    /// Gets any pertinent data that should be associated with the event.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? AuditData { get; protected set; }

    /// <summary>
    /// Reserved for use by entity framework
    /// </summary>
    protected AuditEventEntity()
    {
    }

    /// <summary>
    /// Creates a new instance of an <see cref="AuditEventEntity"/>.
    /// </summary>
    /// <param name="auditEvent">The <see cref="AuditEvent"/> that will be extracted into the new instance.</param>
    /// <param name="source">Type name of the domain entity that the audit event is pertinent to</param>
    internal AuditEventEntity(AuditEvent auditEvent, string source)
    {
        if (auditEvent == null) throw new ArgumentNullException(nameof(auditEvent));
         
        EventName = auditEvent.EventName;
        UserId = auditEvent.UserId;
        ActorType = auditEvent.ActorType;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        CorrelationId = auditEvent.CorrelationId;
        SourceEntityType = auditEvent.SourceEntityType;
        SourceEntityId = auditEvent.SourceEntityId;

        if (auditEvent.AuditData != null)
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            AuditData = JsonSerializer.Serialize(auditEvent.AuditData, options);
        }
    }
}