using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Service class for getting a document for a given <see cref="FellingLicenceApplication"/>. 
/// </summary>
public abstract class GetDocumentServiceBase
{
    protected readonly IAuditService<GetDocumentServiceBase> _auditService;
    protected readonly IFileStorageService _storageService;
    protected readonly RequestContext _requestContext;
    protected readonly ILogger<GetDocumentServiceBase> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="GetDocumentServiceBase"/>.
    /// </summary>
    /// <param name="storageService">The configured <see cref="IFileStorageService"/> to be used.</param>
    /// <param name="auditService"></param>
    /// <param name="requestContext"></param>
    /// <param name="logger"></param>
    protected GetDocumentServiceBase(
        IFileStorageService storageService,
        IAuditService<GetDocumentServiceBase> auditService,
        RequestContext requestContext,
        ILogger<GetDocumentServiceBase> logger)
    {
        _storageService = Guard.Against.Null(storageService);
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _logger = logger;
    }

    protected async Task<Result<FileToStoreModel>> HandleGetFileSuccessAsync(
        Guid userAccountId,
        Guid applicationId, 
        Document documentToGet,
        FileToStoreModel storedFileModel,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.GetFellingLicenceAttachmentEvent, applicationId, userAccountId,_requestContext,
            new
            {
                documentId = documentToGet.Id,
                documentToGet.Purpose,
                documentToGet.FileName,
                documentToGet.Location,
                storedFileModelName=storedFileModel.FileName,
                storedFileModelContentType=storedFileModel.ContentType,
            }
        ), cancellationToken);

        return Result.Success(storedFileModel);
    }

    protected async Task<Result<FileToStoreModel>> HandleGetFileFailureAsync(
        Guid userAccountId,
        Guid applicationId, 
        Guid documentId,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.GetFellingLicenceAttachmentFailureEvent, applicationId, userAccountId, _requestContext,
            new
            {
                documentId,
                failureReason = reason
            }
        ), cancellationToken);

        return Result.Failure<FileToStoreModel>(reason);
    }
}