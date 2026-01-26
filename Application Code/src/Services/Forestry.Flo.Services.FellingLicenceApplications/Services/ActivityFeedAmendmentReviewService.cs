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

        var amendmentReviewsResult = 
            await _fellingLicenceApplicationRepository.GetFellingAndRestockingAmendmentReviewsAsync(providerModel.FellingLicenceId, cancellationToken);

        if (amendmentReviewsResult.IsFailure)
        {
            _logger.LogError("Could not load amendment reviews for application with ID {ApplicationId}, error: {Error}", providerModel.FellingLicenceId, amendmentReviewsResult.Error);
            return Result.Failure<IList<ActivityFeedItemModel>>(amendmentReviewsResult.Error);
        }

        var internalUsersResult = await GetActivityFeedInternalUsers(amendmentReviewsResult.Value, cancellationToken);
        if (internalUsersResult.IsFailure)
        {
            return Result.Failure<IList<ActivityFeedItemModel>>(internalUsersResult.Error);
        }
        var activityFeedItemInternalUsers = internalUsersResult.Value;

        var externalUsersResult = await GetActivityFeedExternalUsers(amendmentReviewsResult.Value, cancellationToken);
        if (externalUsersResult.IsFailure)
        {
            return Result.Failure<IList<ActivityFeedItemModel>>(externalUsersResult.Error);
        }
        var activityFeedItemExternalUsers = externalUsersResult.Value;

        foreach (var review in amendmentReviewsResult.Value)
        {
            var activityFeedItemInternalUserModel = activityFeedItemInternalUsers.FirstOrDefault(x => x.Id == review.AmendingWoodlandOfficerId);

            var officerActivityFeedItem = new ActivityFeedItemModel
            {
                ActivityFeedItemType = ActivityFeedItemType.AmendmentOfficerReason,
                AssociatedId = review.Id,
                VisibleToApplicant = true,
                VisibleToConsultee = false,
                FellingLicenceApplicationId = providerModel.FellingLicenceId,
                CreatedTimestamp = review.AmendmentsSentDate,
                Text = review.AmendmentsReason ?? string.Empty,
                CreatedByUser = activityFeedItemInternalUserModel
            };

            activityFeedItems.Add(officerActivityFeedItem);

            if (review.ResponseReceivedDate != null)
            {
                var applicantText = review.ApplicantAgreed is false
                    ? "Applicant disagreed with amendments: " + review.ApplicantDisagreementReason
                    : "Applicant agreed to amendments";

                var activityFeedItemExternalUserModel = activityFeedItemExternalUsers.FirstOrDefault(x => x.Id == review.RespondingApplicantId);

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
        return
        [
            ActivityFeedItemType.AmendmentApplicantReason, ActivityFeedItemType.AmendmentOfficerReason
        ];
    }

    private async Task<Result<List<ActivityFeedItemUserModel>>> GetActivityFeedInternalUsers(IEnumerable<FellingAndRestockingAmendmentReview> amendmentReviewsResult, CancellationToken cancellationToken)
    {
        var internalIds = amendmentReviewsResult
            .Where(r => r.AmendingWoodlandOfficerId.HasValue)
            .Select(r => r.AmendingWoodlandOfficerId!.Value)
            .ToList();

        var internalUsers = await _internalAccountsRepository.GetUsersWithIdsInAsync(internalIds, cancellationToken);
        if (internalUsers.IsFailure)
        {
            _logger.LogError("Could not load internal users for assignee history activity feed items, error: {Error}", internalUsers.Error);
            return Result.Failure<List<ActivityFeedItemUserModel>>("Could not load internal users for activity feed");
        }

        var activityFeedItemInternalUsers = internalUsers.Value
            .Select(internalUser => new ActivityFeedItemUserModel
            {
                AccountType = internalUser.AccountType,
                FirstName = internalUser.FirstName,
                Id = internalUser.Id,
                LastName = internalUser.LastName,
                IsActiveUser = internalUser.Status == Status.Confirmed
            })
            .ToList();

        return Result.Success(activityFeedItemInternalUsers);
    }

    private async Task<Result<List<ActivityFeedItemUserModel>>> GetActivityFeedExternalUsers(IEnumerable<FellingAndRestockingAmendmentReview> amendmentReviewsResult, CancellationToken cancellationToken)
    {
        var externalIds = amendmentReviewsResult
            .Where(r => r.RespondingApplicantId.HasValue)
            .Select(r => r.RespondingApplicantId!.Value)
            .ToList();

        var externalApplicants = await _externalAccountsRepository.GetUsersWithIdsInAsync(externalIds, cancellationToken);
        if (externalApplicants.IsFailure)
        {
            _logger.LogError("Could not load external applicants for assignee history activity feed items, error: {Error}", externalApplicants.Error);
            return Result.Failure<List<ActivityFeedItemUserModel>>("Could not load external applicants for activity feed");
        }

        var activityFeedItemExternalUsers = externalApplicants.Value
            .Select(externalUser => new ActivityFeedItemUserModel
            {
                FirstName = externalUser.FirstName,
                Id = externalUser.Id,
                LastName = externalUser.LastName,
                IsActiveUser = externalUser.Status == Applicants.Entities.UserAccount.UserAccountStatus.Active
            })
            .ToList();

        return Result.Success(activityFeedItemExternalUsers);
    }
}