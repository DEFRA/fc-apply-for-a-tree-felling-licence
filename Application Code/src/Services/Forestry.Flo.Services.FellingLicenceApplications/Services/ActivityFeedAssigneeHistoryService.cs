using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.Extensions.Logging;
namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ActivityFeedAssigneeHistoryService : IActivityFeedService
{
    private readonly InternalUsers.Repositories.IUserAccountRepository _internalAccountsRepository;
    private readonly Applicants.Repositories.IUserAccountRepository _externalAccountsRepository;
    private readonly ILogger<ActivityFeedAssigneeHistoryService> _logger;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationRepository;

    public ActivityFeedAssigneeHistoryService(
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationRepository,
        InternalUsers.Repositories.IUserAccountRepository internalAccountsRepository,
        Applicants.Repositories.IUserAccountRepository externalAccountsRepository,
        ILogger<ActivityFeedAssigneeHistoryService> logger)
    {
        _internalAccountsRepository = Guard.Against.Null(internalAccountsRepository);
        _externalAccountsRepository = Guard.Against.Null(externalAccountsRepository);
        _logger = logger;
        _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
    }

    public async Task<Result<IList<ActivityFeedItemModel>>> RetrieveActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(providerModel);

        _logger.LogDebug("Attempt to retrieve activity feed assignee history entries for application with ID {ApplicationId}", providerModel.FellingLicenceId);

        var assigneeHistoryItems = await _fellingLicenceApplicationRepository.GetAssigneeHistoryForApplicationAsync(
            providerModel.FellingLicenceId, cancellationToken);

        var internalUserIds = assigneeHistoryItems
            .Where(x => x.Role != AssignedUserRole.Author && x.Role != AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .ToList();

        var externalUserIds = assigneeHistoryItems
            .Where(x => x.Role == AssignedUserRole.Author || x.Role == AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .ToList();

        var internalUsers = await _internalAccountsRepository.GetUsersWithIdsInAsync(internalUserIds, cancellationToken);
        if (internalUsers.IsFailure)
        {
            _logger.LogError("Could not load internal users for assignee history activity feed items, error: {Error}", internalUsers.Error);
            return Result.Failure<IList<ActivityFeedItemModel>>("Could not load internal users for activity feed");
        }

        var externalApplicants = await _externalAccountsRepository.GetUsersWithIdsInAsync(externalUserIds, cancellationToken);
        if (externalApplicants.IsFailure)
        {
            _logger.LogError("Could not load external applicants for assignee history activity feed items, error: {Error}", externalApplicants.Error);
            return Result.Failure<IList<ActivityFeedItemModel>>("Could not load external applicants for activity feed");
        }

        var activityFeedItems = new List<ActivityFeedItemModel>(assigneeHistoryItems.Count);
        foreach (AssigneeHistory assigneeHistory in assigneeHistoryItems)
        {
            string itemText = "Application assigned to " + assigneeHistory.Role.GetDisplayName() + " (";
            if (assigneeHistory.Role == AssignedUserRole.Author || assigneeHistory.Role == AssignedUserRole.Applicant)
            {
                var applicant = externalApplicants.Value.SingleOrDefault(x => x.Id == assigneeHistory.AssignedUserId);

                itemText += (applicant?.FullName(false) ?? "unknown applicant") + ")";
            }
            else
            {
                var assignee = internalUsers.Value.SingleOrDefault(x => x.Id == assigneeHistory.AssignedUserId);
                itemText += (assignee?.FullName(false) ?? "unknown user") + ")";
            }

            var assigneeActivityFeedItem = new ActivityFeedItemModel
            {
                ActivityFeedItemType = ActivityFeedItemType.AssigneeHistoryNotification,
                AssociatedId = assigneeHistory.Id,
                VisibleToApplicant = true,
                VisibleToConsultee = false,
                FellingLicenceApplicationId = providerModel.FellingLicenceId,
                CreatedTimestamp = assigneeHistory.TimestampAssigned,
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
            ActivityFeedItemType.AssigneeHistoryNotification
        };
    }
}