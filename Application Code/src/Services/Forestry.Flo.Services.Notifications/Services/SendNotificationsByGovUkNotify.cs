using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Notifications.Configuration;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using Notify.Client;
using Notify.Exceptions;
using System.Diagnostics.CodeAnalysis;
using Notify.Interfaces;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Notifications.Services;

/// <summary>
/// Implementation of <see cref="ISendNotifications"/> that sends notifications using the Gov.UK Notify service.
/// </summary>
public class SendNotificationsByGovUkNotify : SendNotificationsBase
{
    private readonly ILogger<SendNotificationsByGovUkNotify> _logger;
    private readonly GovUkNotifyOptions _options;
    private readonly IAsyncNotificationClient _client;

    private const string AttachmentKey = "Attachment";
    private const string HasAttachmentsKey = "HasAttachments";

    /// <summary>
    /// Create a new instance of <see cref="SendNotificationsByGovUkNotify"/>.
    /// </summary>
    /// <param name="options">Configuration options for sending by GovUK Notify.</param>
    /// <param name="notificationHistoryRepository">A <see cref="INotificationHistoryRepository"/> to record sent notifications.</param>
    /// <param name="asyncNotificationClient">An <see cref="IAsyncNotificationClient"/> to send the emails.</param>
    /// <param name="clock">A <see cref="IClock"/> to get the current date and time.</param>
    /// <param name="logger">A logging implementation.</param>
    public SendNotificationsByGovUkNotify(
        IOptions<GovUkNotifyOptions> options,
        INotificationHistoryRepository notificationHistoryRepository, 
        IAsyncNotificationClient asyncNotificationClient,
        IClock clock, 
        ILogger<SendNotificationsByGovUkNotify> logger) 
        : base(notificationHistoryRepository, clock, logger)
    {
        _client = Guard.Against.Null(asyncNotificationClient);
        _options = Guard.Against.Null(options).Value;
        _logger = logger ?? new NullLogger<SendNotificationsByGovUkNotify>();
    }

    /// <inheritdoc />
    protected override async Task<Result<string>> SendNotificationAsyncImpl<T>(
        [DisallowNull] T model, 
        NotificationType notificationType, 
        NotificationRecipient[] recipients,
        NotificationRecipient[]? copyToRecipients = null, 
        NotificationAttachment[]? attachments = null,
        string? senderName = null, 
        CancellationToken cancellationToken = default)
    {
        var templateId = _options.TemplateIds[notificationType];

        var allRecipients = recipients.Concat(copyToRecipients ?? [])
            .Select(r => r.Address)
            .ToArray();

        try
        {
            _logger.LogDebug(
                "Gov UK Notify: Sending a {NotificationType} email to {RecipientCount} addresses using template {TemplateId}",
                notificationType,
                allRecipients.Length,
                templateId);

            var personalisation = GetPersonalisation(model, attachments, false);
            var replyToId = string.IsNullOrWhiteSpace(_options.ReplyToId) ? null : _options.ReplyToId;

            foreach (var recipientAddress in allRecipients)
            {
                await _client.SendEmailAsync(
                    recipientAddress,
                    templateId,
                    personalisation,
                    emailReplyToId: replyToId)
                    .ConfigureAwait(false);
            }

            return Result.Success(JsonConvert.SerializeObject(model));
        }
        catch (NotifyClientException e)
        {
            _logger.LogError(
                "Error response from GovUk Notify attempting to send a {NotificationType} email with message {Error}",
                notificationType,
                e.Message);
            return Result.Failure<string>(e.Message);
        }
    }

    /// <inheritdoc />
    protected override async Task<Result<string>> CreateNotificationContentAsyncImpl<T>(
        [DisallowNull] T model,
        NotificationType notificationType,
        NotificationAttachment[]? attachments = null,
        bool forPreview = false,
        CancellationToken cancellationToken = default)
    {
        var templateId = _options.TemplateIds[notificationType];

        try
        {
            var preview = await _client.GenerateTemplatePreviewAsync(templateId, GetPersonalisation(model, attachments, forPreview));
            return Result.Success(preview.body);
        }
        catch (NotifyClientException e)
        {
            _logger.LogError(
                "Error response from GovUk Notify attempting to generate a preview with message {Error}",
                e.Message);
            return Result.Failure<string>(e.Message);
        }
    }

    private Dictionary<string, dynamic> GetPersonalisation<T>(
        T model,
        NotificationAttachment[]? attachments,
        bool forPreview)
    {
        var personalisation = new Dictionary<string, dynamic>();

        foreach (var property in typeof(T).GetProperties())
        {
            personalisation.Add(property.Name, property.GetValue(model));
        }
        
        personalisation.Add(HasAttachmentsKey, attachments is { Length: > 0 });

        for (int i = 0; i < NotificationConstants.MaxAttachments; i++)
        {
            if (attachments != null && attachments.Length > i)
            {
                personalisation.Add(
                    $"{AttachmentKey}{i + 1}",
                    forPreview 
                        ? attachments[i].FileName 
                        : NotificationClient.PrepareUpload(attachments[i].FileBytes, attachments[i].FileName));
            }
            else
            {
                personalisation.Add($"{AttachmentKey}{i + 1}", string.Empty);
            }
        }

        return personalisation;
    }
}