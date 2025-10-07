using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Services.Notifications.Services;

/// <summary>
/// Implementation of <see cref="INotificationHistoryService"/> that retrieves notification history
/// entries using an <see cref="INotificationHistoryRepository"/> implementation.
/// </summary>
public class NotificationHistoryService : INotificationHistoryService
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
        Guid applicationId,
        NotificationType[]? notificationTypesFilter,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to retrieve notification history entries for application {ApplicationId} and filter {Filter}",
            applicationId,
            notificationTypesFilter == null ? "none" : string.Join(',', notificationTypesFilter));

        var entities = await _repository.GetNotificationHistoryForApplicationAsync(
            applicationId,
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
            Source = x.Source,
            Id = x.Id,
            ExternalId = x.ExternalId,
            Status = x.Status ?? NotificationStatus.New,
            Response = x.Response
        }).ToList();

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> AddNotificationHistoryListAsync(IEnumerable<NotificationHistoryModel> models, CancellationToken cancellationToken)
    {
        try
        {
            var list = models?.ToList() ?? new List<NotificationHistoryModel>();
            var externalIds = list
                .Select(m => m.ExternalId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var existingIds = new HashSet<Guid>();
            if (externalIds.Any())
            {
                var allExisting = await _repository.GetExistingExternalIdsAsync(externalIds, cancellationToken);
                existingIds = [.. allExisting];
            }

            int addedCount = 0;
            foreach (var model in list)
            {
                if (model.ExternalId.HasValue && existingIds.Contains(model.ExternalId.Value))
                    continue;

                var entity = new NotificationHistory
                {
                    CreatedTimestamp = model.CreatedTimestamp,
                    Source = model.Source,
                    NotificationType = model.Type,
                    Text = model.Text,
                    Recipients = model.Recipients != null ? JsonConvert.SerializeObject(model.Recipients) : string.Empty,
                    ApplicationReference = model.ApplicationReference,
                    ApplicationId = model.ApplicationId,
                    ExternalId = model.ExternalId
                };

                _repository.Add(entity);
                addedCount++;
            }

            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added {Count} NotificationHistory entries", addedCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add notification history entries");
            return Result.Failure("Failed to add notification history entries.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<NotificationHistoryModel>> GetNotificationHistoryByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entityResult = await _repository.GetByIdAsync(id, cancellationToken);
        if (entityResult.IsFailure)
        {
            return Result.Failure<NotificationHistoryModel>(entityResult.Error.GetDescription());
        }
        var x = entityResult.Value;
        var model = new NotificationHistoryModel
        {
            Id = x.Id,
            CreatedTimestamp = x.CreatedTimestamp,
            Source = x.Source,
            Type = x.NotificationType,
            Text = x.Text,
            Recipients = string.IsNullOrWhiteSpace(x.Recipients)
                ? null
                : JsonConvert.DeserializeObject<List<NotificationRecipient>>(x.Recipients)!,
            ApplicationReference = x.ApplicationReference,
            ApplicationId = x.ApplicationId,
            ExternalId = x.ExternalId,
            Status = x.Status ?? NotificationStatus.New,
            Response = x.Response,
            LastUpdatedById = x.LastUpdatedById,
            LastUpdatedDate = x.LastUpdatedDate
        };
        return Result.Success(model);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateResponseStatusByIdAsync(Guid id, NotificationStatus status, string? response, Guid lastUpdatedById, DateTime lastUpdatedDate, CancellationToken cancellationToken)
    {
        var updateResult = await _repository.UpdateByIdAsync(id, entity =>
        {
            entity.Status = status;
            entity.Response = response;
            entity.LastUpdatedById = lastUpdatedById;
            entity.LastUpdatedDate = lastUpdatedDate;
        }, cancellationToken);
        return updateResult.IsSuccess ? Result.Success() : Result.Failure(updateResult.Error.GetDescription());
    }
}
