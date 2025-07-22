using Forestry.Flo.Services.Notifications.Entities;

namespace Forestry.Flo.Services.Notifications.Configuration;

/// <summary>
/// Configuration settings for sending emails via Gov.UK Notify
/// </summary>
public class GovUkNotifyOptions
{
    /// <summary>
    /// The API key for the Gov.UK Notify API.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// The ID for the reply to address configured in Gov.UK Notify.
    /// </summary>
    /// <remarks>
    /// This value is optional; if it is not provided then the default reply
    /// to address configured in Gov.UK Notify will be used.
    /// </remarks>
    public string? ReplyToId { get; set; }

    /// <summary>
    /// A dictionary of template IDs for each notification type.
    /// </summary>
    public Dictionary<NotificationType, string> TemplateIds { get; set; }
}