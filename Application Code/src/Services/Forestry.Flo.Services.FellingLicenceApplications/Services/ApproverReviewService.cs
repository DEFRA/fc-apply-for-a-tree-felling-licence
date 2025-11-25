using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public interface IApproverReviewService
{
    /// <summary>
    /// Retrieves the approver review for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The approver review model.</returns>
    Task<Maybe<ApproverReviewModel>> GetApproverReviewAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the approver review for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="model">The approver review model.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the save operation.</returns>
    Task<Result> SaveApproverReviewAsync(Guid applicationId, ApproverReviewModel model, Guid userId, CancellationToken cancellationToken);

    Task<Result> DeleteApproverReviewAsync(Guid applicationId, CancellationToken cancellationToken);
}

public class ApproverReviewService : IApproverReviewService
{
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository;
    private readonly IClock _clock;
    private readonly ILogger<ApproverReviewService> _logger;

    public ApproverReviewService(
        IFellingLicenceApplicationInternalRepository internalFlaRepository,
        IClock clock,
        ILogger<ApproverReviewService> logger)
    {
        _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
        _clock = Guard.Against.Null(clock);
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the approver review for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The approver review model.</returns>
    public async Task<Maybe<ApproverReviewModel>> GetApproverReviewAsync(
    Guid applicationId,
    CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve admins officer review entry for application with id {ApplicationId}", applicationId);
        var approverReview = await _internalFlaRepository.GetApproverReviewAsync(applicationId, cancellationToken);

        if (approverReview.HasNoValue) {
            return Maybe<ApproverReviewModel>.None;
        }
        _logger.LogDebug("Returning the approver review for application with id {ApplicationId}", applicationId);
        return Maybe.From(approverReview.Value.ToModel());
    }

    /// <summary>
    /// Saves the approver review for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="model">The approver review model.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the save operation.</returns>
    public async Task<Result> SaveApproverReviewAsync(
        Guid applicationId,
        ApproverReviewModel model,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(model);
        _logger.LogDebug("Attempting to update the ApproverReview for application with id {ApplicationId}", applicationId);

        try
        {
            if (await AssertApplication(applicationId, userId, cancellationToken) == false)
            {
                return Result.Failure("Application approver review unable to be updated");
            }

            var maybeExistingApproverReview = await _internalFlaRepository.GetApproverReviewAsync(applicationId, cancellationToken);
            var entity = maybeExistingApproverReview.HasValue ? maybeExistingApproverReview.Value : new ApproverReview();
            model.MapToEntity(entity);

            entity.LastUpdatedById = userId;
            entity.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();

            var saveResult = await _internalFlaRepository.AddOrUpdateApproverReviewAsync(entity, cancellationToken);

            if (saveResult.IsFailure)
            {
                _logger.LogError("Could not save changes to approver review, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in SaveApproverReviewAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Deletes the approver review for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the delete operation.</returns>
    public async Task<Result> DeleteApproverReviewAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to delete the ApproverReview for application with id {ApplicationId}", applicationId);

        try
        {
            var maybeApproverReview = await _internalFlaRepository.GetApproverReviewAsync(applicationId, cancellationToken);
            if (maybeApproverReview.HasNoValue)
            {
                _logger.LogWarning("No ApproverReview found for application with id {ApplicationId} to delete", applicationId);
                return Result.Failure("Approver review not found for the specified application.");
            }

            var result = await _internalFlaRepository.DeleteApproverReviewAsync(applicationId, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError("Could not delete approver review for application with id {ApplicationId}, error: {Error}", applicationId, result.Error);
                return Result.Failure(result.Error.ToString());
            }

            _logger.LogDebug("Successfully deleted the ApproverReview for application with id {ApplicationId}", applicationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in DeleteApproverReviewAsync");
            return Result.Failure(ex.Message);
        }
    }

    private async Task<bool> AssertApplication(Guid applicationId, Guid performingUserId, CancellationToken cancellationToken)
    {
        if (await AssertApplicationIsInSentForApprovalState(applicationId, cancellationToken) == false)
        {
            _logger.LogError("Cannot update approver review for application with id {ApplicationId} as it is not in the SentForApproval state", applicationId);
            return false;
        }

        if (await AssertPerformingUserIsAssignedFieldManagerOfficer(applicationId, performingUserId, cancellationToken) == false)
        {
            _logger.LogError("Cannot update woodland officer review for application with id {ApplicationId} as performing user with id {UserId} is not the assigned woodland officer",
                applicationId, performingUserId);
            return false;
        }

        _logger.LogDebug("Application with id {ApplicationId} and user with id {UserId} passed state checks to update woodland officer review details",
            applicationId, performingUserId);
        return true;
    }

    private async Task<bool> AssertPerformingUserIsAssignedFieldManagerOfficer(Guid applicationId, Guid performingUserId, CancellationToken cancellationToken)
    {
        var assigneeHistory = await _internalFlaRepository.GetAssigneeHistoryForApplicationAsync(
            applicationId, cancellationToken);
        var assignedWo = assigneeHistory.SingleOrDefault(x =>
            x.Role == AssignedUserRole.FieldManager && x.TimestampUnassigned.HasValue == false);

        return assignedWo?.AssignedUserId == performingUserId;
    }

    private async Task<bool> AssertApplicationIsInSentForApprovalState(Guid applicationId, CancellationToken cancellationToken)
    {
        var statuses = await _internalFlaRepository.GetStatusHistoryForApplicationAsync(applicationId, cancellationToken);
        return statuses.MaxBy(x => x.Created)?.Status == FellingLicenceStatus.SentForApproval;
    }
}
