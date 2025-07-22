using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ActivityFeedStatusHistoryService : IActivityFeedService
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationRepository;
    private readonly ILogger<ActivityFeedStatusHistoryService> _logger;

    public ActivityFeedStatusHistoryService(
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationRepository,
        ILogger<ActivityFeedStatusHistoryService> logger)
    {
        _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
        _logger = logger;
    }

    public async Task<Result<IList<ActivityFeedItemModel>>> RetrieveActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(providerModel);

        _logger.LogDebug("Attempt to retrieve activity feed status history entries for application with ID {ApplicationId}", providerModel.FellingLicenceId);

        var activityFeedItems = new List<ActivityFeedItemModel>();

        var statusHistories = 
            await _fellingLicenceApplicationRepository.GetStatusHistoryForApplicationAsync(providerModel.FellingLicenceId, cancellationToken);

        foreach (var statusHistory in statusHistories)
        {
            var itemText = "Application status set to " + statusHistory.Status.GetDisplayNameByActorType(requestingActorType);
            var assigneeActivityFeedItem = new ActivityFeedItemModel
            {
                ActivityFeedItemType = ActivityFeedItemType.StatusHistoryNotification,
                AssociatedId = statusHistory.Id,
                VisibleToApplicant = true,
                VisibleToConsultee = false,
                FellingLicenceApplicationId = statusHistory.FellingLicenceApplicationId,
                CreatedTimestamp = statusHistory.Created,
                Text = itemText
            };

            activityFeedItems.Add(assigneeActivityFeedItem);
        }

        var orderedList = activityFeedItems.OrderByDescending(x => x.CreatedTimestamp).ToList();
        return Result.Success<IList<ActivityFeedItemModel>>(orderedList);
    }

    public ActivityFeedItemType[] SupportedItemTypes()
    {
        return new[]
        {
            ActivityFeedItemType.StatusHistoryNotification
        };
    }
}