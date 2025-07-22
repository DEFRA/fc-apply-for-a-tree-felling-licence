using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Notifications.Entities;

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
    public string Recipients { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the optional application reference
    /// </summary>
    public string? ApplicationReference { get; set; }
}