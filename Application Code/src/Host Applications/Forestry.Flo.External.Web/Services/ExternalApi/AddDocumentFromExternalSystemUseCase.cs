using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FlaEntities = Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Services.ExternalApi;

/// <summary>
/// Handles the use case for an external system adding a document to a specified Felling Licence.
/// </summary>
public class AddDocumentFromExternalSystemUseCase
{
    private readonly IGetFellingLicenceApplicationForExternalUsers _fellingLicenceApplicationService;
    private readonly IAddDocumentService _addDocumentService;
    private readonly IAuditService<AddDocumentFromExternalSystemUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<AddDocumentFromExternalSystemUseCase> _logger;
    private readonly DocumentVisibilityOptions _options;

    public AddDocumentFromExternalSystemUseCase(
        IGetFellingLicenceApplicationForExternalUsers fellingLicenceApplicationService,
        IAddDocumentService addDocumentService,
        IAuditService<AddDocumentFromExternalSystemUseCase> auditService,
        RequestContext requestContext,
        ILogger<AddDocumentFromExternalSystemUseCase> logger,
        IOptions<DocumentVisibilityOptions> options)
    {
        _fellingLicenceApplicationService = Guard.Against.Null(fellingLicenceApplicationService);
        _addDocumentService = Guard.Against.Null(addDocumentService);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
        _options = Guard.Against.Null(options.Value);
    }

    /// <summary>
    /// Adds LIS Constraint report.
    /// </summary>
    /// <param name="applicationId">The Felling Licence application Id to save the document with</param>
    /// <param name="fileBytes">Bytes of the document which has been sent which are to be saved</param>
    /// <param name="fileName">The original filename of document which has been sent</param>
    /// <param name="contentType">The content-type of the document which has been sent</param>
    /// <param name="documentPurpose">The DocumentPurpose</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IActionResult> AddLisConstraintReportAsync(
        Guid applicationId, 
        byte[] fileBytes,
        string fileName,
        string contentType,
        FlaEntities.DocumentPurpose documentPurpose,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, fellingApplication, error) = await CanAddDocumentAsync(applicationId, cancellationToken);

        if (isFailure)
        {
            await AuditApiLisFailureAsync(applicationId, error, cancellationToken);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        var fileModel = new FileToStoreModel { ContentType = contentType, FileBytes = fileBytes, FileName = fileName };

        var addDocumentRequest = new AddDocumentsRequest
        {
            ActorType = ActorType.System,
            ApplicationDocumentCount = fellingApplication.Documents!.Count(x => x.DeletionTimestamp is null),
            DocumentPurpose = documentPurpose,
            FellingApplicationId = fellingApplication.Id,
            FileToStoreModels = new List<FileToStoreModel> { fileModel },
            ReceivedByApi = true,
            UserAccountId = null,
            VisibleToApplicant = _options.ExternalLisConstraintReport.VisibleToApplicant,
            VisibleToConsultee = _options.ExternalLisConstraintReport.VisibleToConsultees
        };

        var addDocResult = await _addDocumentService.AddDocumentsAsInternalUserAsync(
            addDocumentRequest,
            cancellationToken);

        if (addDocResult.IsFailure)
        {
            foreach (var errorMsg in addDocResult.Error.UserFacingFailureMessages)  
            {
                _logger.LogWarning("Unable to add document due to error - {error}.", errorMsg);
            }
            await AuditApiLisFailureAsync(applicationId, string.Join(",", addDocResult.Error.UserFacingFailureMessages), cancellationToken);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        _logger.LogDebug("Document named {filename}, having type of {type}, size of {size} bytes and purpose of {documentPurpose} was " +
                         "successfully added to Felling Licence application having identifier of {id} and reference {reference}."
            ,fileModel.FileName, fileModel.ContentType, fileModel.FileBytes.Length, documentPurpose, fellingApplication.Id, fellingApplication.ApplicationReference);
        
        await AuditApiLisSuccessAsync(fellingApplication.Id, fileModel, documentPurpose, cancellationToken);
        return new StatusCodeResult(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Execute rules to assert whether a document can be added to an existing Felling licence application.
    /// </summary>
    /// <param name="applicationId"></param>
    /// <param name="cancellationToken"></param>
    private async Task<Result<FlaEntities.FellingLicenceApplication>> CanAddDocumentAsync(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        // can "impersonate" an FC user as this is a system-triggered use case
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = true
        };

        var fellingLicenceApplication = await _fellingLicenceApplicationService.GetApplicationByIdAsync(applicationId, userAccessModel, cancellationToken);

        if (fellingLicenceApplication.IsFailure)
        {
            _logger.LogWarning("Could not find Felling Licence application having identifier of {id}.", applicationId);
            return Result.Failure<FlaEntities.FellingLicenceApplication>("Felling Licence application not found.");
        }

        var fla = fellingLicenceApplication.Value;

        if (!HasStatusForAccepting())
        {
            _logger.LogWarning("Felling Licence having application identifier of {id} and reference {reference} is not in the correct state to accept a new document."
                , fla.Id, fla.ApplicationReference);
            return Result.Failure<FlaEntities.FellingLicenceApplication>("Felling Licence application has incorrect state to accept document.");
        }

        return fla;

        bool HasStatusForAccepting()
        {
            var mostRecentStatusHistory = fla.StatusHistories.MaxBy(x => x.Created);

            return mostRecentStatusHistory is
            {
                Status: FlaEntities.FellingLicenceStatus.Draft or FlaEntities.FellingLicenceStatus.WithApplicant or FlaEntities.FellingLicenceStatus.ReturnedToApplicant
            };
        }
    }

    private async Task AuditApiLisFailureAsync(
        Guid? applicationId,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.LISConstraintReportConsumedFailure,
            applicationId,
            null,
            _requestContext,
            new
            {
                Error = error
            })
            , cancellationToken);
    }

    private async Task AuditApiLisSuccessAsync(
        Guid? applicationId,
        FileToStoreModel fileToStoreModel,
        FlaEntities.DocumentPurpose documentPurpose,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.LISConstraintReportConsumedOk,
            applicationId,
            null,
            _requestContext,
            new
            {
                name = fileToStoreModel.FileName,
                contentType = fileToStoreModel.ContentType,
                size = fileToStoreModel.FileBytes.Length,
                documentPurpose
            })
            , cancellationToken);
    }
}