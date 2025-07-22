using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Notifications.Services;

/// <summary>
/// Implementation of <see cref="IRetrieveNotificationHistory"/> that retrieves notification history
/// entries using an <see cref="INotificationHistoryRepository"/> implementation.
/// </summary>
public class NotificationHistoryService: IRetrieveNotificationHistory
{
    private readonly INotificationHistoryRepository _repository;
    private readonly ILogger<NotificationHistoryService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="NotificationHistoryService"/>.
    /// </summary>
    /// <param name="repository">An <see cref="INotificationHistoryRepository"/> instance from which to retrieve notification history entries.</param>
    /// <param name="logger">A logger.</param>
    public NotificationHistoryService(
        INotificationHistoryRepository repository, 
        ILogger<NotificationHistoryService> logger)
    {
        _repository = Guard.Against.Null(repository);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<List<NotificationHistoryModel>>> RetrieveNotificationHistoryAsync(
        string applicationReference,
        NotificationType[]? notificationTypesFilter,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to retrieve notification history entries for application {ApplicationReference} and filter {Filter}",
            applicationReference,
            notificationTypesFilter == null ? "none" : string.Join(',', notificationTypesFilter));

        var entities = await _repository.GetNotificationHistoryForApplicationAsync(
            applicationReference,
            notificationTypesFilter,
            cancellationToken);

        if (entities.IsFailure)
        {
            _logger.LogError(entities.Error);
            return entities.ConvertFailure<List<NotificationHistoryModel>>();
        }

        _logger.LogDebug("Returning {NotificationHistoryCount} notification history entries found.", entities.Value.Count);
        var result = entities.Value.Select(x => new NotificationHistoryModel
        {
            Type = x.NotificationType,
            CreatedTimestamp = x.CreatedTimestamp,
            Text = x.Text,
            Recipients = string.IsNullOrWhiteSpace(x.Recipients)
                ? null
                : JsonConvert.DeserializeObject<List<NotificationRecipient>>(x.Recipients)!,
            Source = x.Source
        }).ToList();

        return Result.Success(result);
    }
}