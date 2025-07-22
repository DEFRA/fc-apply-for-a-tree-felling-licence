namespace Forestry.Flo.Services.Notifications.Models;

public record NotificationAttachment(string FileName, byte[] FileBytes, string ContentType);