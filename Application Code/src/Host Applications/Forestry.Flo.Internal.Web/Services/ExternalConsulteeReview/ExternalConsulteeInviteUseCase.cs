using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;
using System.Text.RegularExpressions;
using Forestry.Flo.Services.FellingLicenceApplications.Services;

namespace Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;

public class ExternalConsulteeInviteUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ISendNotifications _notificationService;
    private readonly IAuditService<ExternalConsulteeInviteUseCase> _auditService;
    private readonly ILogger<ExternalConsulteeInviteUseCase> _logger;
    private readonly IClock _clock;
    private readonly UserInviteOptions _settings;
    private readonly RequestContext _requestContext;
    private const string EmailText = "You’ve been invited to review a tree felling licence application." +
                                     " Follow the link in this email to view the application and respond.";
    private const string ApplicationNotFoundError = "Could not locate Felling Licence Application with the given id";
    private const string DefaultPurpose = "Statutory Consultation";

    public ExternalConsulteeInviteUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        ISendNotifications notificationService,
        IFileStorageService fileStorageService,
        IAuditService<ExternalConsulteeInviteUseCase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
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
        _fileStorageService = Guard.Against.Null(fileStorageService);
        _notificationService = Guard.Against.Null(notificationService);
        _auditService = Guard.Against.Null(auditService);
        _logger = Guard.Against.Null(logger);
        _clock = Guard.Against.Null(clock);
        _settings = Guard.Against.Null(options).Value;
        _requestContext = Guard.Against.Null(requestContext);
    }

    /// <summary>
    /// Retrieves the External Consultee Invite View Model from application data by the given application id and the invite model.
    /// </summary>
    /// <param name="applicationId">The application id</param>
    /// <param name="inviteModel">The invite model</param>
    /// <param name="returnUrl">A return URL</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result containing the model</returns>
    public async Task<Result<ExternalConsulteeInviteViewModel>> RetrieveExternalConsulteeInviteViewModelAsync(
        Guid applicationId,
        ExternalConsulteeInviteModel? inviteModel, 
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var accessCode = Guid.NewGuid();
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (hasValue)
        {
            var prCompleted =
                fla.PublicRegister != null
                && (fla.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister
                    || fla.PublicRegister.ConsultationPublicRegisterPublicationTimestamp.HasValue);
            var prExempt = fla.PublicRegister is { WoodlandOfficerSetAsExemptFromConsultationPublicRegister: true };

            return await ExtractApplicationSummaryAsync(fla, cancellationToken)
                .Map(applicationSummary =>
                {
                    inviteModel ??= new ExternalConsulteeInviteModel
                    {
                        Id = Guid.NewGuid(),
                        ConsulteeEmailText = EmailText,
                        SelectedDocumentIds = [],
                        ExternalAccessCode = accessCode,
                        Purpose = DefaultPurpose,
                        ExemptFromConsultationPublicRegister = prExempt
                    };

                    return new ExternalConsulteeInviteViewModel
                    {
                        ExternalConsulteeInvite = inviteModel,
                        InviteFormModel = new ExternalConsulteeInviteFormModel
                        {
                            FellingLicenceApplicationSummary = applicationSummary,
                            Id = inviteModel.Id,
                            ApplicationId = applicationSummary.Id,
                            Email = inviteModel.Email,
                            Purpose = inviteModel.Purpose,
                            ConsulteeName = inviteModel.ConsulteeName,
                            ReturnUrl = returnUrl,
                            InviteLinks = ModelMapping.ToExternalInviteLinkList(fla.ExternalAccessLinks),
                            ExemptFromConsultationPublicRegister = prExempt,
                            PublicRegisterAlreadyCompleted = prCompleted
                        }
                    };
                })
                .OnFailure(e => { _logger.LogError(e); });
        }
        _logger.LogError(ApplicationNotFoundError);
        return Result.Failure<ExternalConsulteeInviteViewModel>(ApplicationNotFoundError);
    }

    /// <summary>
    /// Retrieves the External Consultee Email Text Model from application data and the given invite model.
    /// </summary>
    /// <param name="applicationId">The application id</param>
    /// <param name="returnUrl">A return URL</param>
    /// <param name="inviteModel">The invite model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result containing the model</returns>
    public async Task<Result<ExternalConsulteeEmailTextModel>> RetrieveExternalConsulteeEmailTextViewModelAsync(
        Guid applicationId, 
        string returnUrl, 
        ExternalConsulteeInviteModel inviteModel,
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (hasValue)
            return await ExtractApplicationSummaryAsync(fla, cancellationToken)
                .Map(applicationSummary => new ExternalConsulteeEmailTextModel
                {
                    Email = inviteModel.Email,
                    ConsulteeName = inviteModel.ConsulteeName,
                    ConsulteeEmailText = inviteModel.ConsulteeEmailText,
                    FellingLicenceApplicationSummary = applicationSummary,
                    Id = inviteModel.Id,
                    ApplicationId = applicationSummary.Id,
                    ReturnUrl = returnUrl,
                    ApplicationDocumentsCount = fla.Documents?.Count ?? 0
                })
                .OnFailure(e => { _logger.LogError(e); });
        
        _logger.LogError(ApplicationNotFoundError);
        return Result.Failure<ExternalConsulteeEmailTextModel>(ApplicationNotFoundError);
    }

    /// <summary>
    /// Creates a confirmation of the external consultee invite.
    /// </summary>
    /// <param name="returnUrl">A return URL</param>
    /// <param name="inviteModel">A consultee invite model</param>
    /// <param name="user">The invite sender name</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <param name="applicationId">The application id</param>
    /// <returns>A confirmation model</returns>
    public async Task<Result<ExternalConsulteeInviteConfirmationModel>> CreateExternalConsulteeInviteConfirmationAsync(
        Guid applicationId,
        string returnUrl,
        ExternalConsulteeInviteModel inviteModel, 
        InternalUser user, 
        CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (!hasValue)
        {
            _logger.LogError(ApplicationNotFoundError);
            return Result.Failure<ExternalConsulteeInviteConfirmationModel>(ApplicationNotFoundError);
        }

        var endDate = _clock.GetCurrentInstant().ToDateTimeUtc().AddDays(28);

        var adminHubFooter = await GetAdminHubAddressDetailsAsync(fla.AdministrativeRegion, cancellationToken)
            .ConfigureAwait(false);
        
        var emailModel = new ExternalConsulteeInviteDataModel
        {
            ConsulteeName = inviteModel.ConsulteeName,
            ApplicationReference = fla.ApplicationReference,
            EmailText = inviteModel.ConsulteeEmailText,
            SenderName = user.FullName!,
            SenderEmail = user.EmailAddress!,
            CommentsEndDate = DateTimeDisplay.GetDateDisplayString(endDate),
            ViewApplicationURL = inviteModel.ExternalAccessLink,
            AdminHubFooter = adminHubFooter
        };

        var attachments = await AddNotificationAttachments(
            ModelMapping.ToSupportingDocumentList(fla.Documents?.Where(x => x.VisibleToConsultee).ToList()).ToList(),
            inviteModel.SelectedDocumentIds,
            cancellationToken);

        var notificationType = inviteModel.ExemptFromConsultationPublicRegister
            ? NotificationType.ExternalConsulteeInvite
            : NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo;

        return
            await _notificationService.CreateNotificationContentAsync(
                    emailModel,
                    notificationType,
                    attachments.IsSuccess ? attachments.Value?.ToArray() : null,
                    true,
                    cancellationToken)
                .Map(content =>
                {
                    var supportingDocumentsDictionary =
                        ModelMapping.ToSupportingDocumentList(fla.Documents).ToDictionary(d => d.Id, d => d);

                    return new ExternalConsulteeInviteConfirmationModel
                    {
                        Email = inviteModel.Email,
                        EmailContent = content,
                        PreviewEmailContent = FormatTextForDisplayInWebPage(content),
                        ConsulteeName = inviteModel.ConsulteeName,
                        AttachedDocuments =
                            inviteModel.SelectedDocumentIds.Select(i =>
                                supportingDocumentsDictionary[i]).ToList(),
                        ApplicationId = fla.Id,
                        Id = inviteModel.Id,
                        ReturnUrl = returnUrl,
                        ApplicationDocumentCount = supportingDocumentsDictionary.Count
                    };
                })
                .Bind(async r =>
                    await ExtractApplicationSummaryAsync(fla, cancellationToken)
                        .Map(s =>
                        {
                            r.FellingLicenceApplicationSummary = s;
                            return r;
                        })
                );
    }

    /// <summary>
    /// Send an invitation to an external consultee to review the application.
    /// </summary>
    /// <param name="externalConsulteeInviteModel">The consultee invite view model</param>
    /// <param name="applicationId">The application Id</param>
    /// <param name="user">A current system user</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result of the operation</returns>
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
            return Result.Failure<ExternalConsulteeInviteConfirmationModel>(ApplicationNotFoundError);
        }

        var endDate = _clock.GetCurrentInstant().ToDateTimeUtc().AddDays(28);

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
            FellingLicenceApplicationId = fla.Id,
            CreatedTimeStamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
            ExpiresTimeStamp = _clock.GetCurrentInstant().ToDateTimeUtc().AddDays(_settings.InviteLinkExpiryDays),
            IsMultipleUseAllowed = true
        };

        var notificationType = externalConsulteeInviteModel.ExemptFromConsultationPublicRegister
            ? NotificationType.ExternalConsulteeInvite
            : NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo;

        return await AddNotificationAttachments(
                ModelMapping.ToSupportingDocumentList(fla.Documents?.Where(x => x.VisibleToConsultee).ToList()).ToList(), 
                externalConsulteeInviteModel.SelectedDocumentIds, 
                cancellationToken)
            .Ensure(async _ => await AddOrUpdateApplicationExternalLink(fla, accessLink, cancellationToken)
                .MapError(e => $"External access link creation error, {ExtractDatabaseError(e)}, applicationId: {accessLink.FellingLicenceApplicationId} "))
            .Ensure(async attachments => await _notificationService.SendNotificationAsync(
                externalConsulteeInvite,
                notificationType,
                new NotificationRecipient(externalConsulteeInviteModel.Email, externalConsulteeInviteModel.ConsulteeName),
                attachments: attachments?.ToArray(),
                cancellationToken: cancellationToken))
            .Tap(async () =>
                await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationSent, user, cancellationToken))
            .OnFailure(async e =>
            {
                _logger.LogError(e);
                await PublishAuditEvent(accessLink, AuditEvents.ExternalConsulteeInvitationFailure, user, cancellationToken, e);
            });
    }

    private  Task<UnitResult<UserDbErrorReason>> AddOrUpdateApplicationExternalLink(
        Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication fla, ExternalAccessLink link,
        CancellationToken cancellationToken)
    {
        var inviteLink = fla.ExternalAccessLinks.FirstOrDefault(l =>
            l.Name == link.Name
            && l.ContactEmail == link.ContactEmail
            && l.Purpose == link.Purpose);
        if (inviteLink is null)
            return  FellingLicenceRepository.AddExternalAccessLinkAsync(link, cancellationToken);

        link.Id = inviteLink.Id;
        link.CreatedTimeStamp = inviteLink.CreatedTimeStamp;
        return  FellingLicenceRepository.UpdateExternalAccessLinkAsync(link, cancellationToken);
    }

    /// <summary>
    /// Checks if an invitation for the application review has been already sent to the consultee.
    /// </summary>
    /// <param name="inviteModel">The external consultee invite model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>True result if the invite has already been sent, otherwise returns false</returns>
    public async Task<Result<bool>> CheckIfEmailHasAlreadyBeenSentToConsulteeForThisPurposeAsync(
        ExternalConsulteeInviteFormModel inviteModel, CancellationToken cancellationToken)
    {
        try
        {
            var result = (await FellingLicenceRepository.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(
                inviteModel.ApplicationId, inviteModel.ConsulteeName,
                inviteModel.Email,
                inviteModel.Purpose!,
                cancellationToken)).Any();

            return Result.Success(result);
        }
        catch (Exception e)
        {
            _logger.LogError("Consultee email check failed, details: {Exception}", e);
            return Result.Failure<bool>("Consultee email check failed");
        }
    }

    /// <summary>
    /// Retrieves the External Consultee Email Documents Model from application data and the given invite model.
    /// </summary>
    /// <param name="applicationId">The application Id</param>
    /// <param name="returnUrl">A return URL</param>
    /// <param name="inviteModel">The invite model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result containing the model</returns>
    public async Task<Result<ExternalConsulteeEmailDocumentsModel>>
        RetrieveExternalConsulteeEmailDocumentsViewModelAsync(Guid applicationId, string returnUrl,
            ExternalConsulteeInviteModel inviteModel, CancellationToken cancellationToken)
    {
        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);

        if (hasValue)
            return await ExtractApplicationSummaryAsync(fla, cancellationToken)
                .Map(applicationSummary => new ExternalConsulteeEmailDocumentsModel
                {
                    Email = inviteModel.Email,
                    ConsulteeName = inviteModel.ConsulteeName,
                    SupportingDocuments = ModelMapping.ToSupportingDocumentList(fla.Documents?.Where(x => x.VisibleToConsultee).ToList()).ToList(),
                    SelectedDocumentIds = inviteModel.SelectedDocumentIds,
                    FellingLicenceApplicationSummary = applicationSummary,
                    Id = inviteModel.Id,
                    ApplicationId = applicationSummary.Id,
                    ReturnUrl = returnUrl
                })
                .OnFailure(e => { _logger.LogError(e); });
        
        _logger.LogError(ApplicationNotFoundError);
        return Result.Failure<ExternalConsulteeEmailDocumentsModel>(ApplicationNotFoundError);

    }

    /// <summary>
    /// Retrieves the External Consultee Re-invite Model from application data and the given invite model.
    /// </summary>
    /// <param name="applicationId">The application Id</param>
    /// <param name="returnUrl">A return URL</param>
    /// <param name="inviteModel">The invite model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result containing the model</returns>
    public Task<Result<ExternalConsulteeReInviteModel>> RetrieveExternalConsulteeReInviteViewModelAsync(
        Guid applicationId, string returnUrl, ExternalConsulteeInviteModel inviteModel,
        CancellationToken cancellationToken) =>
        GetFellingLicenceDetailsAsync(applicationId, cancellationToken)
            .Map(applicationSummary => new ExternalConsulteeReInviteModel
            {
                Purpose = inviteModel.Purpose!,
                Email = inviteModel.Email,
                ConsulteeName = inviteModel.ConsulteeName,
                FellingLicenceApplicationSummary = applicationSummary,
                Id = inviteModel.Id,
                ApplicationId = applicationSummary.Id,
                ReturnUrl = returnUrl
            })
            .OnFailure(e => { _logger.LogError(e); });

    /// <summary>
    /// Retrieves the Application Summary Model from application data.
    /// </summary>
    /// <param name="applicationId">The application id</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The application summary model</returns>
    public Task<Result<FellingLicenceApplicationSummaryModel>> RetrieveApplicationSummaryAsync(Guid applicationId,
        CancellationToken cancellationToken) =>
        GetFellingLicenceDetailsAsync(applicationId, cancellationToken)
            .OnFailure(e => { _logger.LogError(e); });
    
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
    
    private static string FormatTextForDisplayInWebPage(string emailBody)
    {
        var r = new Regex(@"\<a.+?>");
        emailBody = r.Replace(emailBody, "<span style='cursor:pointer; color:blue; text-decoration:underline;' >");
        r = new Regex(@"\</a\>");
        emailBody = r.Replace(emailBody, "</span>");

        r = new Regex(@"\r?\n");
        return r.Replace(emailBody, "<br/>");
    }

    private async Task<Result<List<NotificationAttachment>?>> AddNotificationAttachments(
        List<SupportingDocument> documents,
        List<Guid> selectedDocuments,
        CancellationToken cancellationToken)
    {
        if (!documents.Any())
        {
            return Result.Success<List<NotificationAttachment>?>(null);
        }

        var attachments = new List<NotificationAttachment>();
        foreach (var supportingDocument in documents.Where(x => selectedDocuments.Contains(x.Id)))
        {
            var (_, isFailure, fileResult, error) =
                await _fileStorageService.GetFileAsync(supportingDocument.Location, cancellationToken);
            if (isFailure)
            {
                return Result.Failure<List<NotificationAttachment>?>(
                    $"Error on getting supporting documents, file name: {supportingDocument.FileName}, error: {error.ToString()}");
            }

            attachments.Add(new NotificationAttachment(supportingDocument.FileName, fileResult.FileBytes,
                supportingDocument.MimeType));
        }

        return Result.Success<List<NotificationAttachment>?>(attachments);
    }

    private static string ExtractDatabaseError(UserDbErrorReason e) =>
        e == UserDbErrorReason.NotUnique
            ? "the access link already exists"
            : "a database error";
}