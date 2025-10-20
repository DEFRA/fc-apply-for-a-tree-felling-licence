using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Service that produces a list of applications whose active amendment review is near / past the reminder
/// threshold prior to the response deadline and have not yet had a reminder notification sent.
/// </summary>
public class LateAmendmentResponseWithdrawalService : ILateAmendmentResponseWithdrawalService
{
    private readonly IFellingLicenceApplicationInternalRepository _repository;
    private readonly IClock _clock;
    private readonly ILogger<LateAmendmentResponseWithdrawalService> _logger;

    public LateAmendmentResponseWithdrawalService(
        IFellingLicenceApplicationInternalRepository repository,
        IClock clock,
        ILogger<LateAmendmentResponseWithdrawalService> logger)
    {
        _repository = Guard.Against.Null(repository);
        _clock = Guard.Against.Null(clock);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<Result<IList<LateAmendmentResponseWithdrawalModel>>> GetLateAmendmentResponseForReminderApplicationsAsync(
        TimeSpan reminderPeriod,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var applications = await _repository.GetApplicationsForLateAmendmentNotificationAsync(
            now,
            reminderPeriod,
            cancellationToken);

        var resultModels = new List<LateAmendmentResponseWithdrawalModel>();

        foreach (var app in applications)
        {
            if (app.WoodlandOfficerReview == null)
            {
                continue; // defensive; query should already ensure WO review present
            }

            var candidateReview = app.WoodlandOfficerReview.FellingAndRestockingAmendmentReviews
                .Where(r => r.AmendmentReviewCompleted != true && r.ReminderNotificationTimeStamp == null)
                .OrderByDescending(r => r.AmendmentsSentDate)
                .FirstOrDefault(r => now >= (r.ResponseDeadline - reminderPeriod));

            if (candidateReview == null)
            {
                continue;
            }

            resultModels.Add(new LateAmendmentResponseWithdrawalModel
            {
                ApplicationId = app.Id,
                ApplicationReference = app.ApplicationReference,
                PropertyName = app.SubmittedFlaPropertyDetail?.Name,
                CreatedById = app.CreatedById,
                WoodlandOwnerId = app.WoodlandOwnerId,
                AmendmentReviewId = candidateReview.Id,
                AmendmentsSentDate = candidateReview.AmendmentsSentDate,
                ResponseDeadline = candidateReview.ResponseDeadline,
                AdministrativeRegion = app.AdministrativeRegion,
                ReminderNotificationDateSent = now,
                WoodlandOfficerReviewLastUpdatedById = app.WoodlandOfficerReview.LastUpdatedById
            });
        }

        return Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(resultModels);
    }

    /// <inheritdoc />
    public async Task<Result<IList<LateAmendmentResponseWithdrawalModel>>> GetLateAmendmentResponseForWithdrawalAsync(
        CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var applications = await _repository.GetApplicationsForLateAmendmentWithdrawalAsync(
            now,
            cancellationToken);

        var resultModels = new List<LateAmendmentResponseWithdrawalModel>();

        foreach (var app in applications)
        {
            if (app.WoodlandOfficerReview == null)
            {
                continue;
            }

            var candidateReview = app.WoodlandOfficerReview.FellingAndRestockingAmendmentReviews
                .Where(r => r.AmendmentReviewCompleted != true)
                .OrderByDescending(r => r.AmendmentsSentDate)
                .FirstOrDefault(r => now > r.ResponseDeadline); // deadline passed

            if (candidateReview == null)
            {
                continue;
            }

            resultModels.Add(new LateAmendmentResponseWithdrawalModel
            {
                ApplicationId = app.Id,
                ApplicationReference = app.ApplicationReference,
                PropertyName = app.SubmittedFlaPropertyDetail?.Name,
                CreatedById = app.CreatedById,
                WoodlandOwnerId = app.WoodlandOwnerId,
                AmendmentReviewId = candidateReview.Id,
                AmendmentsSentDate = candidateReview.AmendmentsSentDate,
                ResponseDeadline = candidateReview.ResponseDeadline,
                AdministrativeRegion = app.AdministrativeRegion,
                ReminderNotificationDateSent = now,
                WoodlandOfficerReviewLastUpdatedById = app.WoodlandOfficerReview.LastUpdatedById
            });
        }

        return Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(resultModels);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateReminderNotificationTimeStampAsync(
        Guid applicationId,
        Guid amendmentReviewId,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var appMaybe = await _repository.GetAsync(applicationId, cancellationToken);
        if (appMaybe.HasNoValue)
        {
            _logger.LogWarning("Application {ApplicationId} not found when attempting to set amendment reminder timestamp", applicationId);
            return Result.Failure("Application not found");
        }

        var app = appMaybe.Value;
        var review = app.WoodlandOfficerReview?.FellingAndRestockingAmendmentReviews
            .FirstOrDefault(r => r.Id == amendmentReviewId);

        if (review == null)
        {
            _logger.LogWarning("Amendment review {ReviewId} for application {ApplicationId} not found", amendmentReviewId, applicationId);
            return Result.Failure("Amendment review not found");
        }

        if (review.ReminderNotificationTimeStamp != null)
        {
            _logger.LogDebug("Reminder timestamp already set for amendment review {ReviewId} (application {ApplicationId})", amendmentReviewId, applicationId);
            return Result.Success();
        }

        review.ReminderNotificationTimeStamp = now;

        var saveResult = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogError("Failed to persist reminder timestamp for amendment review {ReviewId} on application {ApplicationId}: {Error}", amendmentReviewId, applicationId, saveResult.Error);
            return Result.Failure(saveResult.Error.ToString());
        }

        _logger.LogInformation("Set amendment review reminder timestamp for application {ApplicationId} review {ReviewId} at {Timestamp}", applicationId, amendmentReviewId, now);
        return Result.Success();
    }
}