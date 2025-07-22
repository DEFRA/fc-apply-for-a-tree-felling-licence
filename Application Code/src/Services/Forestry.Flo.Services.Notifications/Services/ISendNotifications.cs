using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;

namespace Forestry.Flo.Services.Notifications.Services;

/// <summary>
/// Contract for services that send notifications.
/// </summary>
public interface ISendNotifications
{
    /// <summary>
    /// Send a notification with a single recipient.
    /// </summary>
    /// <typeparam name="T">The type of the model for the values to be merged into the notification text.</typeparam>
    /// <param name="model">The instance of <see cref="T"/> holding values to merge into the notification text.</param>
    /// <param name="notificationType">The <see cref="NotificationType"/> of notification to send.</param>
    /// <param name="recipient">The <see cref="NotificationRecipient"/> to send the notification to.</param>
    /// <param name="copyToRecipients">An optional array of <see cref="NotificationRecipient"/> to copy the notification to.</param>
    /// <param name="attachments">An optional array of <see cref="NotificationAttachment"/> to attach to the notification.</param>
    /// <param name="senderName">An optional name of the person sending the notification.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    /// <remarks>The generic model for this method allows the use of any type to be passed into the method to
    /// act as the source of values to merge into the notification content.  Data models for some notifications can be
    /// defined within this project where simply passing in a relevant entity or viewmodel from the UI does not provide
    /// all the required values for a notification.</remarks>
    Task<Result> SendNotificationAsync<T>(
        T model, 
        NotificationType notificationType, 
        NotificationRecipient recipient,
        NotificationRecipient[]? copyToRecipients = null,
        NotificationAttachment[]? attachments = null,
        string? senderName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a notification with multiple recipients.
    /// </summary>
    /// <typeparam name="T">The type of the model for the values to be merged into the notification text.</typeparam>
    /// <param name="model">The instance of <see cref="T"/> holding values to merge into the notification text.</param>
    /// <param name="notificationType">The <see cref="NotificationType"/> of notification to send.</param>
    /// <param name="recipients">An array of <see cref="NotificationRecipient"/> to send the notification to.</param>
    /// <param name="copyToRecipients">An optional array of <see cref="NotificationRecipient"/> to copy the notification to.</param>
    /// <param name="attachments">An optional array of <see cref="NotificationAttachment"/> to attach to the notification.</param>
    /// <param name="senderName">An optional name of the person sending the notification.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    /// <remarks>The generic model for this method allows the use of any type to be passed into the method to
    /// act as the source of values to merge into the notification content.  Data models for some notifications can be
    /// defined within this project where simply passing in a relevant entity or viewmodel from the UI does not provide
    /// all the required values for a notification.</remarks>
    Task<Result> SendNotificationAsync<T>(
        T model,
        NotificationType notificationType,
        NotificationRecipient[] recipients,
        NotificationRecipient[]? copyToRecipients = null,
        NotificationAttachment[]? attachments = null,
        string? senderName = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Request a preview copy of the notification that would be sent with the given parameters.
    /// </summary>
    /// <typeparam name="T">The type of the model for the values to be merged into the notification text.</typeparam>
    /// <param name="model">The instance of <see cref="T"/> holding values to merge into the notification text.</param>
    /// <param name="notificationType">The <see cref="NotificationType"/> of notification to generate a preview for.</param>
    /// <param name="attachments">An optional array of <see cref="NotificationAttachment"/> to attach to the notification.</param>
    /// <param name="forPreview">An optional flag to indicate that the content is being requested as a preview rather than
    /// to actually send an email.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct containing either the successful result value or an error.</returns>
    Task<Result<string>> CreateNotificationContentAsync<T>(
        [DisallowNull] T model, 
        NotificationType notificationType, 
        NotificationAttachment[]? attachments = null,
        bool forPreview = false,
        CancellationToken cancellationToken = default);
}