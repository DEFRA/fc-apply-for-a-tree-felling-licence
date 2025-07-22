using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication
{
    /// <summary>
    /// Handles use case for an internal user removing a supporting document
    /// from a felling licence application
    /// </summary>
    public class RemoveSupportingDocumentUseCase
    {
        private readonly RequestContext _requestContext;
        private readonly IAuditService<RemoveSupportingDocumentUseCase> _auditService;
        private readonly IRemoveDocumentService _service;
        private readonly ILogger<AddSupportingDocumentsUseCase> _logger;

        public RemoveSupportingDocumentUseCase(
            IRemoveDocumentService service,
            IAuditService<RemoveSupportingDocumentUseCase> auditService,
            RequestContext requestContext,
            ILogger<AddSupportingDocumentsUseCase> logger)
        {
            _service = Guard.Against.Null(service);
            _logger = logger;
            _requestContext = Guard.Against.Null(requestContext);
            _auditService = Guard.Against.Null(auditService);
        }

        /// <summary>
        /// Removes a supporting document using the configured <see cref="IFileStorageService"/>.
        /// </summary>
        /// <param name="user">The internal user removing the document.</param>
        /// <param name="applicationId">The application id.</param>
        /// <param name="documentId">The supporting document id.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Result"/> indicating whether the document has been removed successfully.</returns>
        public async Task<Result> RemoveSupportingDocumentsAsync(
            InternalUser user,
            Guid applicationId,
            Guid documentId,
            CancellationToken cancellationToken)
        {
            Guard.Against.Null(user);

            var internalRequest = new RemoveDocumentRequest
            {
                ApplicationId = applicationId,
                DocumentId = documentId,
                UserId = user.UserAccountId!.Value
            };

            var result = await _service.RemoveDocumentAsInternalUserAsync(
                internalRequest,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Unable to remove supporting document {docId} from application {appId} in RemoveSupportingDocumentsAsync, with error {error}", documentId, applicationId, result.Error);

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.RemoveFellingLicenceAttachmentFailureEvent, applicationId, user.UserAccountId, _requestContext,
                    new
                    {
                        ApplicationId = applicationId, 
                        Section = "Supporting Documentation",
                        user.UserAccountId,
                        result.Error
                    }), cancellationToken);

                return result;
            }

            _logger.LogDebug("Document successfully removed from application by internal user, application id {appId}, user id {userId}", applicationId, user.UserAccountId);

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.RemoveFellingLicenceAttachmentEvent, applicationId, user.UserAccountId, _requestContext,
                new
                {
                    ApplicationId = applicationId,
                    user.UserAccountId,
                    Section = "Supporting Documentation"
                }), cancellationToken);

            return Result.Success();
        }

        public async Task<Result> RemoveFellingLicenceDocument(InternalUser user, Guid applicationId, Guid documentIdentifier, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Attempting to remove Licence.");

            var result = await _service.PermanentlyRemoveDocumentAsync(applicationId,
                documentIdentifier,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Unable to permanently remove document {docId} from application {appId} in PermanentlyRemoveDocumentAsync, with error {error}", documentIdentifier, applicationId, result.Error);

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.RemoveFellingLicenceAttachmentFailureEvent, applicationId, user.UserAccountId, _requestContext,
                        new
                        {
                            ApplicationId = applicationId,
                            user.UserAccountId,
                            Section = "Supporting Documentation",
                            result.Error
                        }),
                    cancellationToken);

                return result;
            }

            _logger.LogDebug("Document successfully permanently removed from application by external user, application id {appId}, user id {userId}", applicationId, user.UserAccountId);

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.RemoveFellingLicenceAttachmentEvent, applicationId, user.UserAccountId, _requestContext,
                new
                {
                    ApplicationId = applicationId,
                    user.UserAccountId,
                    Section = "Supporting Documentation"
                }), cancellationToken);

            return Result.Success();
        }

    }
}