using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Handles use case for a user getting/viewing a supporting document
/// from a felling licence application
/// </summary>
public class GetSupportingDocumentUseCase : IGetSupportingDocumentUseCase
{
    private readonly IGetDocumentServiceInternal _getDocumentService;
    private readonly ILogger<GetSupportingDocumentUseCase> _logger;

    public GetSupportingDocumentUseCase (
        IGetDocumentServiceInternal getDocumentService,
        ILogger<GetSupportingDocumentUseCase> logger)
    {
        _getDocumentService = Guard.Against.Null(getDocumentService);
        _logger = logger;
    }

    public async Task<Result<FileContentResult>> GetSupportingDocumentAsync(
        InternalUser user,
        Guid applicationId,
        Guid documentIdentifier,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve document from storage, for application having Id of [{applicationId}] for document with Id [{documentId}]."
            , applicationId, documentIdentifier);

        var request = new GetDocumentRequest
        {
            DocumentId = documentIdentifier,
            ApplicationId = applicationId,
            UserId = user.UserAccountId!.Value
        };
        var (isSuccess, _, fileToStoreModel, error) = await _getDocumentService.GetDocumentAsync(
            request, cancellationToken);

        if (isSuccess)
        {
            _logger.LogDebug("Document retrieved from storage ok. has id of [{documentId}], type is [{documentType}] with original name of [{documentName}]."
                , documentIdentifier, fileToStoreModel.ContentType, fileToStoreModel.FileName);

            var fileContentResult = new FileContentResult(fileToStoreModel.FileBytes, fileToStoreModel.ContentType)
            {
                FileDownloadName = fileToStoreModel.FileName
            };

            return Result.Success(fileContentResult);
        }

        _logger.LogWarning("Document having identifier of [{documentId}] could not be retrieved from storage, error is :[{error}]."
            , documentIdentifier, error);

        return Result.Failure<FileContentResult>("File could not be retrieved");
    }
}