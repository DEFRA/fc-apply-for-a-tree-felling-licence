using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Handles use case for a external applicant getting/viewing a supporting document
/// from a felling licence application
/// </summary>
public class GetSupportingDocumentUseCase
{
    private readonly IGetDocumentServiceExternal _getDocumentService;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly ILogger<GetSupportingDocumentUseCase> _logger;

    public GetSupportingDocumentUseCase (
        IGetDocumentServiceExternal getDocumentService,
        IRetrieveUserAccountsService retrieveUserAccountsService,
        ILogger<GetSupportingDocumentUseCase> logger)
    {
        _getDocumentService = Guard.Against.Null(getDocumentService);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _logger = logger;
    }

    public async Task<Result<FileContentResult>> GetSupportingDocumentAsync(
        ExternalApplicant user,
        Guid applicationId, 
        Guid documentIdentifier, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve document from storage, for application having Id of [{applicationId}] for document with Id [{documentId}].",
            applicationId, documentIdentifier);

        var userAccessModel = await _retrieveUserAccountsService.RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken);

        var request = new GetDocumentExternalRequest
        {
            ApplicationId = applicationId,
            DocumentId = documentIdentifier,
            UserId = user.UserAccountId!.Value,
            UserAccessModel = userAccessModel.Value 
        };

        var (isSuccess, _, fileToStoreModel, error) = await _getDocumentService.GetDocumentAsync(
            request,
            cancellationToken);

        if (isSuccess)
        {
            _logger.LogDebug("Document with id {DocumentId} successfully retrieved, type is {DocumentType} and filename {FileName}.",
                documentIdentifier, fileToStoreModel.ContentType, fileToStoreModel.FileName);

            var fileContentResult = new FileContentResult(fileToStoreModel.FileBytes, fileToStoreModel.ContentType)
            {
                FileDownloadName = fileToStoreModel.FileName
            };

            return Result.Success(fileContentResult);
        }

        _logger.LogWarning("Document with id {DocumentId} could not be retrieved from storage, error {Error}",
            documentIdentifier, error);

        return Result.Failure<FileContentResult>("File could not be retrieved");
    }
}
