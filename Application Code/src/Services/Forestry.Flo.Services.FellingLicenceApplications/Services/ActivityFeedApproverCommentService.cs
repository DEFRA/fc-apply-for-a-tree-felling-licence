using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Provides activity feed items representing approver review comments for felling licence applications.
/// </summary>
/// <remarks>
/// This service retrieves comments made by approvers during the review process and exposes them as
/// activity feed items. It supports only approver review comment items and is intended for use within internal
/// workflows where such comments are relevant. The service depends on user and application repositories to obtain
/// necessary data and is not intended for direct use by external consumers.
/// </remarks>
public class ActivityFeedApproverCommentService : IActivityFeedService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationRepository;
    private readonly ILogger<ActivityFeedApproverCommentService> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="ActivityFeedApproverCommentService"/> class.
    /// </summary>
    /// <param name="userAccountRepository">Repository of internal users to retrieve the approver account.</param>
    /// <param name="fellingLicenceApplicationRepository">Repository of felling licence applications to retrieve application details.</param>
    /// <param name="logger">A logging instance.</param>
    public ActivityFeedApproverCommentService(
        IUserAccountRepository userAccountRepository,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationRepository,
        ILogger<ActivityFeedApproverCommentService> logger)
    {
        _userAccountRepository = Guard.Against.Null(userAccountRepository);
        _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
        _logger = logger ?? new NullLogger<ActivityFeedApproverCommentService>();
    }

    /// <inheritdoc/>
    public async Task<Result<IList<ActivityFeedItemModel>>> RetrieveActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel, 
        ActorType requestingActorType,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(providerModel);

        _logger.LogDebug("Attempt to retrieve activity feed approver comments for application {ApplicationId}", providerModel.FellingLicenceId);

        var results = new List<ActivityFeedItemModel>();

        var approverReview = await _fellingLicenceApplicationRepository.GetApproverReviewAsync(
            providerModel.FellingLicenceId, cancellationToken);

        if (approverReview.HasValue)
        {
            _logger.LogDebug("Application {ApplicationId} has an approver review, checking for comments", providerModel.FellingLicenceId);

            var text = string.Empty;
            var visibleToApplicant = true;

            if (!string.IsNullOrWhiteSpace(approverReview.Value.DurationChangeReason))
            {
                text = $"Approved licence duration changed from woodland officer recommendation:\n{approverReview.Value.DurationChangeReason}";
                visibleToApplicant = false;
            }
            else if (!string.IsNullOrWhiteSpace(approverReview.Value.ReferToLocalAuthorityReason))
            {
                text = $"Reason for referral to local authority:\n{approverReview.Value.ReferToLocalAuthorityReason}";
            }
            else if (!string.IsNullOrWhiteSpace(approverReview.Value.ApplicationRefusedReason))
            {
                text = $"Reason for application refusal:\n{approverReview.Value.ApplicationRefusedReason}";
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Approver review comment found for application {ApplicationId}, retrieving user {UserAccountId}",
                    providerModel.FellingLicenceId, approverReview.Value.LastUpdatedById);

                var approver =
                    await _userAccountRepository.GetAsync(approverReview.Value.LastUpdatedById, cancellationToken);

                ActivityFeedItemUserModel? user = null;

                if (approver.IsSuccess)
                {
                    _logger.LogDebug("User account found for user id {UserAccountId}", approverReview.Value.LastUpdatedById);

                    user = new ActivityFeedItemUserModel
                    {
                        AccountType = approver.Value.AccountType,
                        FirstName = approver.Value.FirstName,
                        LastName = approver.Value.LastName,
                        Id = approver.Value.Id,
                        IsActiveUser = approver.Value.Status == Status.Confirmed
                    };
                }

                var activityFeedItem = new ActivityFeedItemModel
                {
                    ActivityFeedItemType = ActivityFeedItemType.ApproverReviewComment,
                    AssociatedId = providerModel.FellingLicenceId,
                    VisibleToApplicant = visibleToApplicant,
                    VisibleToConsultee = false,
                    FellingLicenceApplicationId = providerModel.FellingLicenceId,
                    CreatedTimestamp = approverReview.Value.LastUpdatedDate,
                    Text = text,
                    CreatedByUser = user
                };

                results.Add(activityFeedItem);
            }
        }

        return Result.Success<IList<ActivityFeedItemModel>>(results);

    }

    /// <inheritdoc/>
    public ActivityFeedItemType[] SupportedItemTypes()
    {
        return
        [
            ActivityFeedItemType.ApproverReviewComment
        ];
    }
}