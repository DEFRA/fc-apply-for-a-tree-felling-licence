using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
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
            applicationId);

        return Maybe<ExternalAccessLinkModel>.From(result);
    }

    /// <inheritdoc />
    public async Task<List<ConsulteeCommentModel>> RetrieveConsulteeCommentsForAuthorAsync(
        Guid applicationId, string emailAddress,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve consultee comments for application id {ApplicationId} and contact email {ContactEmail}", applicationId, emailAddress);

        var comments = await _repository.GetConsulteeCommentsAsync(
            applicationId,
            emailAddress,
            cancellationToken);

        return comments.Select(x => new ConsulteeCommentModel
        {
            ApplicableToSection = x.ApplicableToSection,
            AuthorContactEmail = x.AuthorContactEmail,
            AuthorName = x.AuthorName,
            Comment = x.Comment,
            CreatedTimestamp = x.CreatedTimestamp,
            FellingLicenceApplicationId = x.FellingLicenceApplicationId
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<Result> AddCommentAsync(
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
            ApplicableToSection = model.ApplicableToSection,
            Comment = model.Comment
        };
        var result = await _repository.AddConsulteeCommentAsync(comment, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not add consultee comment, error: {Error}", result.Error);
            return Result.Failure("Could not add consultee comment");
        }

        return Result.Success();
    }
}