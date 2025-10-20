using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NodaTime;
using System.Globalization;
using Forestry.Flo.HostApplicationsCommon.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Handles the use case for a user generating a pdf of the felling licence application and attaching it
/// </summary>
public class GeneratePdfApplicationUseCase : GeneratePdfApplicationUseCaseBase, IGeneratePdfApplicationUseCase
{
    private readonly IAuditService<GeneratePdfApplicationUseCaseBase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IApproverReviewService _approverReviewService;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly IRetrieveUserAccountsService _externalAccountService;
    private readonly IUserAccountService _internalAccountService;
    private readonly IRetrieveWoodlandOwners _woodlandOwnerService;
    private readonly IPropertyProfileRepository _propertyProfileRepository;
    private readonly IAddDocumentService _addDocumentService;
    private readonly ICreateApplicationSnapshotDocumentService _createApplicationSnapshotDocumentService;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService;
    private readonly ILogger<GeneratePdfApplicationUseCase> _logger;
    private readonly IForesterServices _iForesterServices;
    private readonly IForestryServices _forestryAccess;
    private readonly DocumentVisibilityOptions _documentVisibilityOptions;
    private readonly IClock _clock;
    private readonly ICalculateConditions _calculateConditionsService;
    private readonly PDFGeneratorAPIOptions _licencePdfOptions;

    public GeneratePdfApplicationUseCase(
        IAuditService<GeneratePdfApplicationUseCaseBase> auditService,
        RequestContext requestContext,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IApproverReviewService approverReviewService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IUserAccountService internalAccountService,
        IRetrieveUserAccountsService externalAccountService,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IPropertyProfileRepository propertyProfileRepository,
        IAddDocumentService addDocumentService,
        ICreateApplicationSnapshotDocumentService createApplicationSnapshotDocumentServiceService,
        IForesterServices iForesterServices,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IOptions<DocumentVisibilityOptions> documentVisibilityOptions,
        IClock clock,
        ICalculateConditions calculateConditionsService,
        IOptions<PDFGeneratorAPIOptions> licencePdfOptions,
        ILogger<GeneratePdfApplicationUseCase> logger) :
        base(
            auditService,
            requestContext,
            getWoodlandOfficerReviewService,
            internalAccountService,
            externalAccountService,
            woodlandOwnerService,
            propertyProfileRepository,
            iForesterServices,
            getConfiguredFcAreasService,
            clock,
            licencePdfOptions,
            calculateConditionsService,
            approverReviewService,
            logger)
    {
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _internalAccountService = Guard.Against.Null(internalAccountService);
        _externalAccountService = Guard.Against.Null(externalAccountService);
        _woodlandOwnerService = Guard.Against.Null(woodlandOwnerService);
        _propertyProfileRepository = Guard.Against.Null(propertyProfileRepository);
        _addDocumentService = Guard.Against.Null(addDocumentService);
        _createApplicationSnapshotDocumentService = Guard.Against.Null(createApplicationSnapshotDocumentServiceService);
        _iForesterServices = Guard.Against.Null(iForesterServices);
        _documentVisibilityOptions = Guard.Against.Null(documentVisibilityOptions.Value);
        _clock = Guard.Against.Null(clock);
        _calculateConditionsService = Guard.Against.Null(calculateConditionsService);
        _licencePdfOptions = Guard.Against.Null(licencePdfOptions.Value);
        _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
        _logger = logger;
        _approverReviewService = Guard.Against.Null(approverReviewService);
    }

