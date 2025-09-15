using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;
using Notify.Models.Responses;

namespace Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;

public class ExternalConsulteeInviteUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IExternalConsulteeReviewService _externalConsulteeReviewService;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly ISendNotifications _notificationService;
    private readonly IAuditService<ExternalConsulteeInviteUseCase> _auditService;
    private readonly ILogger<ExternalConsulteeInviteUseCase> _logger;
    private readonly IClock _clock;
    private readonly UserInviteOptions _settings;
    private readonly RequestContext _requestContext;
    private const string ApplicationNotFoundError = "Could not locate Felling Licence Application with the given id";

    public ExternalConsulteeInviteUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        ISendNotifications notificationService,
        IAuditService<ExternalConsulteeInviteUseCase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IExternalConsulteeReviewService externalConsulteeReviewService,
        ILogger<ExternalConsulteeInviteUseCase> logger,
        IClock clock,
        IOptions<UserInviteOptions> options,
        RequestContext requestContext) : base(
        internalUserAccountService,
        externalUserAccountService,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        agentAuthorityService,
        getConfiguredFcAreasService)
    {
        _externalConsulteeReviewService = Guard.Against.Null(externalConsulteeReviewService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _notificationService = Guard.Against.Null(notificationService);
        _auditService = Guard.Against.Null(auditService);
        _logger = Guard.Against.Null(logger);
        _clock = Guard.Against.Null(clock);
        _settings = Guard.Against.Null(options).Value;
        _requestContext = Guard.Against.Null(requestContext);
    }

    /// <summary>
    /// Retrieves the existing invited external consultees and the not needed/complete status of
    /// consultations for the application.
    /// </summary>
    /// <param name="applicationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="ExternalConsulteeIndexViewModel"/> view model for the consultee invite index page.</returns>
    public async Task<Result<ExternalConsulteeIndexViewModel>> GetConsulteeInvitesIndexViewModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (hasValue)
        {
            return await ExtractApplicationSummaryAsync(fla, cancellationToken)
                .Map(applicationSummary => new ExternalConsulteeIndexViewModel
                {
                    ApplicationId = applicationId,
                    FellingLicenceApplicationSummary = applicationSummary,
                    InviteLinks = ModelMapping.ToExternalInviteLinkList(fla.ExternalAccessLinks, fla.ConsulteeComments),
                    ApplicationNeedsConsultations = fla.WoodlandOfficerReview?.ApplicationNeedsConsultations,
                    ConsultationsComplete = fla.WoodlandOfficerReview?.ConsultationsComplete ?? false,
                    CurrentDateTimeUtc = _clock.GetCurrentInstant().ToDateTimeUtc()
                })
                .OnFailure(e => { _logger.LogError(e); });
        }

        _logger.LogError(ApplicationNotFoundError);
        return Result.Failure<ExternalConsulteeIndexViewModel>(ApplicationNotFoundError);
    }

    /// <summary>
    /// Updates the woodland officer review record to set consultations as not needed.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if successful.</returns>
    public async Task<Result> SetDoesNotRequireConsultationsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Setting woodland officer review consultations to not needed");

        return await _updateWoodlandOfficerReviewService.UpdateConsultationsStatusAsync(
            applicationId, user.UserAccountId!.Value, false, false, cancellationToken);
    }

    /// <summary>
    /// Updates the woodland officer review record to set consultations as complete.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if successful.</returns>
    public async Task<Result> SetConsultationsCompleteAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Setting woodland officer review consultations to complete");

        return await _updateWoodlandOfficerReviewService.UpdateConsultationsStatusAsync(
            applicationId, user.UserAccountId!.Value, null, true, cancellationToken);
    }

    /// <summary>
    /// Gets a new External Consultee Invite View Model from application data by the given application id.
    /// </summary>
    /// <param name="applicationId">The application id</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A <see cref="ExternalConsulteeInviteFormModel"/> model for inviting a new consultee.</returns>
    public async Task<Result<ExternalConsulteeInviteFormModel>> GetNewExternalConsulteeInviteViewModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (hasValue)
        {
            var prCompleted =
                fla.PublicRegister != null
                && (fla.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister
                    || fla.PublicRegister.ConsultationPublicRegisterPublicationTimestamp.HasValue);
            var prExempt = fla.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister;

            var documentModels = ModelMapping
                .ToDocumentModelList(fla.Documents?
                    .Where(x => x.VisibleToConsultee && x.DeletionTimestamp.HasNoValue())
                    .OrderByDescending(x => x.CreatedTimestamp)
                    .ToList() ?? [])
                .ToList();

            return await ExtractApplicationSummaryAsync(fla, cancellationToken)
                .Map(applicationSummary =>
                {
                    return new ExternalConsulteeInviteFormModel
                    {
                        FellingLicenceApplicationSummary = applicationSummary,
                        ApplicationId = applicationId,
                        ExemptFromConsultationPublicRegister = prExempt,
                        PublicRegisterAlreadyCompleted = prCompleted,
                        SelectedDocumentIds = documentModels.Select(d => (Guid?)d.Id).ToList(),
                        ConsulteeDocuments = documentModels
                    };
                })
                .OnFailure(e => { _logger.LogError(e); });
        }
        _logger.LogError(ApplicationNotFoundError);
        return Result.Failure<ExternalConsulteeInviteFormModel>(ApplicationNotFoundError);
    }

    /// <summary>
    /// Send an invitation to an external consultee to review the application.
    /// </summary>
    /// <param name="externalConsulteeInviteModel">The consultee invite view model</param>
    /// <param name="applicationId">The application Id</param>
    /// <param name="user">A current system user</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result of the operation.</returns>
    public async Task<Result> InviteExternalConsulteeAsync(
        ExternalConsulteeInviteModel externalConsulteeInviteModel,
        Guid applicationId,
        InternalUser user, 
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (!hasValue)
        {
            _logger.LogError(ApplicationNotFoundError);
            return Result.Failure(ApplicationNotFoundError);
        }

        var endDate = _clock.GetCurrentInstant().ToDateTimeUtc().AddDays(_settings.InviteLinkExpiryDays);

        var adminHubFooter = await GetAdminHubAddressDetailsAsync(fla.AdministrativeRegion, cancellationToken)
            .ConfigureAwait(false);

        var externalConsulteeInvite = new ExternalConsulteeInviteDataModel
        {
            ApplicationReference = fla.ApplicationReference,
            ConsulteeName = externalConsulteeInviteModel.ConsulteeName,
            EmailText = externalConsulteeInviteModel.ConsulteeEmailText,
            SenderName = user.FullName!,
            SenderEmail = user.EmailAddress!,
            CommentsEndDate = DateTimeDisplay.GetDateDisplayString(endDate),
            ViewApplicationURL = externalConsulteeInviteModel.ExternalAccessLink,
            AdminHubFooter = adminHubFooter
        };

        var accessLink = new ExternalAccessLink
        {
            Name = externalConsulteeInviteModel.ConsulteeName,
            Purpose = externalConsulteeInviteModel.Purpose!,
            AccessCode = externalConsulteeInviteModel.ExternalAccessCode,
            ContactEmail = externalConsulteeInviteModel.Email,
            FellingLicenceApplicationId = applicationId,
            CreatedTimeStamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
            ExpiresTimeStamp = _clock.GetCurrentInstant().ToDateTimeUtc().AddDays(_settings.InviteLinkExpiryDays),
            IsMultipleUseAllowed = true,
            LinkType = ExternalAccessLinkType.ConsulteeInvite,
            SharedSupportingDocuments = externalConsulteeInviteModel.SelectedDocumentIds
        };

        var notificationType = externalConsulteeInviteModel.ExemptFromConsultationPublicRegister
            ? NotificationType.ExternalConsulteeInvite
            : NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo;

        return await FellingLicenceRepository.AddExternalAccessLinkAsync(accessLink, cancellationToken)
            .MapError(e => $"External access link creation error, {ExtractDatabaseError(e)}, applicationId: {accessLink.FellingLicenceApplicationId}")
            .Ensure(async () => await _updateWoodlandOfficerReviewService.UpdateConsultationsStatusAsync(
                applicationId, user.UserAccountId!.Value, true, false, cancellationToken))
            .Ensure(async () => await _notificationService.SendNotificationAsync(
                externalConsulteeInvite,
                notificationType,
                new NotificationRecipient(externalConsulteeInviteModel.Email, externalConsulteeInviteModel.ConsulteeName),
                cancellationToken: cancellationToken))
            .Tap(async () => await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationSent, user, cancellationToken))
            .OnFailure(async e =>
            {
                _logger.LogError(e);
                await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationFailure, user, cancellationToken, e);
            });
    }

    /// <summary>
    /// Gets a view model to display the received comments for a given application id and access code.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve the received comments for.</param>
    /// <param name="accessCode">The access code of the particular invite to retrieve comments for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ReceivedConsulteeCommentsViewModel"/> model of the data to display.</returns>
    public async Task<Result<ReceivedConsulteeCommentsViewModel>> GetReceivedCommentsAsync(
        Guid applicationId,
        Guid accessCode,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to retrieve information to display consultee comments received for application id {ApplicationId} and access link code {AccessCode}",
            applicationId,
            accessCode);

        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (!hasValue)
        {
            _logger.LogError("Could not locate Felling Licence Application with the given id {id}", applicationId);
            return Result.Failure<ReceivedConsulteeCommentsViewModel>($"Could not locate Felling Licence Application with the given id {applicationId}");
        }

        var (_, isFailure, flaModel, error) = await ExtractApplicationSummaryAsync(fla, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Could not load application summary model: {Error}", error);
            return Result.Failure<ReceivedConsulteeCommentsViewModel>($"Could not load application summary model: {error}");
        }

        var comments = await _externalConsulteeReviewService.RetrieveConsulteeCommentsForAccessCodeAsync(
            applicationId,
            accessCode,
            cancellationToken);

        var items = comments
            .OrderByDescending(x => x.CreatedTimestamp)
            .Select(x => new ReceivedConsulteeCommentModel
            {
                AuthorName = x.AuthorName,
                Comment = x.Comment,
                CreatedTimestamp = x.CreatedTimestamp,
                Attachments = GetAttachments(x.ConsulteeAttachmentIds.ToList(), fla.Documents)
            });

        var result = new ReceivedConsulteeCommentsViewModel
        {
            ApplicationId = applicationId,
            ConsulteeName = fla.ExternalAccessLinks?.FirstOrDefault(x => x.AccessCode == accessCode)?.Name,
            Email = fla.ExternalAccessLinks?.FirstOrDefault(x => x.AccessCode == accessCode)?.ContactEmail,
            FellingLicenceApplicationSummary = flaModel,
            ReceivedComments = items.ToList()
        };

        return Result.Success(result);
    }

    private Task PublishAuditEvent(ExternalAccessLink accessLink, string eventName, InternalUser user,
        CancellationToken cancellationToken,
        string? error = null) =>
        _auditService.PublishAuditEventAsync(
            new AuditEvent(
                eventName,
                accessLink.FellingLicenceApplicationId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    InvitedByUserId = user.UserAccountId,
                    ConsulteeName = accessLink.Name,
                    ConsulteeEmailAddress = accessLink.ContactEmail,
                    ApplicationId = accessLink.FellingLicenceApplicationId,
                    InviteExpiryDateTime = accessLink.ExpiresTimeStamp,
                    Error = error
                }),
            cancellationToken);

    private static string ExtractDatabaseError(UserDbErrorReason e) =>
        e == UserDbErrorReason.NotUnique
            ? "the access link already exists"
            : "a database error";

    private static Dictionary<Guid, string> GetAttachments(IList<Guid>? consulteeAttachmentIds, IList<Document>? flaDocuments)
    {
        if (consulteeAttachmentIds == null || !consulteeAttachmentIds.Any() || flaDocuments == null || !flaDocuments.Any())
        {
            return new Dictionary<Guid, string>();
        }

        var result = new Dictionary<Guid, string>();
        foreach (var consulteeAttachmentId in consulteeAttachmentIds)
        {
            var document = flaDocuments.FirstOrDefault(x => x.Id == consulteeAttachmentId);
            if (document != null)
            {
                result[consulteeAttachmentId] = document.FileName;
            }
        }

        return result;
    }
}