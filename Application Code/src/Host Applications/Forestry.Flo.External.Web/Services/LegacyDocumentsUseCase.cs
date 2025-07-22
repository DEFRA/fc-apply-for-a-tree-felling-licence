using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services;

public class LegacyDocumentsUseCase
{
    private readonly IRetrieveLegacyDocuments _retrieveLegacyDocuments;
    private readonly RequestContext _requestContext;
    private readonly ILogger<LegacyDocumentsUseCase> _logger;

    public LegacyDocumentsUseCase(
        IRetrieveLegacyDocuments retrieveLegacyDocuments,
        RequestContext requestContext,
        ILogger<LegacyDocumentsUseCase> logger)
    {
        ArgumentNullException.ThrowIfNull(retrieveLegacyDocuments);
        ArgumentNullException.ThrowIfNull(requestContext);

        _retrieveLegacyDocuments = retrieveLegacyDocuments;
        _requestContext = requestContext;
        _logger = logger;
    }

    public async Task<Result<IList<LegacyDocumentModel>>> RetrieveLegacyDocumentsAsync(
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request for legacy documents for user with id {UserId}", user.UserAccountId);

        return await _retrieveLegacyDocuments.RetrieveLegacyDocumentsForUserAsync(user.UserAccountId!.Value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<FileContentResult>> RetrieveLegacyDocumentContentAsync(
        ExternalApplicant user,
        Guid legacyDocumentId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request to retrieve legacy document with id {LegacyDocumentId} for user with id {UserId}", legacyDocumentId, user.UserAccountId);

        var result = await _retrieveLegacyDocuments
            .RetrieveLegacyDocumentContentAsync(user.UserAccountId!.Value, legacyDocumentId, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            _logger.LogError("Could not retrieve file content with error {Error}", result.Error);
            return result.ConvertFailure<FileContentResult>();
        }

        _logger.LogDebug("File content retrieved, returning FileContentResult");
        var fileContentResult = new FileContentResult(result.Value.FileBytes, result.Value.ContentType)
        {
            FileDownloadName = result.Value.FileName
        };
        return Result.Success(fileContentResult);
    }
}