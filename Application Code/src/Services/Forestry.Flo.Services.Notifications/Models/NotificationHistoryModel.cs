using Forestry.Flo.Services.Notifications.Entities;

namespace Forestry.Flo.Services.Notifications.Models;

public class NotificationHistoryModel
{
    public DateTime CreatedTimestamp { get; set; }

    public string? Source { get; set; }

    public NotificationType Type { get; set; }

    public string Text { get; set; }

    public IEnumerable<NotificationRecipient>? Recipients { get; set; }
}