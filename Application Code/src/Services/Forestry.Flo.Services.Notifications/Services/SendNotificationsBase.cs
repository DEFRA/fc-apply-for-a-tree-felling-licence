using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NodaTime;

namespace Forestry.Flo.Services.Notifications.Services;

/// <summary>
/// Base class for services dealing with sending notifications.
/// </summary>
public abstract class SendNotificationsBase : ISendNotifications
{
    private readonly INotificationHistoryRepository _notificationHistoryRepository;
    private readonly IClock _clock;
    private readonly ILogger<SendNotificationsBase> _logger;

    /// <summary>
    /// Create a new instance of <see cref="SendNotificationsBase"/>.
    /// </summary>
    /// <param name="notificationHistoryRepository">A <see cref="INotificationHistoryRepository"/> to store
    /// a history of sent notifications.</param>
    /// <param name="clock">A <see cref="IClock"/> to get the current date and time.</param>
    /// <param name="logger">A logging implementation.</param>
    protected SendNotificationsBase(
        INotificationHistoryRepository notificationHistoryRepository,
        IClock clock,
        ILogger<SendNotificationsBase> logger)
    {
        _notificationHistoryRepository = Guard.Against.Null(notificationHistoryRepository);
        _clock = Guard.Against.Null(clock);
        _logger = logger ?? new NullLogger<SendNotificationsBase>();
    }

    /// <inheritdoc />
    public Task<Result> SendNotificationAsync<T>(
        T model,
        NotificationType notificationType,
        NotificationRecipient recipient,
        NotificationRecipient[]? copyToRecipients = null,
        NotificationAttachment[]? attachments = null,
        string? senderName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(recipient);

        _logger.LogDebug(
            "Received request to send email notification type {NotificationType} to recipient address {RecipientAddress}",
            notificationType, recipient.Address);

        return SendAndLogNotificationAsync(model, notificationType, new[] { recipient }, copyToRecipients, attachments,
            senderName, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> SendNotificationAsync<T>(
        T model,
        NotificationType notificationType,
        NotificationRecipient[] recipients,
        NotificationRecipient[]? copyToRecipients = null,
        NotificationAttachment[]? attachments = null,
        string? senderName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(recipients);
        if (!recipients.Any())
        {
            throw new ArgumentException("At least one recipient must be provided", nameof(recipients));
        }

        _logger.LogDebug(
            "Received request to send email notification type {NotificationType} to recipient addresses {RecipientAddresses}",
            notificationType, string.Join(", ", recipients.Select(x => x.Address)));

        return SendAndLogNotificationAsync(model, notificationType, recipients, copyToRecipients, attachments, senderName, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<string>> CreateNotificationContentAsync<T>(
        [DisallowNull] T model, 
        NotificationType notificationType,
        NotificationAttachment[]? attachments = null,
        bool forPreview = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(notificationType);

        _logger.LogDebug(
            "Received request to generate notification content for type {NotificationType}",
            notificationType);

        return CreateNotificationContentAsyncImpl(model, notificationType, attachments, forPreview, cancellationToken);
    }

    /// <summary>
    /// Specific service implementation for sending a notification.
    /// </summary>
    /// <typeparam name="T">The type of the data model for the notification.</typeparam>
    /// <param name="model">The data model for the notification.</param>
    /// <param name="notificationType">The type of the notification.</param>
    /// <param name="recipients">The list of To address <see cref="NotificationRecipient"/> for the notification.</param>
    /// <param name="copyToRecipients">The list of Copy To address <see cref="NotificationRecipient"/> for the notification.</param>
    /// <param name="attachments">An optional array of <see cref="NotificationAttachment"/> to attach to the notification.</param>
    /// <param name="senderName">An optional name of the person sending the notification.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    protected abstract Task<Result<string>> SendNotificationAsyncImpl<T>(
        [DisallowNull]T model,
        NotificationType notificationType,
        NotificationRecipient[] recipients,
        NotificationRecipient[]? copyToRecipients = null,
        NotificationAttachment[]? attachments = null,
        string? senderName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Specific service implementation for creating the content for a notification.
    /// </summary>
    /// <typeparam name="T">The type of the data model for the notification.</typeparam>
    /// <param name="model">The data model for the notification.</param>
    /// <param name="notificationType">The type of the notification.</param>
    /// <param name="attachments">An optional array of <see cref="NotificationAttachment"/> to attach to the notification.</param>
    /// <param name="forPreview">An optional flag to indicate that the content is being requested as a preview rather than
    /// to actually send an email.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct containing the successful result value or an error.</returns>
    protected abstract Task<Result<string>> CreateNotificationContentAsyncImpl<T>(
        [DisallowNull]T model, 
        NotificationType notificationType,
        NotificationAttachment[]? attachments = null,
        bool forPreview = false,
        CancellationToken cancellationToken = default);

    private async Task<Result> SendAndLogNotificationAsync<T>(
        [DisallowNull]T model,
        NotificationType notificationType,
        NotificationRecipient[] recipients,
        NotificationRecipient[]? copyToRecipients,
        NotificationAttachment[]? attachments,
        string? senderName,
        CancellationToken cancellationToken)
    {
        var result = await SendNotificationAsyncImpl(
                model, notificationType, recipients, copyToRecipients, attachments, senderName, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ConvertFailure();
        }

        return await AddToNotificationHistoryAsync(
                model, notificationType, recipients, copyToRecipients, senderName, result.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string GetRecipients(NotificationRecipient[] recipients, NotificationRecipient[]? copyToRecipients)
    {
        var recipientsList = new List<NotificationRecipient>();
        recipientsList.AddRange(recipients);

        if (copyToRecipients is not null)
        {
            recipientsList.AddRange(copyToRecipients);
        }

        return JsonConvert.SerializeObject(recipientsList);
    }

    private static string? GetApplicationReference<T>(T model)
    {
        var applicationModel = model as IApplicationNotification;
        return applicationModel?.ApplicationReference;
    }

    private async Task<Result> AddToNotificationHistoryAsync<T>(
        [DisallowNull] T model,
        NotificationType notificationType,
        NotificationRecipient[] recipients,
        NotificationRecipient[]? copyToRecipients,
        string? senderName,
        string? content,
        CancellationToken cancellationToken)
    {
        _notificationHistoryRepository.Add(new NotificationHistory
        {
            Recipients = GetRecipients(recipients, copyToRecipients),
            Source = senderName,
            Text = content ?? "Content unavailable",
            CreatedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
            NotificationType = notificationType,
            ApplicationReference = GetApplicationReference(model)
        });

        await _notificationHistoryRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .OnFailure(e => _logger.LogError(
                "Email send history not saved for the recipients: {Recipients}",
                string.Join(", ", recipients.Select(x => x.Address))))
            .ConfigureAwait(false);
        return Result.Success();
    }
}