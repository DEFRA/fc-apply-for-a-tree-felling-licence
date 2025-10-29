using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Notifications.Entities;

[Index(nameof(ExternalId))]
public class NotificationHistory
{
    /// <summary>
    /// Gets and Sets the notification history item ID.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets and Sets the notification created timestamp
    /// </summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Gets and Sets the source of the notification
    /// </summary>
    public string? Source { get; set; } 

    /// <summary>
    /// Gets and Sets the notification type
    /// </summary>
    [Required]
    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// Gets and Sets the notification text
    /// </summary>
    [Required]
    public string Text { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the notification recipients list
    /// </summary>
    [Required]
    public string? Recipients { get; set; } = null;

    /// <summary>
    /// Gets and Sets the optional application reference
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the optional identifier for the application the notification is associated with.
    /// </summary>
    public Guid? ApplicationId { get; set; }

    /// <summary>
    /// Gets and Sets the external identifier for the notification history item.
    /// </summary>
    public Guid? ExternalId { get; set; }

    /// <summary>
    /// Gets and Sets the response message or payload received after sending the notification.
    /// This may contain details from the notification service, such as confirmation or error information.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets and Sets the current status of the notification.
    /// Expected values may include "New", "Reviewed", "Responded", or other status indicators from the notification process.
    /// </summary>
    public NotificationStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last updated this record.
    /// </summary>
    public Guid? LastUpdatedById { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this record was last updated.
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }
}