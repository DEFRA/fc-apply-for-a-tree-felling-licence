using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ExternalGetDocumentService : GetDocumentServiceBase, IGetDocumentServiceExternal
{
    private readonly IGetFellingLicenceApplicationForExternalUsers _getFellingLicenceApplicationForExternalUsersService;
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationExternalRepository;

    public ExternalGetDocumentService(
        IFileStorageService storageService,
        IAuditService<GetDocumentServiceBase> auditService,
        IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationForExternalUsersService,
        RequestContext requestContext,
        ILogger<GetDocumentServiceBase> logger,
        IFellingLicenceApplicationExternalRepository fellingLicenceApplicationExternalRepository) 
        : base(storageService, auditService, requestContext, logger)
    {
        _getFellingLicenceApplicationForExternalUsersService = Guard.Against.Null(getFellingLicenceApplicationForExternalUsersService);
        _fellingLicenceApplicationExternalRepository = Guard.Against.Null(fellingLicenceApplicationExternalRepository);
    }

    public async Task<Result<FileToStoreModel>> GetDocumentAsync(
        GetDocumentExternalRequest request,
        CancellationToken cancellationToken)
    {
        var canAccessResult = await _getFellingLicenceApplicationForExternalUsersService.GetApplicationByIdAsync(
            request.ApplicationId,
            request.UserAccessModel, 
            cancellationToken);

        if (canAccessResult.IsFailure)
        {
            _logger.LogWarning("Could not access the document with id {DocumentId} for application with id {ApplicationId} " +
                               "as the user with the id of {userId} does not have access to the application.", 
                request.DocumentId, request.ApplicationId, request.UserAccessModel.UserAccountId);

            return await HandleGetFileFailureAsync(request.UserId, request.ApplicationId, request.DocumentId,
                "Not authorised to access the file", cancellationToken);
        }

        var (hasValue, documentToGet) =
            await _fellingLicenceApplicationExternalRepository.GetDocumentByIdAsync(
                request.ApplicationId, request.DocumentId, cancellationToken);

        if (!hasValue)
        {
            _logger.LogWarning("Could not get document with id {DocumentId} for application with id {ApplicationId}."
                , request.DocumentId, request.ApplicationId);
            return await HandleGetFileFailureAsync(request.UserId, request.ApplicationId, request.DocumentId,
                "Document not found", cancellationToken);
        }

        var getFileResult = await _storageService.GetFileAsync(documentToGet.Location!, cancellationToken);

        if (getFileResult.IsSuccess)
        {
            var storedFileModel = new FileToStoreModel
            {
                ContentType = documentToGet.MimeType,
                FileBytes = getFileResult.Value.FileBytes,
                FileName = documentToGet.FileName
            };

            _logger.LogDebug(
                "Document with id {DocumentId} and location {Location} was successfully retrieved from storage.",
                request.DocumentId, documentToGet.Location);

            return await HandleGetFileSuccessAsync(request.UserId, request.ApplicationId, documentToGet, storedFileModel,
                cancellationToken);
        }

        _logger.LogError("Failed to retrieve file with location {Location} for document with id {DocumentId} - error {Error}",
            documentToGet.Location!, request.DocumentId, getFileResult.Error);

        return await HandleGetFileFailureAsync(request.UserId, request.ApplicationId, request.DocumentId,
            getFileResult.Error.ToString(), cancellationToken);
    }
}