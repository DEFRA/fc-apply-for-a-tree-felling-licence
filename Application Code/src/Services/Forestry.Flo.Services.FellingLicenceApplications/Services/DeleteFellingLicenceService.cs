using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FileStorage.Services;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class DeleteFellingLicenceService : IDeleteFellingLicenceService
{
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationExternalRepository;
    private readonly IGetFellingLicenceApplicationForExternalUsers _getFellingLicenceApplicationService;
    private readonly ILogger<DeleteFellingLicenceService> _logger;
    private readonly IAuditService<DeleteFellingLicenceService> _audit;
    private readonly RequestContext _requestContext;
    private readonly IFileStorageService _storageService;

    public DeleteFellingLicenceService(
        IAuditService<DeleteFellingLicenceService> auditService,
        RequestContext requestContext,
        ILogger<DeleteFellingLicenceService> logger,
        IFellingLicenceApplicationExternalRepository fellingLicenceApplicationExternalRepository,
        IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationService,
        IFileStorageService storageService )
    {
        _getFellingLicenceApplicationService = Guard.Against.Null(getFellingLicenceApplicationService);
        _fellingLicenceApplicationExternalRepository = Guard.Against.Null(fellingLicenceApplicationExternalRepository);
        _logger = Guard.Against.Null(logger);
        _audit = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _storageService = Guard.Against.Null(storageService);
    }


    public async Task<Result> DeleteDraftApplicationAsync(
        Guid applicationId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        var applicationResult =
            await _getFellingLicenceApplicationService.GetApplicationByIdAsync(applicationId, userAccessModel,
                cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            _logger.LogError($"Failed to get {nameof(FellingLicenceApplication)} with ID {applicationId}, error {applicationResult.Error}");

            return Result.Failure($"Failed to get {nameof(FellingLicenceApplication)}");
        }

        var applicationStatus =
            applicationResult.Value.StatusHistories.OrderByDescending(s => s.Created).First().Status;
        if (applicationStatus is not FellingLicenceStatus.Draft)
        {
            _logger.LogError($"{nameof(FellingLicenceApplication)} with ID {applicationId} cannot be deleted as that option is only for applications currently in draft status");

            return Result.Failure($"{nameof(FellingLicenceApplication)} is not a draft");
        }

        var result = await _fellingLicenceApplicationExternalRepository.DeleteFlaAsync(applicationResult.Value,  cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError($"Could not delete the {nameof(FellingLicenceApplication)} with ID {applicationId}");
            
            return Result.Failure($"{nameof(FellingLicenceApplication)} could not be deleted!");
        }

        return Result.Success();
    }


    public async Task<Result> PermanentlyRemoveDocumentAsync(
        Guid applicationId,
        Document document,
        CancellationToken cancellationToken)
    {
        var removeFileResult = await _storageService.RemoveFileAsync(document.Location!, cancellationToken);

        _logger.LogDebug("Call to remove file with id of [{documentIdentifier}] with location of [{location}] " +
                         "has success result of [{result}],", document.Id, document.Location, removeFileResult.IsSuccess);

        if (removeFileResult.IsFailure)
        {
            _logger.LogWarning(
                "Did not receive Success result when removing file with id of [{id}], received error [{error}].",
                document.Id, removeFileResult.Error);

            await HandleFileRemovalFailureAsync(
                null,
                applicationId,
                document.Id,
                document.Purpose,
                removeFileResult.Error.ToString(),
                cancellationToken);

            return Result.Failure("Could not remove document from file storage");
        }

        _logger.LogDebug(
            "File with id of [{documentIdentifier}] and location of [{location}] was successfully removed from storage.",
            document.Id, document.Location);

        await HandleFileRemovedSuccessfullyAsync(
            null,
            applicationId,
            document,
            cancellationToken);

        return Result.Success();
    }

    private async Task HandleFileRemovedSuccessfullyAsync(
        Guid? userAccountId,
        Guid applicationId,
        Document document,
        CancellationToken cancellationToken)
    {
        await _audit.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.RemoveFellingLicenceAttachmentEvent,
            applicationId,
            userAccountId,
            _requestContext,
            new
            {
                documentId = document.Id,
                document.Purpose,
                document.FileName,
                document.Location
            }
        ), cancellationToken);
    }

    private async Task HandleFileRemovalFailureAsync(
        Guid? userAccountId,
        Guid applicationId,
        Guid documentId,
        DocumentPurpose? purpose,
        string reason,
        CancellationToken cancellationToken)
    {
        await _audit.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.RemoveFellingLicenceAttachmentFailureEvent,
            applicationId,
            userAccountId,
            _requestContext,
            new
            {
                purpose,
                documentId,
                FailureReason = reason
            }
        ), cancellationToken);
    }

}
