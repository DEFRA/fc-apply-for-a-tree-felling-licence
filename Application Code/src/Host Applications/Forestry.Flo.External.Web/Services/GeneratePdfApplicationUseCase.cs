using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.HostApplicationsCommon.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Handles the use case for an external user generating a pdf of the felling licence application and attaching it to the application.
/// </summary>
public class GeneratePdfApplicationUseCase : GeneratePdfApplicationUseCaseBase
{
    private readonly IAuditService<GeneratePdfApplicationUseCaseBase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly IAddDocumentService _addDocumentService;
    private readonly ICreateApplicationSnapshotDocumentService _createApplicationSnapshotDocumentService;
    private readonly ILogger<GeneratePdfApplicationUseCase> _logger;
    private readonly IClock _clock;

    public GeneratePdfApplicationUseCase(
        IAuditService<GeneratePdfApplicationUseCaseBase> auditService,
        RequestContext requestContext,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IUserAccountService internalAccountService,
        IRetrieveUserAccountsService externalAccountService,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IPropertyProfileRepository propertyProfileRepository,
        IAddDocumentService addDocumentService,
        ICreateApplicationSnapshotDocumentService createApplicationSnapshotDocumentServiceService,
        IForesterServices iForesterServices,
        IGetConfiguredFcAreas getConfiguredFcAreasService, 
        IClock clock,
        ICalculateConditions calculateConditions,
        IApproverReviewService approverReviewService,
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
            calculateConditions,
            approverReviewService,
            logger)
    {
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        Guard.Against.Null(getWoodlandOfficerReviewService);
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _addDocumentService = Guard.Against.Null(addDocumentService);
        _createApplicationSnapshotDocumentService = Guard.Against.Null(createApplicationSnapshotDocumentServiceService);
        _clock = Guard.Against.Null(clock);
        _logger = logger;
    }

    /// <summary>
    /// Generates a preview licence document for a specified application and then adds it as a supporting document.
    /// </summary>
    /// <param name="userId">The identifier for the <see cref="ExternalApplicant"/> generating the preview document.</param>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> containing a document representing the generated preview document, or an error if unsuccessful.</returns>
    public async Task<Result<Document>> GeneratePreviewDocumentAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicence,
                null,
                userId,
                _requestContext,
                new {
                    ApplicationId = applicationId,
                    IsFinal = false,
                }),
            cancellationToken);

        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue) {
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.GeneratingPdfFellingLicenceFailure,
                    null,
                    userId,
                    _requestContext,
                    new {
                        ApplicationId = applicationId,
                        IsFinal = false,
                        Error = "Unable to retrieve application with specified ID"
                    }),
                cancellationToken);

            _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
            return Result.Failure<Document>($"Felling licence application not found, application id: {applicationId}");
        }

        var createRequestModel = await CreateRequestModelAsync(application.Value, cancellationToken);
        if (createRequestModel.IsFailure) {
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.GeneratingPdfFellingLicenceFailure,
                    null,
                    userId,
                    _requestContext,
                    new {
                        ApplicationId = applicationId,
                        IsFinal = false,
                        Error = createRequestModel.Error
                    }),
                cancellationToken);

            _logger.LogError("Failed to get details need to generate a pdf for application with id: {ApplicationId}", applicationId);
            return Result.Failure<Document>($"Failed to get details need to generate a pdf for application with id: {applicationId}");
        }

        var result = await _createApplicationSnapshotDocumentService.CreateApplicationSnapshotAsync(applicationId, createRequestModel.Value, cancellationToken);
        if (result.IsFailure) {
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.GeneratingPdfFellingLicenceFailure,
                    null,
                    userId,
                    _requestContext,
                    new {
                        ApplicationId = applicationId,
                        IsFinal = false,
                        Error = result.Error
                    }),
                cancellationToken);

            _logger.LogError("Unable to create application snapshot for PDF generation for application {id}, with error: {error}", applicationId, result.Error);
            return Result.Failure<Document>("Unable to generate application snapshot for PDF generation");
        }

        var fileName = $"Fla_{application.Value.ApplicationReference}_Preview_{_clock.GetCurrentInstant().ToDateTimeUtc().Date:dd/MM/yyyy}.pdf";

        var fileModel = new FileToStoreModel {
            ContentType = "application/pdf",
            FileBytes = result.Value,
            FileName = fileName
        };

        // licence previews should not be visible to applicants or consultees

        var addDocumentRequest = new AddDocumentsExternalRequest {
            WoodlandOwnerId = application.Value.WoodlandOwnerId,
            ActorType = ActorType.System,
            ApplicationDocumentCount = 0,
            DocumentPurpose = DocumentPurpose.ApplicationDocument,
            FellingApplicationId = application.Value.Id,
            FileToStoreModels = new List<FileToStoreModel> { fileModel },
            ReceivedByApi = false,
            UserAccountId = userId!,
            VisibleToApplicant = false,
            VisibleToConsultee = false
        };

        _logger.LogDebug("Adding Application document to application with id {ApplicationId}", applicationId);

        var addDocResult = await _addDocumentService.AddDocumentsAsInternalUserAsync(
            addDocumentRequest,
            cancellationToken);

        if (addDocResult.IsFailure) {
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.GeneratingPdfFellingLicenceFailure,
                    null,
                    userId,
                    _requestContext,
                    new {
                        ApplicationId = applicationId,
                        IsFinal = false,
                        Error = addDocResult.Error
                    }),
                cancellationToken);

            _logger.LogError("Could not add document to application with id {ApplicationId}", applicationId);

            return Result.Failure<Document>("Could not generate and attach new application document.");
        }

        var fla = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);

        var newDoc = fla.Value.Documents!
            .OrderByDescending(x => x.CreatedTimestamp)
            .FirstOrDefault(x => x.Purpose == DocumentPurpose.ApplicationDocument);

        if (newDoc != null) {
            _logger.LogDebug("Added Application document with document Id {documentId} to application with id {ApplicationId}", newDoc.Id, applicationId);
            return Result.Success(newDoc);
        }

        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicenceFailure,
                null,
                userId,
                _requestContext,
                new {
                    ApplicationId = applicationId,
                    IsFinal = false,
                    Error = "File not found"
                }),
            cancellationToken);

        _logger.LogError("Could not retrieve the new document from application with id {ApplicationId}", applicationId);
        return Result.Failure<Document>($"Could not retrieve the new document from application with id {applicationId}");
    }
}