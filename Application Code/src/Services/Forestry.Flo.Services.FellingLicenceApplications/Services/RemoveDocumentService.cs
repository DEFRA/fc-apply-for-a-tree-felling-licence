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
            var documentMaybe = await _fellingLicenceApplicationInternalRepository.GetDocumentByIdAsync(applicationId, documentIdentifier, cancellationToken);

            if (documentMaybe.HasNoValue)
            {
                _logger.LogWarning("Could not find a document with id of [{documentIdentifier}] in application with id [{appId}]", documentIdentifier, applicationId);
                return await HandleFileRemovalFailureAsync(
                    userAccountId,
                    applicationId,
                    documentIdentifier,
                    null,
                    "Document not found",
                    cancellationToken);
            }

            var document = documentMaybe.Value;

            if (document.Purpose is not (DocumentPurpose.Attachment or DocumentPurpose.EiaAttachment))
            {
                _logger.LogWarning("Only attachments and EIA attachments may be deleted, document id: [{documentIdentifier}, document purpose: [{purpose}]]", documentIdentifier, document.Purpose);

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
                _logger.LogWarning("Document not uploaded by {actorType}, document id: [{documentIdentifier}]", requiredActorType.GetDisplayName(), documentIdentifier);
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

            _logger.LogDebug("Call to remove file with id of [{documentIdentifier}] with location of [{location}] " +
                             "has success result of [{result}],", document.Value.Id, document.Value.Location, removeFileResult.IsSuccess);

            if (removeFileResult.IsFailure)
            {
                _logger.LogWarning(
                    "Did not receive Success result when removing file with id of [{id}], received error [{error}].",
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
                "File with id of [{documentIdentifier}] and location of [{location}] was successfully removed from storage.",
                document.Value.Id, document.Value.Location);

            var deleteResult = await _fellingLicenceApplicationInternalRepository.DeleteDocumentAsync(document.Value, cancellationToken);

            if (deleteResult.IsFailure)
            {
                _logger.LogWarning(
                    "Unable to update database to remove entry for document [{id}], received error [{error}].",
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
    }

}
