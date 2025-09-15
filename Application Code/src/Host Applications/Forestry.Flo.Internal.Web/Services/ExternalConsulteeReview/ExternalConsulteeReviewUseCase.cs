using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;

public class ExternalConsulteeReviewUseCase: FellingLicenceApplicationUseCaseBase
{
    private readonly IAddDocumentService _addDocumentService;
    private readonly IRemoveDocumentService _removeDocumentService;
    private readonly IGetDocumentServiceInternal _getDocumentService;
    private readonly RequestContext _requestContext;
    private readonly IExternalConsulteeReviewService _externalConsulteeReviewService;
    private readonly IAuditService<ExternalConsulteeReviewUseCase> _auditService;
    private readonly ILogger<ExternalConsulteeReviewUseCase> _logger;
    private readonly IClock _clock;

    public ExternalConsulteeReviewUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IAuditService<ExternalConsulteeReviewUseCase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IExternalConsulteeReviewService externalConsulteeReviewService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IGetDocumentServiceInternal getDocumentService,
        IAddDocumentService addDocumentService,
        IRemoveDocumentService removeDocumentService,
        ILogger<ExternalConsulteeReviewUseCase> logger,
        RequestContext requestContext,
        IClock clock) : base(
        internalUserAccountService,
        externalUserAccountService,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        agentAuthorityService,
        getConfiguredFcAreasService)
    {
        _addDocumentService = Guard.Against.Null(addDocumentService);
        _removeDocumentService = Guard.Against.Null(removeDocumentService);
        _getDocumentService = Guard.Against.Null(getDocumentService);
        _requestContext = Guard.Against.Null(requestContext);
        _externalConsulteeReviewService = Guard.Against.Null(externalConsulteeReviewService);
        _auditService = Guard.Against.Null(auditService);
        _logger = Guard.Against.Null(logger);
        _clock = Guard.Against.Null(clock);
    }

    public async Task<Result<ExternalInviteLink>> ValidateAccessCodeAsync(
        Guid applicationId,
        Guid accessCode,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var externalAccessLink = await _externalConsulteeReviewService.VerifyAccessCodeAsync(
            applicationId, accessCode, emailAddress, cancellationToken);

        if (externalAccessLink.HasNoValue)
        {
            return Result.Failure<ExternalInviteLink>("Could not locate valid external access link");
        }
        
        return Result.Success(new ExternalInviteLink
        {
            ContactEmail = externalAccessLink.Value.ContactEmail,
            ExpiresTimeStamp = externalAccessLink.Value.ExpiresTimeStamp,
            Name = externalAccessLink.Value.ContactName,
            Purpose = externalAccessLink.Value.Purpose,
            LinkType = externalAccessLink.Value.LinkType,
            SharedSupportingDocuments = externalAccessLink.Value.SharedSupportingDocuments
        });
    }


    public async Task<Result<ExternalConsulteeReviewViewModel>> GetApplicationSummaryForConsulteeReviewAsync(
        Guid applicationId,
        ExternalInviteLink externalInviteLink,
        Guid accessCode,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to retrieve information to display consultee comments page for application id {ApplicationId} and external access link id {ExternalAccessLinkId}",
            applicationId,
            externalInviteLink.Id);

        var (hasValue, fla) = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);
        
        if (!hasValue)
        {
            _logger.LogError("Could not locate Felling Licence Application with the given id {id}", applicationId);
            return Result.Failure<ExternalConsulteeReviewViewModel>($"Could not locate Felling Licence Application with the given id {applicationId}");
        }

        var (_, isFailure, flaModel, error) = await ExtractApplicationSummaryAsync(fla, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Could not load application summary model: {Error}", error);
            return Result.Failure<ExternalConsulteeReviewViewModel>($"Could not load application summary model: {error}");
        }

        var comments = await _externalConsulteeReviewService.RetrieveConsulteeCommentsForAccessCodeAsync(
            applicationId,
            accessCode, 
            cancellationToken);

        var items = comments
            .OrderByDescending(x => x.CreatedTimestamp)
            .Select(x => new ActivityFeedItemModel
        {
            ActivityFeedItemType = ActivityFeedItemType.ConsulteeComment,
            CreatedTimestamp = x.CreatedTimestamp,
            FellingLicenceApplicationId = x.FellingLicenceApplicationId,
            VisibleToConsultee = true,
            Text = x.Comment,
            Source = $"{x.AuthorName} ({x.AuthorContactEmail})",
            Attachments = GetAttachments(x.ConsulteeAttachmentIds.ToList(), fla.Documents)
        }).ToList();

        var feed = new ActivityFeedModel
        {
            ActivityFeedItemModels = items,
            ApplicationId = applicationId,
            ShowAddCaseNote = false,
            ActivityFeedTitle = "Your added comments",
            ShowFilters = false,
            ViewingUserActorType = ActorType.ExternalConsultee,
            ViewingUserAuthenticationToken = accessCode,
            ViewingUserEmail = externalInviteLink.ContactEmail
        };

        var result = new ExternalConsulteeReviewViewModel
        {
            ApplicationSummary = flaModel,
            ConsulteeDocuments = ModelMapping.ToDocumentModelList(fla.Documents
                    .Where(x => x.VisibleToConsultee 
                                && x.DeletionTimestamp.HasNoValue()
                                && externalInviteLink.SharedSupportingDocuments.Any(s => s == x.Id))
                    .OrderByDescending(x => x.CreatedTimestamp)
                    .ToList())
                    .ToList(),
            AddConsulteeComment = new AddConsulteeCommentModel
            {
                ApplicationId = applicationId,
                AuthorContactEmail = externalInviteLink.ContactEmail,
                AuthorName = externalInviteLink.Name,
                LinkExpiryDateTime = externalInviteLink.ExpiresTimeStamp,
                AccessCode = accessCode
            },
            ActivityFeed = feed
        };

        return Result.Success(result);
    }

    public async Task<Result> AddConsulteeCommentAsync(
        AddConsulteeCommentModel model,
        FormFileCollection consulteeAttachmentFiles,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(model);
        
        _logger.LogDebug("Attempting to store new consultee comment for application with id {ApplicationId}", model.ApplicationId);

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var attachmentIds = new List<Guid>();
        if (consulteeAttachmentFiles?.Any() ?? false)
        {
            var filesModel = ModelMapping.ToFileToStoreModel(consulteeAttachmentFiles);

            var addDocumentsRequest = new AddDocumentsRequest
            {
                ActorType = ActorType.ExternalConsultee,
                VisibleToConsultee = true,
                VisibleToApplicant = true,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.ConsultationAttachment,
                FellingApplicationId = model.ApplicationId,
                FileToStoreModels = filesModel,
                ReceivedByApi = false
            };

            var addDocsResult = await _addDocumentService.AddDocumentsAsInternalUserAsync(
                addDocumentsRequest, cancellationToken);

            if (addDocsResult.IsFailure)
            {
                _logger.LogError("Attempt to store consultee attachments failed: {Error}", addDocsResult.Error);
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.AddConsulteeCommentFailure,
                    model.ApplicationId,
                    null,
                    _requestContext,
                    new
                    {
                        Error = "Failed to attach consultee documents: " + string.Join(", ", addDocsResult.Error.UserFacingFailureMessages),
                        model.AuthorName,
                        model.AuthorContactEmail
                    }), cancellationToken);
                return Result.Failure("Failed to store consultee attachments");
            }

            attachmentIds = addDocsResult.Value.DocumentIds.ToList();
        }
        
        var consulteeCommentModel = new ConsulteeCommentModel
        {
            FellingLicenceApplicationId = model.ApplicationId,
            AuthorContactEmail = model.AuthorContactEmail,
            CreatedTimestamp = now,
            AuthorName = model.AuthorName,
            Comment = model.Comment,
            ConsulteeAttachmentIds = attachmentIds,
            AccessCode = model.AccessCode
        };
        var addCommentResult = await _externalConsulteeReviewService.AddCommentAsync(
            consulteeCommentModel, cancellationToken);

        if (addCommentResult.IsFailure)
        {
            _logger.LogError("Attempt to store new consultee comment failed");

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.AddConsulteeCommentFailure,
                model.ApplicationId,
                null,
                _requestContext,
                new
                {
                    Error = "Failed to save consultee comment: " + addCommentResult.Error,
                    model.AuthorName,
                    model.AuthorContactEmail
                }), cancellationToken);

            foreach (var attachmentId in attachmentIds)
            {
                var cleanupResult = await _removeDocumentService.PermanentlyRemoveDocumentAsync(model.ApplicationId, attachmentId, cancellationToken);
                if (cleanupResult.IsFailure)
                {
                    _logger.LogError("Failed to clean up consultee attachment with id {AttachmentId} after failed comment save: {Error}",
                        attachmentId, cleanupResult.Error);
                }
            }

            return Result.Failure(addCommentResult.Error);
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AddConsulteeComment,
            model.ApplicationId,
            null,
            _requestContext,
            new
            {
                model.AuthorName,
                model.AuthorContactEmail
            }), cancellationToken);

        return Result.Success();
    }

    public async Task<Result<FileContentResult>> GetSupportingDocumentAsync(
        Guid applicationId,
        Guid accessCode,
        Guid documentIdentifier,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve document from storage, for consultee access code [{accessCode}], application having Id of [{applicationId}] for document with Id [{documentId}].",
            accessCode, applicationId, documentIdentifier);

        var accessValid = await ValidateAccessCodeAsync(applicationId, accessCode, emailAddress, cancellationToken);
        if (accessValid.IsFailure)
        {
            _logger.LogWarning("Access code validation failed for access code [{accessCode}], application id [{applicationId}], email address [{emailAddress}]. Error: {Error}",
                accessCode, applicationId, emailAddress, accessValid.Error);
            return Result.Failure<FileContentResult>(accessValid.Error);
        }

        var request = new GetDocumentRequest
        {
            DocumentId = documentIdentifier,
            ApplicationId = applicationId
        };
        var (isSuccess, _, fileToStoreModel, error) = await _getDocumentService.GetDocumentAsync(
            request, cancellationToken);

        if (isSuccess)
        {
            _logger.LogDebug("Document retrieved from storage with id of [{documentId}], type is [{documentType}] with original name of [{documentName}].",
                documentIdentifier, fileToStoreModel.ContentType, fileToStoreModel.FileName);

            var fileContentResult = new FileContentResult(fileToStoreModel.FileBytes, fileToStoreModel.ContentType)
            {
                FileDownloadName = fileToStoreModel.FileName
            };

            return Result.Success(fileContentResult);
        }

        _logger.LogWarning("Document having identifier of [{documentId}] could not be retrieved from storage, error is :[{error}].", 
            documentIdentifier, error);

        return Result.Failure<FileContentResult>("File could not be retrieved");
    }
    
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