using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using System.Net;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Service class for adding one or more documents to the system for a given <see cref="FellingLicenceApplication"/>. 
/// </summary>
public class AddDocumentService : IAddDocumentService
{
    private readonly IAuditService<AddDocumentService> _auditService;
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository;
    private readonly IFileStorageService _storageService;
    private readonly FileValidator _fileValidator;
    private readonly RequestContext _requestContext;
    private readonly UserFileUploadOptions _userFileUploadOptions;
    private readonly FileTypesProvider _fileTypesProvider;
    private readonly ILogger<AddDocumentService> _logger;
    private readonly IClock _clock;

    /// <summary>
    /// Creates a new instance of <see cref="AddDocumentService"/>.
    /// </summary>
    /// <param name="clock"></param>
    /// <param name="storageService">The configured <see cref="IFileStorageService"/> to be used.</param>
    /// <param name="fileTypesProvider">A <see cref="FileTypesProvider"/> to calculate the file type.</param>
    /// <param name="userFileUploadOptions">Configuration settings for user file uploads.</param>
    /// <param name="auditService">An auditing service.</param>
    /// <param name="fellingLicenceApplicationRepository">A repository implementation to interact with the underlying database.</param>
    /// <param name="fileValidator">A validator for uploaded files.</param>
    /// <param name="requestContext">The request context.</param>
    /// <param name="logger">A logging implementation.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddDocumentService(IClock clock,
        IFileStorageService storageService,
        FileTypesProvider fileTypesProvider,
        IOptions<UserFileUploadOptions> userFileUploadOptions,
        IAuditService<AddDocumentService> auditService,
        IFellingLicenceApplicationExternalRepository fellingLicenceApplicationRepository,
        FileValidator fileValidator,
        RequestContext requestContext,
        ILogger<AddDocumentService> logger)
    {
        _clock = Guard.Against.Null(clock);
        _storageService = Guard.Against.Null(storageService);
        _fileValidator = Guard.Against.Null(fileValidator);
        _requestContext = Guard.Against.Null(requestContext);
        _fileTypesProvider = Guard.Against.Null(fileTypesProvider);
        _userFileUploadOptions = Guard.Against.Null(userFileUploadOptions.Value);
        _auditService = Guard.Against.Null(auditService);
        _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AddDocumentsSuccessResult, AddDocumentsFailureResult>> AddDocumentsAsExternalApplicantAsync(
        AddDocumentsExternalRequest addDocumentsRequest,
        CancellationToken cancellationToken)
    {
        var userPermission = await _fellingLicenceApplicationRepository.VerifyWoodlandOwnerIdForApplicationAsync(
            addDocumentsRequest.WoodlandOwnerId,
            addDocumentsRequest.FellingApplicationId,
            cancellationToken);

        if (!userPermission)
        {
            _logger.LogWarning("External applicant with woodland owner id [{WoodlandOwnerId}] " +
                               "lacks permission to add documents to application having id of [{ApplicationId}].",
                addDocumentsRequest.WoodlandOwnerId, addDocumentsRequest.FellingApplicationId);
            var message = $"External applicant lacks permission to add documents to application, app id: {addDocumentsRequest.FellingApplicationId}, woodland owner id: {addDocumentsRequest.WoodlandOwnerId}.";
            await RaiseFailureAuditEventAsync(
                addDocumentsRequest.UserAccountId,
                addDocumentsRequest.FellingApplicationId,
                null,
                message,
                cancellationToken);
            return CreateFailureResult(new List<string>{message});
        }

        return await AddDocumentsAsync(addDocumentsRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<AddDocumentsSuccessResult, AddDocumentsFailureResult>> AddDocumentsAsInternalUserAsync(
        AddDocumentsRequest addDocumentsRequest,
        CancellationToken cancellationToken)
    {
        return await AddDocumentsAsync(addDocumentsRequest, cancellationToken);
    }

    private async Task<Result<AddDocumentsSuccessResult, AddDocumentsFailureResult>> AddDocumentsAsync(
        AddDocumentsRequest addDocumentsRequest,
        CancellationToken cancellationToken)
    {
        var userFacingErrors = new List<string>();

        var applicationExists =
            await _fellingLicenceApplicationRepository.CheckApplicationExists(
                addDocumentsRequest.FellingApplicationId, cancellationToken);

        if (!applicationExists)
        {
            var message = $"An application cannot be retrieved with an id of {addDocumentsRequest.FellingApplicationId}.";
            userFacingErrors.Add(message);
            _logger.LogError("Could not retrieve application with id {ApplicationId}", addDocumentsRequest.FellingApplicationId);
            await RaiseFailureAuditEventAsync(addDocumentsRequest.UserAccountId, addDocumentsRequest.FellingApplicationId, null, message, cancellationToken);
            return CreateFailureResult(userFacingErrors);
        }
            
        if (addDocumentsRequest is 
            { ReceivedByApi: true, DocumentPurpose: not DocumentPurpose.FcLisConstraintReport and not DocumentPurpose.ExternalLisConstraintReport })
        {
            var message = $"Attempt by Api to add document with incorrect purpose type {addDocumentsRequest.DocumentPurpose}.";
            userFacingErrors.Add(message);
            _logger.LogError("Attempt by Api to add document with incorrect purpose type {DocumentPurpose}.", addDocumentsRequest.DocumentPurpose);
            await RaiseFailureAuditEventAsync(addDocumentsRequest.UserAccountId, addDocumentsRequest.FellingApplicationId, null, message, cancellationToken);
            return CreateFailureResult(userFacingErrors);
        }

        var documentStorageFailure = false;
        var documentIds = new List<Guid>();

        var fileUploadReason = addDocumentsRequest.DocumentPurpose switch
        {
            DocumentPurpose.EiaAttachment => FileUploadReason.EiaDocument,
            DocumentPurpose.WmpDocument => FileUploadReason.WmpDocument,
            _ => FileUploadReason.SupportingDocument
        };

        if (addDocumentsRequest.FileToStoreModels.Any())
        {
            var exceedsMaximumDocumentsPerUser =
                addDocumentsRequest.ApplicationDocumentCount + addDocumentsRequest.FileToStoreModels.Count > _userFileUploadOptions.MaxNumberDocuments
                && addDocumentsRequest.DocumentPurpose != DocumentPurpose.WmpDocument;  // only EIA/supporting docs are limited; WMP docs are not

            if (!exceedsMaximumDocumentsPerUser)
            {
                foreach (var fileToStoreModel in addDocumentsRequest.FileToStoreModels)
                {
                    var validationResult = _fileValidator.Validate(fileToStoreModel.FileBytes, fileToStoreModel.FileName, addDocumentsRequest.ReceivedByApi, fileUploadReason);

                    if (validationResult.IsFailure)
                    {
                        _logger.LogWarning("File for upload was not successfully validated. Validator returned with [{FileValidationError}].", validationResult.Error);

                        await HandleFailureAsync(addDocumentsRequest.UserAccountId,
                            addDocumentsRequest.FellingApplicationId,
                            StoreFileFailureResult.CreateWithInvalidFileReason(validationResult.Error), fileToStoreModel,
                            userFacingErrors, cancellationToken);

                        continue;
                    }

                    var result = await AddFileAsync(
                        addDocumentsRequest,
                        fileToStoreModel, 
                        fileUploadReason,
                        userFacingErrors,
                        cancellationToken);

                    if (result.IsFailure)
                    {
                        documentStorageFailure = true;
                        _logger.LogWarning(result.Error);
                    }
                    else
                    {
                        documentIds.Add(result.Value);
                    }
                }
            }
            else
            {
                userFacingErrors.Add($"You can only upload up to {_userFileUploadOptions.MaxNumberDocuments} documents.");
                await RaiseFailureAuditEventAsync(
                    addDocumentsRequest.UserAccountId,
                    addDocumentsRequest.FellingApplicationId,
                    null,
                    "Exceeded maximum documents permitted.",
                    cancellationToken);
                return CreateFailureResult(userFacingErrors);
            }
        }
        else
        {
            userFacingErrors.Add("No documents were specified.");
        }

        return userFacingErrors.Any() || documentStorageFailure
            ? CreateFailureResult(userFacingErrors) 
            : CreateSuccessResult(documentIds, userFacingErrors);
    }

    private async Task<Result<Guid>> AddFileAsync(
        AddDocumentsRequest addDocumentsRequest,
        FileToStoreModel fileToStore,
        FileUploadReason fileUploadReason,
        ICollection<string> userFacingErrors,
        CancellationToken cancellationToken)
    {
        var storedLocationPath = Path.Combine(addDocumentsRequest.FellingApplicationId.ToString(), addDocumentsRequest.DocumentPurpose.ToString());
           
        _logger.LogDebug("File with name [{UploadFileName}] of type [{Purpose}] for application having id of [{ApplicationId}] " +
                         "is to be saved to location of [{Location}].", 
            fileToStore.FileName, addDocumentsRequest.DocumentPurpose, addDocumentsRequest.FellingApplicationId, storedLocationPath);
            

        var (isSuccess, _, savedFile, error) = await _storageService.StoreFileAsync(
            fileToStore.FileName,
            fileToStore.FileBytes,
            storedLocationPath,
            addDocumentsRequest.ReceivedByApi,
            fileUploadReason,
            cancellationToken); 

        _logger.LogDebug("Call to store file returned having success of [{Result}].", isSuccess);

        return isSuccess
            ? await HandleSuccessAsync(
                addDocumentsRequest,
                fileToStore,
                savedFile,
                cancellationToken)
            : (await HandleFailureAsync(
                addDocumentsRequest.UserAccountId,
                addDocumentsRequest.FellingApplicationId,
                error,
                fileToStore,
                userFacingErrors,
                cancellationToken)).ConvertFailure<Guid>();
    }

    private async Task<Result<Guid>> HandleSuccessAsync(
        AddDocumentsRequest addDocumentsRequest,
        FileToStoreModel fileToStore, 
        StoreFileSuccessResult savedFile,
        CancellationToken cancellationToken)
    {
        var documentEntity = CreateDocumentEntity(
            addDocumentsRequest.ActorType,
            addDocumentsRequest.UserAccountId,
            addDocumentsRequest.DocumentPurpose,
            fileToStore.FileName, 
            fileToStore.ContentType, 
            savedFile.FileSize, 
            savedFile.Location!,
            addDocumentsRequest.VisibleToApplicant,
            addDocumentsRequest.VisibleToConsultee,
            addDocumentsRequest.FellingApplicationId);
             
        var updateDbResult = await UpdateFellingLicenceApplicationAsync(
            addDocumentsRequest.UserAccountId,
            documentEntity, 
            addDocumentsRequest.FellingApplicationId,
            cancellationToken);

        if (!updateDbResult.IsSuccess)
            return Result.Failure<Guid>(
                $"Unable to save document entity as part of application having id of [{addDocumentsRequest.FellingApplicationId}].");

        await RaiseSuccessAuditEventAsync(
            addDocumentsRequest.UserAccountId,
            addDocumentsRequest.FellingApplicationId,
            documentEntity,
            cancellationToken);

        return Result.Success(documentEntity.Id);
    }

    private async Task<Result> HandleFailureAsync(
        Guid? userAccountId,
        Guid fellingLicenceApplicationId,
        StoreFileFailureResult error, 
        FileToStoreModel fileToStore,
        ICollection<string> userFacingErrors,
        CancellationToken cancellationToken)
    {
        if (error.StoreFileFailureResultReason != StoreFileFailureResultReason.FailedValidation)
        {
            userFacingErrors.Add("Unable to save file at this time.");
            await RaiseFailureAuditEventAsync(userAccountId, fellingLicenceApplicationId, fileToStore, error.StoreFileFailureResultReason.ToString(), cancellationToken);
            return Result.Failure($"Unable to store file named [{fileToStore.FileName}].");
        }

        userFacingErrors.Add(GetErrorMessageForDisplay(fileToStore, error.InvalidReason));
        await RaiseFailureAuditEventAsync(userAccountId, fellingLicenceApplicationId, fileToStore, error.InvalidReason.ToString(), cancellationToken);
        return Result.Failure($"File for upload failed validation {error.InvalidReason}.");
    }

    private static string GetErrorMessageForDisplay(
        FileToStoreModel fileToStore, 
        FileInvalidReason validateInvalidFileError)
    {
        // Don't trust the file name sent by the client. To display the file name, HTML-encode the value.
        var trustedFileNameForDisplay = WebUtility.HtmlEncode(fileToStore.FileName);
 
        return validateInvalidFileError switch
        {
            FileInvalidReason.EmptyFile => $"Contents of '{trustedFileNameForDisplay}' was empty.",
            FileInvalidReason.ExtensionNotSupported =>
                $"The type of file '{trustedFileNameForDisplay}' is not permitted to be attached.",
            FileInvalidReason.FileTooLarge =>
                $"Size of '{trustedFileNameForDisplay}' exceeds the permitted file size.",
            FileInvalidReason.FailedVirusScan =>
                $"File '{trustedFileNameForDisplay}' contains a virus or malware and so cannot be attached.",
            FileInvalidReason.FileSignatureDoesNotMatchSuppliedFileExtension=>
                $"Content of '{trustedFileNameForDisplay}' does not match the type of file and so cannot be attached.",
            FileInvalidReason.InternalError =>
                $"Unexpected error occurred during upload of '{trustedFileNameForDisplay}', please retry.",
            _ => throw new ArgumentOutOfRangeException(nameof(validateInvalidFileError), validateInvalidFileError,
                "Unhandled FileAttachmentReason")
        };
    }
        
    private Document CreateDocumentEntity(
        ActorType actorType,
        Guid? userId,
        DocumentPurpose purpose, 
        string fileName, 
        string contentType,
        long fileSize, 
        string location,
        bool visibleToApplicants,
        bool visibleToConsultees,
        Guid fellingLicenceId
    )
    {
        return new Document
        {
            FileName = fileName,
            CreatedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
            Purpose = purpose,
            MimeType = contentType,
            FileType = _fileTypesProvider.FindFileTypeByMimeTypeWithFallback(contentType).Extension,
            FileSize = fileSize,
            Location = location,
            VisibleToApplicant = visibleToApplicants,
            VisibleToConsultee = visibleToConsultees,
            AttachedByType = actorType,
            AttachedById = userId,
            FellingLicenceApplicationId = fellingLicenceId,
        };
    }
        
    private async Task<Result> UpdateFellingLicenceApplicationAsync(
        Guid? userAccountId,
        Document documentEntity,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var saveResult = await _fellingLicenceApplicationRepository.AddDocumentAsync(documentEntity, cancellationToken);

        if (saveResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, applicationId, userAccountId, _requestContext,
                    new
                    {
                        Document= new
                        {
                            documentEntity.Id,
                            documentEntity.FileName,
                            documentEntity.FileType,
                            documentEntity.Purpose,
                            documentEntity.Location,
                            visibleToApplicant = documentEntity.VisibleToApplicant,
                            visibleToConsultee = documentEntity.VisibleToConsultee
                        }
                    }), 
                cancellationToken);

            return Result.Success();
        }

        var errorDescription = saveResult.Error.GetDescription();

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateFellingLicenceApplicationFailure, 
                applicationId, 
                userAccountId,
                _requestContext,
                new
                {
                    errorDescription,
                    Document= new
                    {
                        documentEntity.Id,
                        documentEntity.FileName,
                        documentEntity.FileType,
                        documentEntity.Purpose,
                        documentEntity.Location,
                        visibleToApplicant = documentEntity.VisibleToApplicant,
                        visibleToConsultee = documentEntity.VisibleToConsultee
                    }
                }),
            cancellationToken);

            _logger.LogError(
                "The supporting documents could not be added due to the '{ErrorDescription}' reason, application id: {ApplicationId}",
                errorDescription, applicationId);

        return Result.Failure(errorDescription);
    }

    private async Task RaiseSuccessAuditEventAsync(
        Guid? userAccountId,
        Guid applicationId, 
        Document document,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AddFellingLicenceAttachmentEvent, applicationId, userAccountId, _requestContext,
            new
            {
                document.Id,
                document.FileName,
                document.Purpose,
                document.Location,
                visibleToApplicant = document.VisibleToApplicant,
                visibleToConsultee = document.VisibleToConsultee
            }
        ), cancellationToken);
    }

    private async Task RaiseFailureAuditEventAsync(
        Guid? userAccountId,
        Guid applicationId, 
        FileToStoreModel? fileToStore,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AddFellingLicenceAttachmentFailureEvent, applicationId, userAccountId, _requestContext,
            new
            {
                FailureReason = reason,
                fileToStore?.FileName,
                fileToStore?.ContentType
            }
        ), cancellationToken);
    }

    private static Result<AddDocumentsSuccessResult, AddDocumentsFailureResult> CreateFailureResult(IEnumerable<string> userFacingErrors)
    {
        return Result.Failure<AddDocumentsSuccessResult, AddDocumentsFailureResult>(
            new AddDocumentsFailureResult(userFacingErrors));
    }

    private static Result<AddDocumentsSuccessResult, AddDocumentsFailureResult> CreateSuccessResult(
        IEnumerable<Guid> documentIds,
        IEnumerable<string> userFacingErrors)
    {
        return Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(
            new AddDocumentsSuccessResult(documentIds, userFacingErrors));
    }
}