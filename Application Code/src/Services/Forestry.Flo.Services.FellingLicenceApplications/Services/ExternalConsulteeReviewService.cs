using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Implementation of <see cref="IExternalConsulteeReviewService"/> that uses a
/// <see cref="IFellingLicenceApplicationInternalRepository"/> implementation to access relevant data.
/// </summary>
public class ExternalConsulteeReviewService : IExternalConsulteeReviewService
{
    private readonly IClock _clock;
    private readonly ILogger<ExternalConsulteeReviewService> _logger;
    private readonly IFellingLicenceApplicationInternalRepository _repository;

    public ExternalConsulteeReviewService(
        IFellingLicenceApplicationInternalRepository repository,
        IClock clock,
        ILogger<ExternalConsulteeReviewService> logger)
    {
        _repository = Guard.Against.Null(repository);
        _clock = Guard.Against.Null(clock);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Maybe<ExternalAccessLinkModel>> VerifyAccessCodeAsync(
        Guid applicationId, 
        Guid accessCode, 
        string emailAddress, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve external access link for application with id {ApplicationId}", applicationId);

        var link = await _repository.GetValidExternalAccessLinkAsync(applicationId, accessCode, emailAddress,
            _clock.GetCurrentInstant().ToDateTimeUtc(), cancellationToken);

        if (link.HasNoValue)
        {
            _logger.LogDebug("No external access link was found with the given values");
            return Maybe<ExternalAccessLinkModel>.None;
        }

        var result = new ExternalAccessLinkModel(
            link.Value.Name,
            link.Value.ContactEmail,
            link.Value.Purpose,
            link.Value.CreatedTimeStamp,
            link.Value.ExpiresTimeStamp,
            applicationId,
            link.Value.LinkType,
            link.Value.SharedSupportingDocuments);

        return Maybe<ExternalAccessLinkModel>.From(result);
    }

    /// <inheritdoc />
    public async Task<List<ConsulteeCommentModel>> RetrieveConsulteeCommentsForAccessCodeAsync(
        Guid applicationId, 
        Guid accessCode,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve consultee comments for application id {ApplicationId} and access code {AccessCode}", applicationId, accessCode);

        var comments = await _repository.GetConsulteeCommentsAsync(
            applicationId,
            accessCode,
            cancellationToken);

        return comments.Select(x => new ConsulteeCommentModel
        {
            AuthorContactEmail = x.AuthorContactEmail,
            AuthorName = x.AuthorName,
            Comment = x.Comment,
            CreatedTimestamp = x.CreatedTimestamp,
            FellingLicenceApplicationId = x.FellingLicenceApplicationId,
            ConsulteeAttachmentIds = x.DocumentIds
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<Result<ConsulteeCommentNotificationModel>> AddCommentAsync(
        ConsulteeCommentModel model, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to add a new consultee comment to application with id {ApplicationId}", model.FellingLicenceApplicationId);

        var comment = new ConsulteeComment
        {
            FellingLicenceApplicationId = model.FellingLicenceApplicationId,
            CreatedTimestamp = model.CreatedTimestamp,
            AuthorName = model.AuthorName,
            AuthorContactEmail = model.AuthorContactEmail,
            Comment = model.Comment,
            DocumentIds = model.ConsulteeAttachmentIds.ToList(),
            AccessCode = model.AccessCode
        };
        var result = await _repository.AddConsulteeCommentAsync(comment, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not add consultee comment, error: {Error}", result.Error);
            return Result.Failure<ConsulteeCommentNotificationModel>("Could not add consultee comment");
        }

        var application = await _repository.GetAsync(model.FellingLicenceApplicationId, cancellationToken);

        if (application.HasNoValue)
        {
            // this shouldn't be possible, as the application must exist for the comment to have been added successfully, but we should still handle it just in case
            _logger.LogError("Could not find application with id {ApplicationId}", model.FellingLicenceApplicationId);
            return Result.Failure<ConsulteeCommentNotificationModel>("Could not find application");
        }

        var assignedStaff = application.Value.AssigneeHistories
            .Where(x => x.Role != AssignedUserRole.Author
                        && x.Role != AssignedUserRole.Applicant
                        && x.TimestampUnassigned.HasNoValue())
            .Select(x => x.AssignedUserId)
            .Distinct();

        var isWithApplicant = FellingLicenceStatusConstants.SubmitStatuses.Contains(
            application.Value.GetCurrentStatus());

        var notificationModel = new ConsulteeCommentNotificationModel
        {
            AdminHub = application.Value.AdministrativeRegion,
            ApplicationReference = application.Value.ApplicationReference,
            AssignedFcStaff = assignedStaff.ToArray(),
            PropertyName = isWithApplicant
                ? null
                : application.Value.SubmittedFlaPropertyDetail!.Name,
            LinkedPropertyProfileId = isWithApplicant
                ? application.Value.LinkedPropertyProfile!.PropertyProfileId
                : null
        };

        return Result.Success(notificationModel);
    }
}