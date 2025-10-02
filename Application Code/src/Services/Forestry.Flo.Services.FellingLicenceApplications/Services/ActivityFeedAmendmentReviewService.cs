using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ActivityFeedAmendmentReviewService : IActivityFeedService
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationRepository;
    private readonly ILogger<ActivityFeedAmendmentReviewService> _logger;
    private readonly InternalUsers.Repositories.IUserAccountRepository _internalAccountsRepository;
    private readonly Applicants.Repositories.IUserAccountRepository _externalAccountsRepository;

    public ActivityFeedAmendmentReviewService(
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationRepository,
        InternalUsers.Repositories.IUserAccountRepository internalAccountsRepository,
        Applicants.Repositories.IUserAccountRepository externalAccountsRepository,
        ILogger<ActivityFeedAmendmentReviewService> logger)
    {
        _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
        _internalAccountsRepository = Guard.Against.Null(internalAccountsRepository);
        _externalAccountsRepository = Guard.Against.Null(externalAccountsRepository);
        _logger = logger;
    }

    public async Task<Result<IList<ActivityFeedItemModel>>> RetrieveActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(providerModel);

        _logger.LogDebug("Attempt to retrieve activity feed amendment review entries for application with ID {ApplicationId}", providerModel.FellingLicenceId);

        var activityFeedItems = new List<ActivityFeedItemModel>();

        var assigneeHistoryItems = await _fellingLicenceApplicationRepository.GetAssigneeHistoryForApplicationAsync(
    providerModel.FellingLicenceId, cancellationToken);

        // Get only the last internal user ID
        var lastInternalUserId = assigneeHistoryItems
            .Where(x => x.Role != AssignedUserRole.Author && x.Role != AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .LastOrDefault();

        // Get only the last external user ID
        var lastExternalUserId = assigneeHistoryItems
            .Where(x => x.Role == AssignedUserRole.Author || x.Role == AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .LastOrDefault();

        var internalUsers = await _internalAccountsRepository.GetUsersWithIdsInAsync(new List<Guid> { lastInternalUserId }, cancellationToken);
        if (internalUsers.IsFailure)
        {
            _logger.LogError("Could not load internal users for assignee history activity feed items, error: {Error}", internalUsers.Error);
            return Result.Failure<IList<ActivityFeedItemModel>>("Could not load internal users for activity feed");
        }

        var externalApplicants = await _externalAccountsRepository.GetUsersWithIdsInAsync(new List<Guid> { lastExternalUserId }, cancellationToken);
        if (externalApplicants.IsFailure)
        {
            _logger.LogError("Could not load external applicants for assignee history activity feed items, error: {Error}", externalApplicants.Error);
            return Result.Failure<IList<ActivityFeedItemModel>>("Could not load external applicants for activity feed");
        }
        var internalUser = internalUsers.Value.FirstOrDefault(x => x.Id == lastInternalUserId);
        var activityFeedItemInternalUserModel = internalUser != null
            ? new ActivityFeedItemUserModel
            {
                AccountType = internalUser.AccountType,
                FirstName = internalUser.FirstName,
                Id = internalUser.Id,
                LastName = internalUser.LastName,
                IsActiveUser = internalUser.Status == Status.Confirmed
            }
            : null;

        var externalUser = externalApplicants.Value.FirstOrDefault(x => x.Id == lastExternalUserId);
        var activityFeedItemExternalUserModel = externalUser != null
            ? new ActivityFeedItemUserModel
            {
                FirstName = externalUser.FirstName,
                Id = externalUser.Id,
                LastName = externalUser.LastName,
                IsActiveUser = externalUser.Status == Applicants.Entities.UserAccount.UserAccountStatus.Active
            }
            : null;

        var amendmentReviewsResult = 
            await _fellingLicenceApplicationRepository.GetFellingAndRestockingAmendmentReviewsAsync(providerModel.FellingLicenceId, cancellationToken);

        foreach (var review in amendmentReviewsResult.Value)
        {

            var officerActivityFeedItem = new ActivityFeedItemModel
            {
                ActivityFeedItemType = ActivityFeedItemType.AmendmentOfficerReason,
                AssociatedId = review.Id,
                VisibleToApplicant = true,
                VisibleToConsultee = false,
                FellingLicenceApplicationId = providerModel.FellingLicenceId,
                CreatedTimestamp = review.AmendmentsSentDate,
                Text = review.AmendmentsReason,
                CreatedByUser = activityFeedItemInternalUserModel
            };

            activityFeedItems.Add(officerActivityFeedItem);

            if (review.ResponseReceivedDate != null && review.ApplicantDisagreementReason != null)
            {
                var applicantText = "Applicant disagreed amendments: " + review.ApplicantDisagreementReason;
                var applicantActivityFeedItem = new ActivityFeedItemModel
                {
                    ActivityFeedItemType = ActivityFeedItemType.AmendmentApplicantReason,
                    AssociatedId = review.Id,
                    VisibleToApplicant = true,
                    VisibleToConsultee = false,
                    FellingLicenceApplicationId = providerModel.FellingLicenceId,
                    CreatedTimestamp = review.ResponseReceivedDate.Value,
                    Text = applicantText,
                    CreatedByUser = activityFeedItemExternalUserModel
                };

                activityFeedItems.Add(applicantActivityFeedItem);
            }
        }

        var orderedList = activityFeedItems.OrderByDescending(x => x.CreatedTimestamp).ToList();
        return Result.Success<IList<ActivityFeedItemModel>>(orderedList);
    }

    public ActivityFeedItemType[] SupportedItemTypes()
    {
        return new[]
        {
            ActivityFeedItemType.AmendmentApplicantReason, ActivityFeedItemType.AmendmentOfficerReason
        };
    }
}