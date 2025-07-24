using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Forestry.Flo.Services.Notifications.Configuration;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using RazorLight;
using System.Diagnostics.CodeAnalysis;

namespace Forestry.Flo.Services.Notifications.Services;

/// <summary>
/// Implementation of <see cref="ISendNotifications"/> that sends emails via SMTP using an <see cref="IFluentEmail"/> instance.
/// </summary>
public class EmailService : SendNotificationsBase
{
    private readonly ISender _sender;
    private readonly ITemplateRenderer _renderer;
    private readonly ILogger<EmailService> _logger;
    private readonly NotificationsOptions _options;

    /// <summary>
    /// Implementation of <see cref="ISendNotifications"/> that sends emails via SMTP using an <see cref="IFluentEmail"/> instance.
    /// </summary>
    public EmailService(
        ITemplateRenderer renderer,
        ISender sender,
        IOptions<NotificationsOptions> options,
        INotificationHistoryRepository notificationHistoryRepository,
        IClock clock,
        ILogger<EmailService> logger)
        : base(notificationHistoryRepository, clock, logger)
    {
        _sender = Guard.Against.Null(sender);
        _renderer = Guard.Against.Null(renderer);
        _options = Guard.Against.Null(options.Value);
        _logger = logger ?? new NullLogger<EmailService>();
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
        try
        {
            var subject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);
            var content = await CreateNotificationContentAsyncImpl(model!, notificationType).ConfigureAwait(false);

            if (content.IsFailure)
            {
                content.ConvertFailure();
            }

            var email = new Email(_renderer, _sender, _options.DefaultFromAddress, _options.DefaultFromName)
                .To(recipients.Select(x => new Address(x.Address, x.Name)))
                .Subject(subject)
                .Body(content.Value, true);

            if (copyToRecipients != null)
            {
                email = email.CC(copyToRecipients.Select(x => new Address(x.Address, x.Name)));
            }

            if (string.IsNullOrWhiteSpace(_options.CopyToAddress) == false)
            {
                email = email.CC(_options.CopyToAddress);
            }

            if (attachments is not null)
            {
                email = email.Attach(AddEmailAttachments(attachments));
            }

            var response = await email.SendAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Email sending for email id {EmailId} returned response success {SendResponse}",
                response.MessageId, response.Successful);

            if (response.Successful)
            {
                return Result.Success(content.Value);
            }

            var errorMessage = string.Join(Environment.NewLine, response.ErrorMessages);
            _logger.LogError("Email send error messages: {ErrorMessages}", errorMessage);
            return Result.Failure<string>(errorMessage);
        }
        catch (NotSupportedException e)
        {
            _logger.LogError(e, "Could not translate notification type to template file path or subject");
            return Result.Failure<string>($"Unsupported notification type {notificationType} was detected.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception caught in EmailService");
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
        try
        {
            var bodyTemplateFile = NotificationTemplates.NotificationBodyTemplatePath(notificationType);
            var notificationsAssembly = typeof(EmailService).Assembly;

            var engine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(notificationsAssembly, notificationsAssembly.GetName().Name)
                .UseMemoryCachingProvider()
                .SetOperatingAssembly(notificationsAssembly)
                .Build();

            var result = await engine.CompileRenderAsync(bodyTemplateFile, model).ConfigureAwait(false);
            return Result.Success(result);
        }
        catch (NotSupportedException e)
        {
            _logger.LogError(e, "Could not translate notification type to template file path or subject");
            return Result.Failure<string>($"Unsupported notification type {notificationType} was detected.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception caught in EmailService");
            return Result.Failure<string>(e.Message);
        }

    }

    private static IEnumerable<Attachment> AddEmailAttachments(IEnumerable<NotificationAttachment> attachments) =>
        attachments.Select(document =>
            new Attachment
            {
                Data = new MemoryStream(document.FileBytes), 
                Filename = document.FileName, 
                ContentType = document.ContentType
            }).ToList();
}
