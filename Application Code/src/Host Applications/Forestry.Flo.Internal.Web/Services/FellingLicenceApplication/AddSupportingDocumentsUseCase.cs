using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication
{
    /// <summary>
    /// Handles the use case for an internal user uploading one or more supporting documents as part
    /// of a felling licence application.
    /// </summary>
    public class AddSupportingDocumentsUseCase : IAddSupportingDocumentsUseCase
    {
        private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationRepository;
        private readonly IAddDocumentService _service;
        private readonly ILogger<AddSupportingDocumentsUseCase> _logger;

        public AddSupportingDocumentsUseCase(
            IAddDocumentService service,
            IAuditService<AddSupportingDocumentsUseCase> auditService,
            IFellingLicenceApplicationInternalRepository fellingLicenceApplicationRepository,
            RequestContext requestContext,
            ILogger<AddSupportingDocumentsUseCase> logger)
        {
            _service = Guard.Against.Null(service);
            Guard.Against.Null(auditService);
            _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
            Guard.Against.Null(requestContext);
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Result> AddDocumentsToApplicationAsync(
            InternalUser user,
            Guid applicationId,
            FormFileCollection supportingDocumentationFiles,
            ModelStateDictionary modelState,
            bool visibleToApplicant,
            bool visibleToConsultees,
            CancellationToken cancellationToken)
        {
            if (!supportingDocumentationFiles.Any())
            {
                return Result.Success();
            }

            var applicationResult = 
                await _fellingLicenceApplicationRepository.GetAsync(
                    applicationId,
                    cancellationToken);

            if (!applicationResult.HasValue)
            {
                _logger.LogError("Application not found in AddDocumentsToApplicationAsync, id {id}", applicationId);
                return Result.Failure($"Application not found in AddDocumentsToApplicationAsync, id {applicationId}.");
            }

            var filesModel = ModelMapping.ToFileToStoreModel(supportingDocumentationFiles);

            var addDocumentRequest = new AddDocumentsRequest
            {
                ActorType = ActorType.InternalUser,
                ApplicationDocumentCount = applicationResult.Value.Documents!.Count(x => x.DeletionTimestamp is null),
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = applicationId,
                FileToStoreModels = filesModel,
                ReceivedByApi = false,
                UserAccountId = user.UserAccountId,
                VisibleToApplicant = visibleToApplicant,
                VisibleToConsultee = visibleToConsultees
            };

            var result = await _service.AddDocumentsAsInternalUserAsync(
                addDocumentRequest,
                cancellationToken);

            if (result.IsSuccess)
            {
                AddErrorsToModelState(modelState, result.Value.UserFacingFailureMessages);
                return Result.Success();
            }

            AddErrorsToModelState(modelState, result.Error.UserFacingFailureMessages);
            _logger.LogError("Unable to add supporting documents to application {id} in AddDocumentsToApplicationAsync", applicationId);
            return Result.Failure(result.Error.ToString());
        }

        private static void AddErrorsToModelState(ModelStateDictionary modelState, IEnumerable<string> userFacingFailureMessage)
        {
            foreach (var message in userFacingFailureMessage)
            {
                modelState.AddModelError("supporting-documentation-files", message);
            }
        }
    }
}