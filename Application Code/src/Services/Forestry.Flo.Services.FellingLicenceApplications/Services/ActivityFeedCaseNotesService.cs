using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ActivityFeedCaseNotesService : IActivityFeedService
{
    private readonly ILogger<ActivityFeedCaseNotesService> _logger;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IViewCaseNotesService _viewCaseNotesService;

    public ActivityFeedCaseNotesService(
        IViewCaseNotesService viewCaseNotesService,
        IUserAccountRepository userAccountRepository,
        ILogger<ActivityFeedCaseNotesService> logger)
    {
        _logger = logger;
        _userAccountRepository = Guard.Against.Null(userAccountRepository);
        _viewCaseNotesService = Guard.Against.Null(viewCaseNotesService);
    }

    public async Task<Result<IList<ActivityFeedItemModel>>> RetrieveActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(providerModel);

        _logger.LogDebug("Attempt to retrieve activity feed case note entries for application with ID {ApplicationId}", providerModel.FellingLicenceId);

        var activityFeedItems = new List<ActivityFeedItemModel>();

        var caseNoteTypes = providerModel.ItemTypes!.Where(x => x.GetActivityFeedItemTypeAttribute() == ActivityFeedItemCategory.CaseNote).ToArray();
        var caseNoteTypesConverted = Array.ConvertAll(caseNoteTypes, ActivityItemTypeToCaseNoteType);

        var caseNotes = await _viewCaseNotesService.GetSpecificCaseNotesAsync(providerModel.FellingLicenceId, caseNoteTypesConverted, cancellationToken);

        var userIds = caseNotes.Select(x => x.CreatedByUserId).Distinct().ToList();
        var users = userIds.Count > 0 
            ? await _userAccountRepository.GetUsersWithIdsInAsync(userIds, cancellationToken) 
            : new Result<IList<UserAccount>, UserDbErrorReason>();

        if (users.IsFailure)
        {
            _logger.LogError("Unable to determine all user accounts for activity feed items with error {error}", users.Error);
            return Result.Failure<IList<ActivityFeedItemModel>>("Unable to determine all user accounts for activity feed items");
        }

        foreach (var caseNote in caseNotes)
        {
            var user = users.Value.FirstOrDefault(x => x.Id == caseNote.CreatedByUserId);
            var activityFeedItemUserModel = user != null
                ? new ActivityFeedItemUserModel
                  {
                      AccountType = user.AccountType,
                      FirstName = user.FirstName,
                      Id = user.Id,
                      LastName = user.LastName,
                      IsActiveUser = user.Status == Status.Confirmed
                  }
                : null;

            var assigneeActivityFeedItem = new ActivityFeedItemModel
            {
                ActivityFeedItemType = (ActivityFeedItemType)caseNote.Type,
                AssociatedId = caseNote.Id,
                VisibleToApplicant = caseNote.VisibleToApplicant,
                VisibleToConsultee = caseNote.VisibleToConsultee,
                FellingLicenceApplicationId = caseNote.FellingLicenceApplicationId,
                CreatedTimestamp = caseNote.CreatedTimestamp,
                Text = caseNote.Text,
                CreatedByUser = activityFeedItemUserModel
            };

            activityFeedItems.Add(assigneeActivityFeedItem);
        }

        var orderedList = activityFeedItems.OrderByDescending(x => x.CreatedTimestamp).ToList();
        return Result.Success<IList<ActivityFeedItemModel>>(orderedList);
    }

    public ActivityFeedItemType[] SupportedItemTypes()
    {
        return
        [
            ActivityFeedItemType.CaseNote,
            ActivityFeedItemType.AdminOfficerReviewComment,
            ActivityFeedItemType.WoodlandOfficerReviewComment,
            ActivityFeedItemType.SiteVisitComment,
            ActivityFeedItemType.ReturnToApplicantComment,
            ActivityFeedItemType.LarchCheckComment,
        ];
    }


    private static CaseNoteType ActivityItemTypeToCaseNoteType(ActivityFeedItemType activityFeedItemType)
    {
        return (CaseNoteType)Enum.Parse(typeof(CaseNoteType), activityFeedItemType.ToString());
    }
}