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

public class InternalGetDocumentService : GetDocumentServiceBase, IGetDocumentServiceInternal
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;

    public InternalGetDocumentService(
        IFileStorageService storageService,
        IAuditService<GetDocumentServiceBase> auditService,
        RequestContext requestContext,
        ILogger<GetDocumentServiceBase> logger,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository) 
        : base(storageService, auditService, requestContext, logger)
    {
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
    }

    public async Task<Result<FileToStoreModel>> GetDocumentAsync(
        GetDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var (hasValue, documentToGet) =
            await _fellingLicenceApplicationInternalRepository.GetDocumentByIdAsync(
                request.ApplicationId, request.DocumentId, cancellationToken);

        if (!hasValue)
        {
            _logger.LogWarning("Could not get document with id {DocumentId} on application with id {ApplicationId}."
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
                "File with document id {DocumentId} and location {Location} was successfully retrieved from storage.",
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
