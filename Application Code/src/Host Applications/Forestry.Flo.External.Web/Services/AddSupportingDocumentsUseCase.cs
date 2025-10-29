using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.External.Web.Services
{
    /// <summary>
    /// Handles the use case for a user uploading one or more supporting documents as part
    /// of a felling licence application.
    /// </summary>
    public class AddSupportingDocumentsUseCase : ApplicationUseCaseCommon
    {
        private readonly IAuditService<AddSupportingDocumentsUseCase> _auditService;
        private readonly IAddDocumentService _service;
        private readonly RequestContext _requestContext;
        private readonly ILogger<AddSupportingDocumentsUseCase> _logger;
        
        public AddSupportingDocumentsUseCase(
            IAddDocumentService service,
            IAuditService<AddSupportingDocumentsUseCase> auditService,
            IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers,
            IRetrieveUserAccountsService retrieveUserAccountsServiceForExternalUsers,
            IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
            IGetPropertyProfiles getPropertyProfilesService,
            IGetCompartments getCompartmentsService,
            IAgentAuthorityService agentAuthorityService,
            RequestContext requestContext,
            ILogger<AddSupportingDocumentsUseCase> logger) : 
        base(
            retrieveUserAccountsServiceForExternalUsers, 
            retrieveWoodlandOwnersService, 
            getFellingLicenceApplicationServiceForExternalUsers, 
            getPropertyProfilesService, 
            getCompartmentsService,
            agentAuthorityService,
            logger)
        {
            _service = Guard.Against.Null(service);
            _auditService = Guard.Against.Null(auditService);
            _requestContext = Guard.Against.Null(requestContext);
            _logger = logger;
        }

        /// <summary>
        /// Saves each uploaded file in <see cref="FormFileCollection"/> using the configured <see cref="IFileStorageService"/>, firstly validating the file with <see cref="FileValidator"/>.
        /// </summary>
        /// <remarks>
        /// Adds a <see cref="ModelError"/> for each <see cref="FormFile"/> that could not be saved either due to validation failure or error.
        /// </remarks>
        /// <param name="user"></param>
        /// <param name="addSupportingDocumentModel"></param>
        /// <param name="supportingDocumentationFiles"></param>
        /// <param name="modelState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result> AddDocumentsToApplicationAsync(
            ExternalApplicant user,
            AddSupportingDocumentModel addSupportingDocumentModel,
            FormFileCollection supportingDocumentationFiles,
            ModelStateDictionary modelState,
            CancellationToken cancellationToken)
        {
            if (!supportingDocumentationFiles.Any()) return Result.Success();
            
            await base.EnsureApplicationIsEditable(addSupportingDocumentModel.FellingLicenceApplicationId, user, cancellationToken)
                .ConfigureAwait(false);

            var applicationResult = await 
                UserCanProcessUseCaseAsync(user, addSupportingDocumentModel.FellingLicenceApplicationId, cancellationToken)
                    .ConfigureAwait(false);

            if (!applicationResult.HasValue) 
                return Result.Failure("Application not found or user cannot access it.");

            var filesModel = ModelMapping.ToFileToStoreModel(supportingDocumentationFiles);

            var addDocumentRequest = new AddDocumentsExternalRequest
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = applicationResult.Value.Documents!.Count(x => x.DeletionTimestamp.HasNoValue() && x.Purpose == DocumentPurpose.Attachment),
                DocumentPurpose = addSupportingDocumentModel.Purpose,
                FellingApplicationId = addSupportingDocumentModel.FellingLicenceApplicationId,
                FileToStoreModels = filesModel,
                ReceivedByApi = false,
                UserAccountId = user.UserAccountId,
                VisibleToApplicant = true,
                VisibleToConsultee = addSupportingDocumentModel.AvailableToConsultees,
                WoodlandOwnerId = applicationResult.Value.WoodlandOwnerId
            };

            var result = await _service.AddDocumentsAsExternalApplicantAsync(addDocumentRequest, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                AddErrorsToModelState(modelState, result.Value.UserFacingFailureMessages);
                return Result.Success();
            }

            AddErrorsToModelState(modelState, result.Error.UserFacingFailureMessages);
            return Result.Failure("f");
        }
        
        private async Task<Maybe<FellingLicenceApplication>> UserCanProcessUseCaseAsync(
            ExternalApplicant user,
            Guid applicationId,
            CancellationToken cancellationToken)
        {
            var application = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);
            if (application.IsSuccess)
            {
                return Maybe<FellingLicenceApplication>.From(application.Value);
            }

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.AddFellingLicenceAttachmentFailureEvent, null, user.UserAccountId, _requestContext,
                new { user.WoodlandOwnerId, Section = "Supporting Documentation", Reason = "Unauthorized" }), cancellationToken);


            _logger.LogError(
                "User is not Woodland Owner and cannot save an attachment, user id: {UserAccountId}",
                user.UserAccountId);
                
            return Maybe<FellingLicenceApplication>.None;
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