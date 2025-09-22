using Forestry.Flo.Services.Notifications.Entities;

namespace Forestry.Flo.Services.Notifications.Models;

public class NotificationHistoryModel
{
    public Guid? Id { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public string? Source { get; set; }

    public NotificationType Type { get; set; }

    public string Text { get; set; }

    public IEnumerable<NotificationRecipient>? Recipients { get; set; }

    public string? ApplicationReference { get; set; }

    public Guid? ApplicationId { get; set; }

    public Guid? ExternalId { get; set; }

    public NotificationStatus Status { get; set; }

    public string? Response { get; set; }

    public bool Reviewed
    {
        get => Status == NotificationStatus.Reviewed;
        set => Status = value ? NotificationStatus.Reviewed : NotificationStatus.New;
    }

    /// <summary>
    /// Gets or sets the ID of the user who last updated this record.
    /// </summary>
    public Guid? LastUpdatedById { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this record was last updated.
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last updated this record.
    /// </summary>
    public string? LastUpdatedUser { get; set; }
}