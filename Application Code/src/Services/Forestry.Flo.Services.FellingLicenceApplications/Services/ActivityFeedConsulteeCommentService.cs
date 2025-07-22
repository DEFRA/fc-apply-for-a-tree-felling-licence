using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ActivityFeedConsulteeCommentService : IActivityFeedService
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationRepository;
    private readonly ILogger<ActivityFeedConsulteeCommentService> _logger;

    public ActivityFeedConsulteeCommentService(
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationRepository,
        ILogger<ActivityFeedConsulteeCommentService> logger)
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

        _logger.LogDebug("Attempt to retrieve activity feed consultee comments for application with id {ApplicationId}", providerModel.FellingLicenceId);

        var comments = await _fellingLicenceApplicationRepository.GetConsulteeCommentsAsync(
            providerModel.FellingLicenceId, null, cancellationToken);

        var activityFeedItems = new List<ActivityFeedItemModel>(comments.Count);

        foreach (var consulteeComment in comments)
        {
            var itemText = consulteeComment.ApplicableToSection.HasValue
                ? $"Regarding {consulteeComment.ApplicableToSection.Value.GetDisplayName()}:\n{consulteeComment.Comment}"
                : consulteeComment.Comment;

            activityFeedItems.Add(new ActivityFeedItemModel
            {
                ActivityFeedItemType = ActivityFeedItemType.ConsulteeComment,
                CreatedTimestamp = consulteeComment.CreatedTimestamp,
                AssociatedId = consulteeComment.Id,
                FellingLicenceApplicationId = consulteeComment.FellingLicenceApplicationId,
                VisibleToConsultee = true,
                VisibleToApplicant = true,
                Text = itemText,
                Source = $"{consulteeComment.AuthorName} ({consulteeComment.AuthorContactEmail})"
            });
        }

        var orderedList = activityFeedItems.OrderByDescending(x => x.CreatedTimestamp).ToList();
        return Result.Success<IList<ActivityFeedItemModel>>(orderedList);
    }

    public ActivityFeedItemType[] SupportedItemTypes()
    {
        return new[]
        {
            ActivityFeedItemType.ConsulteeComment
        };
    }
}