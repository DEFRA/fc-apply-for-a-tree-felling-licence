using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Services;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

/// <summary>
/// Handles use case for retrieving new comments from the Public Register for applications.
/// </summary>
public class PublicRegisterCommentsUseCase : IPublicRegisterCommentsUseCase
{
    private readonly IGetFellingLicenceApplicationForInternalUsers _getFellingLicenceApplicationService;
    private readonly IPublicRegister _publicRegister;
    private readonly IClock _clock;
    private readonly ILogger<PublicRegisterCommentsUseCase> _logger;
    private readonly INotificationHistoryService _notificationHistoryService;

    public PublicRegisterCommentsUseCase(
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplicationService,
        IPublicRegister publicRegister,
        IClock clock,
        ILogger<PublicRegisterCommentsUseCase> logger,
        INotificationHistoryService notificationHistoryService)
    {
        _getFellingLicenceApplicationService = Guard.Against.Null(getFellingLicenceApplicationService);
        _publicRegister = Guard.Against.Null(publicRegister);
        _clock = Guard.Against.Null(clock);
        _logger = Guard.Against.Null(logger);
        _notificationHistoryService = Guard.Against.Null(notificationHistoryService);
    }

    /// <summary>
    /// Gets new comments from the Public Register for all applications on the Consultation Public Register for yesterday.
    /// </summary>
    public async Task<Result<string>> GetNewCommentsFromPublicRegisterAsync(CancellationToken cancellationToken)
    {
        try
        {
            var applications = await _getFellingLicenceApplicationService.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(cancellationToken);
            int retrievedCount = 0;
            var today = LocalDate.FromDateTime(_clock.GetCurrentInstant().ToDateTimeUtc());
            var notificationModels = new List<Flo.Services.Notifications.Models.NotificationHistoryModel>();

            foreach (var app in applications)
            {
                if (string.IsNullOrWhiteSpace(app.ApplicationReference))
                    continue;

                var commentsResult = await _publicRegister.GetCaseCommentsByCaseReferenceAsync(app.ApplicationReference, cancellationToken);
                if (commentsResult.IsSuccess && commentsResult.Value != null && commentsResult.Value.Count > 0)
                {
                    retrievedCount += commentsResult.Value.Count;
                    foreach (var comment in commentsResult.Value)
                    {
                        notificationModels.Add(new Flo.Services.Notifications.Models.NotificationHistoryModel
                        {
                            Text = comment.CaseNote,
                            Source = $"{comment.Firstname} {comment.Surname}",
                            Type = NotificationType.PublicRegisterComment,
                            ApplicationReference = app.ApplicationReference,
                            ApplicationId = app.PublicRegister.FellingLicenceApplicationId,
                            CreatedTimestamp = comment.CreatedDate,
                            ExternalId = comment.GlobalID
                        });
                    }
                }
                else if (commentsResult.IsFailure)
                {
                    _logger.LogError("Failed to get comments for application {ApplicationReference}: {Error}", app.ApplicationReference, commentsResult.Error);
                }
            }

            var addResult = await _notificationHistoryService.AddNotificationHistoryListAsync(notificationModels, cancellationToken);
            if (addResult.IsFailure)
            {
                var failMessage = $"Failed to persist public register comments: {addResult.Error}";
                _logger.LogError(failMessage);
                return Result.Failure<string>(failMessage);
            }

            var message = $"Total comments retrieved: {retrievedCount}, total comments imported: {notificationModels.Count}";
            _logger.LogInformation(message);
            return Result.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve/import public register comments");
            return Result.Failure<string>($"Failed to retrieve/import public register comments: {ex.Message}");
        }
    }
}
