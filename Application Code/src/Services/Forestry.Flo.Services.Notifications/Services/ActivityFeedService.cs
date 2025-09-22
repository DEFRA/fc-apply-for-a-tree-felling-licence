using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.Notifications.Entities;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Notifications.Services;

public class ActivityFeedService : IActivityFeedService
{
    private readonly ILogger<ActivityFeedService> _logger;
    private readonly INotificationHistoryService _notificationHistoryService;

    public ActivityFeedService(
        ILogger<ActivityFeedService> logger,
        INotificationHistoryService notificationHistoryService
        )
    {
        _logger = Guard.Against.Null(logger);
        _notificationHistoryService = Guard.Against.Null(notificationHistoryService);
    }


    public async Task<Result<IList<ActivityFeedItemModel>>> RetrieveActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellationToken)
    {
        var activityFeedItems = new List<ActivityFeedItemModel>();

        var filteredTypes = providerModel.ItemTypes!.Where(x => SupportedItemTypes().Contains(x)).ToArray();

        var notificationTypes = new NotificationType[filteredTypes.Length];

        for (int i = 0; i < filteredTypes.Length; i++)
        {
            var itemType = filteredTypes[i];
            notificationTypes[i] = (NotificationType) Enum.Parse(typeof(NotificationType), itemType.ToString());
        }

        var (_, isFailure, notifications) = await _notificationHistoryService.RetrieveNotificationHistoryAsync(
            providerModel.FellingLicenceId!,
            notificationTypes,
            cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Unable to retrieve notifications for application {id}", providerModel.FellingLicenceId);
            return Result.Failure<IList<ActivityFeedItemModel>>("Unable to convert notifications into activity feed items");
        }

        foreach (var notification in notifications)
        {
            var activityFeedItemType = (ActivityFeedItemType)Enum.Parse(typeof(ActivityFeedItemType), notification.Type.ToString());

            var text = activityFeedItemType.GetActivityFeedItemTypeAttribute() ==
                       ActivityFeedItemCategory.OutgoingNotification
                ? activityFeedItemType.GetDisplayName()
                : notification.Text;

            activityFeedItems.Add(new()
            {
                CreatedTimestamp = notification.CreatedTimestamp,
                Text = text,
                FellingLicenceApplicationId = providerModel.FellingLicenceId,
                ActivityFeedItemType = activityFeedItemType,
                Source = notification.Source,
                VisibleToApplicant = false,
                VisibleToConsultee = false,
                Recipients = notification.Recipients?.Select(x => x.Name!).ToArray(),
            });
        }

        return Result.Success<IList<ActivityFeedItemModel>>(activityFeedItems);
    }

    public ActivityFeedItemType[] SupportedItemTypes()
    {
        return new[]
        {
            ActivityFeedItemType.PublicRegisterComment,
            ActivityFeedItemType.ApplicationSubmissionConfirmation,
            ActivityFeedItemType.ApplicationWithdrawnConfirmation,
            ActivityFeedItemType.UserAssignedToApplication,
            ActivityFeedItemType.InformApplicantOfReturnedApplication,
            ActivityFeedItemType.InformFCStaffOfReturnedApplication,
            ActivityFeedItemType.InformWoodlandOfficerOfAdminOfficerReviewCompletion,
            ActivityFeedItemType.InformFieldManagerOfWoodlandOfficerReviewCompletion,
            ActivityFeedItemType.ExternalConsulteeInvite,
            ActivityFeedItemType.ExternalConsulteeInviteWithPublicRegisterInfo,
            ActivityFeedItemType.ApplicationResubmitted,
            ActivityFeedItemType.ConditionsToApplicant,
            ActivityFeedItemType.InformApplicantOfApplicationExtension,
            ActivityFeedItemType.InformApplicantOfApplicationApproval,
            ActivityFeedItemType.InformApplicantOfApplicationRefusal,
            ActivityFeedItemType.InformApplicantOfApplicationReferredToLocalAuthority
        };
    }
}