    /// <inheritdoc />
    public async Task<Result<Document>> GeneratePdfApplicationAsync(
        Guid internalUserId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating PDF for application with id {ApplicationId} by user {UserId}", applicationId, internalUserId);

        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicenceFailure, null, internalUserId, _requestContext,
                new { Error = "Could not find Fla with id" }), cancellationToken);

            _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
            return Result.Failure<Document>($"Felling licence application not found, application id: {applicationId}");
        }

        var isFinal =
            application.Value.GetCurrentStatus() is FellingLicenceStatus.Approved;

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.GeneratingPdfFellingLicence, null, internalUserId, _requestContext,
            new { applicationId, isFinal = isFinal, }), cancellationToken);

        var createRequestModel = await CreateRequestModelAsync(application.Value, cancellationToken);
        if (createRequestModel.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicenceFailure, null, internalUserId, _requestContext,
                new { applicationId, isFinal = isFinal, Error = createRequestModel.Error }), cancellationToken);

            _logger.LogError("Failed to get details need to generate a pdf for application with id: {ApplicationId}", applicationId);
            return Result.Failure<Document>($"Failed to get details need to generate a pdf for application with id: {applicationId}");
        }

        var result = await _createApplicationSnapshotDocumentService.CreateApplicationSnapshotAsync(applicationId, createRequestModel.Value, cancellationToken);
        if (result.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicenceFailure, null, internalUserId, _requestContext,
                new { applicationId, isFinal = isFinal, Error = result.Error }), cancellationToken);

            _logger.LogError("Could not retrieve application with id {ApplicationId}", applicationId);
            return Result.Failure<Document>("Could not generate a new document.");
        }


        var fileName = isFinal 
            ? $"Fla_{application.Value.ApplicationReference}_Approved_{_clock.GetCurrentInstant().ToDateTimeUtc().Date:dd/MM/yyyy}.pdf" 
            : $"Fla_{application.Value.ApplicationReference}_Preview_{_clock.GetCurrentInstant().ToDateTimeUtc().Date:dd/MM/yyyy}.pdf";

        var fileModel = new FileToStoreModel
        {
            ContentType = "application/pdf",
            FileBytes = result.Value,
            FileName = fileName
        };

        // licence previews should not be visible to applicants or consultees
        _logger.LogDebug("Attempting to attach generated licence document to application with id: {ApplicationId}", applicationId);

        var addDocumentRequest = new AddDocumentsExternalRequest
        {
            WoodlandOwnerId = application.Value.WoodlandOwnerId,
            ActorType = ActorType.InternalUser,
            ApplicationDocumentCount = 0,
            DocumentPurpose = DocumentPurpose.ApplicationDocument,
            FellingApplicationId = application.Value.Id,
            FileToStoreModels = new List<FileToStoreModel> { fileModel },
            ReceivedByApi = false,
            UserAccountId = (Guid)internalUserId!,
            VisibleToApplicant = _documentVisibilityOptions.ApplicationDocument.VisibleToApplicant && isFinal,
            VisibleToConsultee = _documentVisibilityOptions.ApplicationDocument.VisibleToConsultees && isFinal
        };

        _logger.LogDebug("Adding Application document to application with id {ApplicationId}", applicationId);
        
        var addDocResult = await _addDocumentService.AddDocumentsAsInternalUserAsync(
            addDocumentRequest,
            cancellationToken);
        if (addDocResult.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicenceFailure, null, internalUserId, _requestContext,
                new { applicationId, isFinal = isFinal, Error = addDocResult.Error }), cancellationToken);

            _logger.LogError("Could not add document to application with id {ApplicationId}", applicationId);
            return Result.Failure<Document>("Could not generate and attach new application document.");
        }

        var fla = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        var newDoc = fla.Value.Documents!
            .OrderByDescending(x => x.CreatedTimestamp)
            .FirstOrDefault(x => x.Purpose == DocumentPurpose.ApplicationDocument);

        if (newDoc != null)
        {
            _logger.LogDebug("Added Application document with document Id {documentId} to application with id {ApplicationId}", newDoc.Id, applicationId);
            return Result.Success(newDoc);
        }
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.GeneratingPdfFellingLicenceFailure, null, internalUserId, _requestContext,
            new { applicationId, isFinal = isFinal, Error = "File not found" }), cancellationToken);

        _logger.LogError("Could not retrieve the new document from application with id {ApplicationId}", applicationId);
        return Result.Failure<Document>($"Could not retrieve the new document from application with id {applicationId}");
    }
}