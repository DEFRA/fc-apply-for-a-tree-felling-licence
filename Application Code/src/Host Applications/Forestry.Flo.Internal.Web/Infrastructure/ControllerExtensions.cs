using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Infrastructure;

public static class ControllerExtensions
{
    public const string ConfirmationMessageKey = "ConfirmationMessage";
    public const string ConfirmationMessageBodyKey = "ConfirmationMessageBody";
    public const string UserGuideKey = "UserGuide";
    public const string UserGuideLinkKey = "UserGuideLink";
    public const string UserGuideBodyKey = "UserGuideBody";
    public const string ErrorMessageKey = "ErrorMessage";
    public const string ErrorFieldNameKey = "ErrorFieldName";

    public static void AddConfirmationMessage(this Controller controller, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        controller.TempData.Add(ConfirmationMessageKey, value);
    }

    public static void AddConfirmationMessage(this Controller controller, string heading, string body)
    {
        if (string.IsNullOrWhiteSpace(heading) && string.IsNullOrWhiteSpace(body))
            return;

        if (!string.IsNullOrWhiteSpace(heading))
        {
            controller.TempData.Add(ConfirmationMessageKey, heading);
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            controller.TempData.Add(ConfirmationMessageBodyKey, body);
        }

    }

    public static void AddErrorMessage(this Controller controller, string value, string? fieldName = default )
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        controller.TempData.TryAdd(ErrorMessageKey, value);
        if (fieldName is not null)
        {
            controller.TempData.TryAdd(ErrorFieldNameKey, fieldName);
        }
    }
    public static void AddUserGuide(this Controller controller, string value, NotificationLink? notificationLink = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        controller.TempData.Add(UserGuideKey, value);

        if (notificationLink != null)
        {
            controller.TempData.Put(UserGuideLinkKey, notificationLink);
        }
    }

    public static void AddUserGuide(this Controller controller, string heading, string body, NotificationLink? notificationLink = null)
    {
        if (string.IsNullOrWhiteSpace(heading) || string.IsNullOrWhiteSpace(body))
            return;

        if (!string.IsNullOrWhiteSpace(heading))
        {
            controller.TempData.Add(UserGuideKey, heading);
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            controller.TempData.Add(UserGuideBodyKey, body);
        }

        if (notificationLink != null)
        {
            controller.TempData.Put(UserGuideLinkKey, notificationLink);
        }
    }

    public class NotificationLink
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
        
        [JsonPropertyName("link")]
        public string Link { get; set; }

        public NotificationLink()
        {
        }

        public NotificationLink(string text, HttpRequest request, string path)
        {
            Text = Guard.Against.NullOrWhiteSpace(text);

            var builder = new UriBuilder(request.Scheme, request.Host.Host)
            {
                Path = path,
                Query = ""
            };
            if (request.Host.Port.HasValue)
                builder.Port = request.Host.Port.Value;

            Link = builder.Uri.AbsoluteUri;
        }
    }
}