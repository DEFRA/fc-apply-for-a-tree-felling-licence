using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using NodaTime;
using Document = Forestry.Flo.Services.FellingLicenceApplications.Entities.Document;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services
{
    /// <summary>
    /// Service class for removing a document from a given <see cref="FellingLicenceApplication"/>. 
    /// </summary>
    public class RemoveDocumentService : IRemoveDocumentService
    {
        private readonly IAuditService<RemoveDocumentService> _auditService;
        private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository;
        private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
        private readonly IFileStorageService _storageService;
        private readonly IClock _clock;
        private readonly RequestContext _requestContext;
        private readonly ILogger<RemoveDocumentService> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="RemoveDocumentService"/>.
        /// </summary>
        /// <param name="storageService">The configured <see cref="IFileStorageService"/> to be used.</param>
        /// <param name="fellingLicenceApplicationRepository"></param>
        /// <param name="fellingLicenceApplicationInternalRepository"></param>
        /// <param name="auditService"></param>
        /// <param name="requestContext"></param>
        /// <param name="clock"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public RemoveDocumentService(
            IFileStorageService storageService,
            IFellingLicenceApplicationExternalRepository fellingLicenceApplicationRepository,
            IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
            IAuditService<RemoveDocumentService> auditService,
            RequestContext requestContext,
            IClock clock,
            ILogger<RemoveDocumentService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _requestContext = Guard.Against.Null(requestContext);
            _auditService = Guard.Against.Null(auditService);
            _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
            _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
            _clock = Guard.Against.Null(clock);
            _logger = Guard.Against.Null(logger);
        }

        /// <inheritdoc/>
        public async Task<Result> RemoveDocumentAsExternalApplicantAsync(
            RemoveDocumentExternalRequest removeDocumentExternalRequest,
            CancellationToken cancellationToken)
        {
            return await RetrieveDocumentAndRemoveAsync(
                removeDocumentExternalRequest.ApplicationId,
                removeDocumentExternalRequest.DocumentId,
                removeDocumentExternalRequest.UserId,
                ActorType.ExternalApplicant,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result> RemoveDocumentAsInternalUserAsync(
            RemoveDocumentRequest removeDocumentRequest,
            CancellationToken cancellationToken)
        {
            return await RetrieveDocumentAndRemoveAsync(
                removeDocumentRequest.ApplicationId,
                removeDocumentRequest.DocumentId,
                removeDocumentRequest.UserId,
                ActorType.InternalUser,
                cancellationToken);
        }

        private async Task<Result> RetrieveDocumentAndRemoveAsync(
            Guid applicationId,
            Guid documentIdentifier,
            Guid userAccountId,
            ActorType requiredActorType,
            CancellationToken cancellationToken)
        {
            var documentMaybe = await _fellingLicenceApplicationRepository.GetDocumentByIdAsync(applicationId, documentIdentifier, cancellationToken);

            if (documentMaybe.HasNoValue)
            {
                _logger.LogWarning("Could not find a document with id of [{DocumentIdentifier}] in application with id [{ApplicationId}]", documentIdentifier, applicationId);
                // deletes are idempotent, calling delete again for the same id should be successful no-op
                return Result.Success();
            }

            var document = documentMaybe.Value;

            if (document.Purpose is not (DocumentPurpose.Attachment or DocumentPurpose.EiaAttachment or DocumentPurpose.WmpDocument or DocumentPurpose.TreeHealthAttachment))
            {
                _logger.LogWarning("Only attachments, EIA attachments or WMP documents may be deleted, document id: [{DocumentIdentifier}], document purpose: [{Purpose}]", documentIdentifier, document.Purpose);

                return await HandleFileRemovalFailureAsync(
                    userAccountId,
                    applicationId,
                    documentIdentifier,
                    document.Purpose,
                    "Only attachments and EIA attachments may be deleted",
                    cancellationToken);
            }

            // only allow actors remove documents uploaded by the same actor type.

            if (document.AttachedByType != requiredActorType)
            {
                _logger.LogWarning("Document not uploaded by {ActorType}, document id: [{DocumentIdentifier}]", requiredActorType.GetDisplayName(), documentIdentifier);
                return await HandleFileRemovalFailureAsync(
                    userAccountId,
                    applicationId,
                    documentIdentifier,
                    document.Purpose,
                    $"Document not uploaded by {requiredActorType.GetDisplayName()}",
                    cancellationToken);
            }

            // update the document entry to contain deletion data

            document.DeletedByUserId = userAccountId;
            document.DeletionTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc();

            // if we removed a WMP document, check if we need to reset the step status to in progress (no WMP documents left on application)
            if (document.Purpose is DocumentPurpose.WmpDocument)
            {
                await CheckWmpDocumentStepStatusAsync(applicationId, documentIdentifier, cancellationToken);
            }

            var saveResult = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (saveResult.IsFailure)
            {
                return await HandleFileRemovalFailureAsync(
                    userAccountId,
                    applicationId,
                    document.Id,
                    document.Purpose,
                    $"Unable to update deletion data in document table for application",
                    cancellationToken);
            }

            await HandleFileRemovedSuccessfullyAsync(
                userAccountId,
                applicationId,
                document,
                cancellationToken);

            return Result.Success();
        }
        
        /// <inheritdoc/>
        public async Task<Result> PermanentlyRemoveDocumentAsync(
            Guid applicationId,
            Guid documentId,
            CancellationToken cancellationToken)
        {
            // documents cannot be permanently deleted by users, this method will instead be called automatically
            // after a document has been soft deleted for a set period of time

            var document = await _fellingLicenceApplicationInternalRepository.GetDocumentByIdAsync(applicationId, documentId, cancellationToken);
            if (document.HasNoValue)
            {
                _logger.LogError("Could not retrieve document with id {DocumentId} for application with id {ApplicationId} to delete", documentId, applicationId);
                await HandleFileRemovalFailureAsync(
                    null, applicationId, documentId, null, "Could not retrieve document to delete", cancellationToken);
                return Result.Failure("Could not retrieve document");
            }

            var removeFileResult = await _storageService.RemoveFileAsync(document.Value.Location!, cancellationToken);

            _logger.LogDebug("Call to remove file with id of [{DocumentIdentifier}] with location of [{Location}] " +
                             "has success result of [{Result}],", document.Value.Id, document.Value.Location, removeFileResult.IsSuccess);

            if (removeFileResult.IsFailure)
            {
                _logger.LogWarning(
                    "Did not receive Success result when removing file with id of [{Id}], received error [{Error}].",
                    document.Value.Id, removeFileResult.Error);

                await HandleFileRemovalFailureAsync(
                    null,
                    applicationId,
                    documentId,
                    document.Value.Purpose,
                    removeFileResult.Error.ToString(),
                    cancellationToken);

                return Result.Failure("Could not remove document from file storage");
            }
            
            _logger.LogDebug(
                "File with id of [{DocumentIdentifier}] and location of [{Location}] was successfully removed from storage.",
                document.Value.Id, document.Value.Location);

            var deleteResult = await _fellingLicenceApplicationInternalRepository.DeleteDocumentAsync(document.Value, cancellationToken);

            if (deleteResult.IsFailure)
            {
                _logger.LogWarning(
                    "Unable to update database to remove entry for document [{Id}], received error [{Error}].",
                    documentId, deleteResult.Error);

                await HandleFileRemovalFailureAsync(
                    null,
                    applicationId,
                    documentId,
                    document.Value.Purpose,
                    deleteResult.Error.ToString(),
                    cancellationToken);

                return Result.Failure(deleteResult.Error.ToString());
            }

            await HandleFileRemovedSuccessfullyAsync(
                null,
                applicationId,
                document.Value,
                cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Hides a document from the applicant's view without removing it.
        /// </summary>
        /// <param name="applicationId">The ID of the application containing the document.</param>
        /// <param name="documentId">The ID of the document to hide.</param>
        /// <param name="userAccountId">The ID of the user performing the action.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A Result indicating success or failure.</returns>
        public async Task<Result> HideDocumentFromApplicantAsync(
            Guid applicationId,
            Guid documentId,
            Guid userAccountId,
            CancellationToken cancellationToken)
        {
            var documentMaybe = await _fellingLicenceApplicationRepository.GetDocumentByIdAsync(applicationId, documentId, cancellationToken);

            if (documentMaybe.HasNoValue)
            {
                _logger.LogWarning("Could not find a document with id of [{DocumentIdentifier}] in application with id [{ApplicationId}]", documentId, applicationId);
                return Result.Failure("Document not found");
            }

            var document = documentMaybe.Value;

            var result = await _fellingLicenceApplicationRepository.UpdateDocumentVisibleToApplicantAsync(
                applicationId,
                documentId,
                false,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Unable to update document visibility for document [{Id}], received error [{Error}].",
                    documentId, result.Error);

                return Result.Failure("Could not update document visibility");
            }

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.HideFellingLicenceDocumentEvent,
                applicationId,
                userAccountId,
                _requestContext,
                new
                {
                    documentId = document.Id,
                    document.Purpose,
                    document.FileName,
                    document.Location
                }
            ), cancellationToken);

            return Result.Success();
        }

        private async Task HandleFileRemovedSuccessfullyAsync(
            Guid? userAccountId,
            Guid applicationId, 
            Document document,
            CancellationToken cancellationToken)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.RemoveFellingLicenceAttachmentEvent,
                applicationId,
                userAccountId,
                _requestContext,
                new
                {
                    documentId = document.Id,
                    document.Purpose,
                    document.FileName,
                    document.Location
                }
            ), cancellationToken);
        }

        private async Task<Result> HandleFileRemovalFailureAsync(
            Guid? userAccountId,
            Guid applicationId, 
            Guid documentId,
            DocumentPurpose? purpose,
            string reason,
            CancellationToken cancellationToken)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.RemoveFellingLicenceAttachmentFailureEvent,
                applicationId,
                userAccountId,
                _requestContext,
                new
                {
                    purpose,
                    documentId,
                    FailureReason = reason
                }
            ), cancellationToken);

            return Result.Failure(reason);
        }

        private async Task<Result> CheckWmpDocumentStepStatusAsync(
            Guid applicationId,
            Guid documentId,
            CancellationToken cancellationToken)
        {
            var application =
                await _fellingLicenceApplicationRepository.GetAsync(applicationId, cancellationToken);

            if (application.HasNoValue)
            {
                _logger.LogError("Failed to retrieve application with id {ApplicationId}", applicationId);
                return Result.Failure("Failed to retrieve application");
            }

            var wmpDocumentsCount = application.Value.Documents
                .Count(d => d is { Purpose: DocumentPurpose.WmpDocument, DeletionTimestamp: null } && d.Id != documentId);

            if (wmpDocumentsCount < 1
                && application.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus is true)
            {
                application.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = false;
            }

            return Result.Success();
        }
    }

}
