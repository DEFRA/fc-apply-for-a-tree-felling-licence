using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;

namespace Forestry.Flo.External.Web.Services
{
    /// <summary>
    /// Handles use case for a user removing a supporting document
    /// from a felling licence application
    /// </summary>
    public class RemoveSupportingDocumentUseCase : ApplicationUseCaseCommon
    {
        private readonly RequestContext _requestContext;
        private readonly IAuditService<RemoveSupportingDocumentUseCase> _auditService;
        private readonly IRemoveDocumentService _removeDocumentService;
        private readonly ILogger<RemoveSupportingDocumentUseCase> _logger;

        public RemoveSupportingDocumentUseCase(
            IRemoveDocumentService removeDocumentService,
            IRetrieveUserAccountsService retrieveUserAccountsService,
            IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
            IAuditService<RemoveSupportingDocumentUseCase> auditService,
            IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationService,
            IGetPropertyProfiles getPropertyProfilesService,
            IGetCompartments getCompartmentsService,
            IAgentAuthorityService agentAuthorityService,
            ILogger<RemoveSupportingDocumentUseCase> logger,
            RequestContext requestContext) : 
        base(
            retrieveUserAccountsService,
            retrieveWoodlandOwnersService, 
            getFellingLicenceApplicationService, 
            getPropertyProfilesService, 
            getCompartmentsService, 
            agentAuthorityService,
            logger)
        {
            _requestContext = requestContext;
            _removeDocumentService = Guard.Against.Null(removeDocumentService);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        /// <summary>
        /// Soft deletes a supporting document from a felling licence application, ensuring the user has access to the application and
        /// that the application is in an editable state. Audits the action and any failures that occur.
        /// </summary>
        /// <param name="user">The user requesting the document removal.</param>
        /// <param name="applicationId">The id of the application to remove the document from.</param>
        /// <param name="documentIdentifier">The id of the document to remove.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Result"/> indicating the outcome.</returns>
        public async Task<Result> RemoveSupportingDocumentAsync(
            ExternalApplicant user,
            Guid applicationId, 
            Guid documentIdentifier, 
            CancellationToken cancellationToken)
        {
            var isApplicationEditable = await base.EnsureApplicationIsEditable(applicationId, user, cancellationToken)
                .ConfigureAwait(false);

            if (isApplicationEditable.IsFailure)
            {
                _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                    applicationId,
                    isApplicationEditable.Error);

                return isApplicationEditable;
            }

            var userAccessModel = await GetUserAccessModelAsync(user, cancellationToken);

            var externalRequest = new RemoveDocumentExternalRequest
            {
                ApplicationId = applicationId,
                DocumentId = documentIdentifier,
                UserId = user.UserAccountId!.Value,
                UserAccessModel = userAccessModel.Value
            };

            var result = await _removeDocumentService.RemoveDocumentAsExternalApplicantAsync(
                externalRequest,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Unable to remove supporting document {docId} from application {appId} in RemoveSupportingDocumentsAsync, with error {error}", documentIdentifier, applicationId, result.Error);

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.RemoveFellingLicenceAttachmentFailureEvent, applicationId, user.UserAccountId, _requestContext,
                    new { 
                        ApplicationId = applicationId,
                        user.WoodlandOwnerId, 
                        Section = "Supporting Documentation", 
                        Reason = "Unauthorized", 
                        result.Error }), 
                    cancellationToken);

                return result;
            }

            _logger.LogDebug("Document successfully removed from application by external user, application id {appId}, user id {userId}", applicationId, user.UserAccountId);

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

        /// <summary>
        /// Permanently removes a supporting document from a felling licence application, ensuring the user has access to the application and
        /// that the application is in an editable state. Audits the action and any failures that occur.
        /// </summary>
        /// <param name="user">The user requesting the document removal.</param>
        /// <param name="applicationId">The id of the application to remove the document from.</param>
        /// <param name="documentIdentifier">The id of the document to remove.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Result"/> indicating the outcome.</returns>
        public async Task<Result> RemoveFellingLicenceDocumentAsync(ExternalApplicant user, Guid applicationId, Guid documentIdentifier, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Attempting to remove Licence.");

            var result = await _removeDocumentService.PermanentlyRemoveDocumentAsync(applicationId,
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

        /// <summary>
        /// Permanently removes a supporting document from a felling licence application without a user context, ensuring that the
        /// application is in an editable state. Audits the action and any failures that occur.
        /// </summary>
        /// <param name="applicationId">The id of the application to remove the document from.</param>
        /// <param name="documentIdentifier">The id of the document to remove.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="Result"/> indicating the outcome.</returns>
        public async Task<Result> RemoveSupportingDocumentBySystemAsync(Guid applicationId, Guid documentIdentifier, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Attempting to remove supporting document {DocumentId} for application {ApplicationId}", documentIdentifier, applicationId);

            var isEditable = await GetFellingLicenceApplicationServiceForExternalUsers.GetIsEditable(
                applicationId,
                UserAccessModel.SystemUserAccessModel,
                cancellationToken);

            if (isEditable.IsFailure || !isEditable.Value)
            {
                // Backend protection against updating submitted FLAs in case UI protections fail.
                return Result.Failure($"Attempt to update a Felling Licence Application with a non editable status is not allowed. ID: {applicationId}");
            }

            var result = await _removeDocumentService.PermanentlyRemoveDocumentAsync(
                applicationId,
                documentIdentifier,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Unable to permanently remove document {DocumentId} from application {ApplicationId}, error: {Error}", documentIdentifier, applicationId, result.Error);

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.RemoveFellingLicenceAttachmentFailureEvent, applicationId, null, _requestContext,
                        new
                        {
                            ApplicationId = applicationId,
                            Section = "Supporting Documentation",
                            DocumentId = documentIdentifier,
                            result.Error
                        }),
                    cancellationToken);

                return result;
            }

            _logger.LogDebug("Document successfully permanently removed from application by system user, application {ApplicationId}, document {DocumentId}", applicationId, documentIdentifier);

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.RemoveFellingLicenceAttachmentEvent, applicationId, null, _requestContext,
                new
                {
                    ApplicationId = applicationId,
                    Section = "Supporting Documentation",
                    DocumentId = documentIdentifier
                }), cancellationToken);

            return Result.Success();
        }
    }
}
