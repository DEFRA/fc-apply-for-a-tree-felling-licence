using System.Globalization;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
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

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Handles the use case for a user generating a pdf of the felling licence application and attaching it
/// </summary>
public class GeneratePdfApplicationUseCase
{
    private readonly IAuditService<GeneratePdfApplicationUseCase> _auditService;
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
        IAuditService<GeneratePdfApplicationUseCase> auditService,
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
        ILogger<GeneratePdfApplicationUseCase> logger)
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

    /// <summary>
    /// Generates a pdf for the current submitted version of the application and then adds it as a supporting document.
    /// </summary>
    /// <param name="user">The <see cref="InternalUser"/> requesting the generation.</param>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="approved">A flag indicating whether an application has been approved.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> containing a <see cref="Document"/> representing the generated PDF, or an error if unsuccessful.</returns>
    public async Task<Result<Document>> GeneratePdfApplicationAsync(
        InternalUser user,
        Guid applicationId,
        bool approved,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating PDF for application with id {ApplicationId} by user {UserId}", applicationId, user.UserAccountId);

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.GeneratingPdfFellingLicence, null, user.UserAccountId, _requestContext,
            new { applicationId, isFinal = approved, }), cancellationToken);

        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicenceFailure, null, user.UserAccountId, _requestContext,
                new { applicationId, isFinal = approved, Error = "Could not find Fla with id" }), cancellationToken);

            _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
            return Result.Failure<Document>($"Felling licence application not found, application id: {applicationId}");
        }

        var createRequestModel = await CreateRequestModelAsync(application.Value, approved, cancellationToken);
        if (createRequestModel.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicenceFailure, null, user.UserAccountId, _requestContext,
                new { applicationId, isFinal = approved, Error = createRequestModel.Error }), cancellationToken);

            _logger.LogError("Failed to get details need to generate a pdf for application with id: {ApplicationId}", applicationId);
            return Result.Failure<Document>($"Failed to get details need to generate a pdf for application with id: {applicationId}");
        }

        var result = await _createApplicationSnapshotDocumentService.CreateApplicationSnapshotAsync(applicationId, createRequestModel.Value, cancellationToken);
        if (result.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.GeneratingPdfFellingLicenceFailure, null, user.UserAccountId, _requestContext,
                new { applicationId, isFinal = approved, Error = result.Error }), cancellationToken);

            _logger.LogError("Could not retrieve application with id {ApplicationId}", applicationId);
            return Result.Failure<Document>("Could not generate a new document.");
        }

        var isFinal = 
            application.Value.StatusHistories.MaxBy(x => x.Created)?.Status is FellingLicenceStatus.SentForApproval && approved;

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
            UserAccountId = (Guid)user.UserAccountId!,
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
                AuditEvents.GeneratingPdfFellingLicenceFailure, null, user.UserAccountId, _requestContext,
                new { applicationId, isFinal = approved, Error = addDocResult.Error }), cancellationToken);

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
            AuditEvents.GeneratingPdfFellingLicenceFailure, null, user.UserAccountId, _requestContext,
            new { applicationId, isFinal = approved, Error = "File not found" }), cancellationToken);

        _logger.LogError("Could not retrieve the new document from application with id {ApplicationId}", applicationId);
        return Result.Failure<Document>($"Could not retrieve the new document from application with id {applicationId}");
    }

    /// <summary>
    /// Creates a <see cref="PDFGeneratorRequest"/> to send to the PDF generator to generate a felling licence document for the supplied application.
    /// </summary>
    /// <param name="application">The relevant application to create the PDF for. </param>
    /// <param name="approved">A flag indicating whether an application has been approved.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns a <see cref="PDFGeneratorRequest"/> for use in generating a pdf of the application.</returns>
    private async Task<Result<PDFGeneratorRequest>> CreateRequestModelAsync(
        Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application,
        bool approved,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Creating a PDF Generator Request for the application with id {application.Id}");
        var userAccount = await _externalAccountService.RetrieveUserAccountByIdAsync(application.CreatedById, cancellationToken);
        if (userAccount.IsFailure)
        {
            _logger.LogError("Unable to retrieve applicant user account of id {applicantId}, error: {error}", application.CreatedById, userAccount.Error);
            return Result.Failure<PDFGeneratorRequest>(userAccount.Error);
        }

        var woodlandOwner = await _woodlandOwnerService.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, cancellationToken);
        if (woodlandOwner.IsFailure)
        {
            _logger.LogError("Unable to retrieve woodland owner of id {woodlandOwnerId}, error: {error}", application.WoodlandOwnerId, woodlandOwner.Error);
            return Result.Failure<PDFGeneratorRequest>(woodlandOwner.Error);
        }

        var approverAccount = await _internalAccountService.GetUserAccountAsync(application.ApproverId ?? Guid.Empty, cancellationToken);
        if (application.ApproverId.HasValue && approverAccount.HasNoValue)
        {
            _logger.LogError("Unable to retrieve approver user account of id {approverId}", application.ApproverId);
        }

        var propertyProfile = await _propertyProfileRepository.GetAsync(application.LinkedPropertyProfile!.PropertyProfileId, application.WoodlandOwnerId, cancellationToken);
        if (propertyProfile.IsFailure)
        {
            _logger.LogError("Unable to retrieve propertyProfile of id {propertyId}, error: {error}", application.LinkedPropertyProfile!.PropertyProfileId, propertyProfile.Error);
            return Result.Failure<PDFGeneratorRequest>($"Unable to retrieve propertyProfile of id {application.LinkedPropertyProfile!.PropertyProfileId}, error: {propertyProfile.Error}");
        }

        // Approved conditions
        var watermarked = "true";
        var approverDate = "To be confirmed";
        if (application.StatusHistories.MaxBy(x => x.Created)!.Status == FellingLicenceStatus.SentForApproval && approved)
        {
            watermarked = "false";
            approverDate = _clock.GetCurrentInstant().ToDateTimeUtc().Date.ToString("dd/MM/yyyy");
        }

        // part 0 - Application General details
        var approverName = "To be confirmed";
        if (approverAccount.HasValue)
        {
            approverName = approverAccount.Value.FullName(false);
        }

        // Admin hub contact info
        var fcContactAddress = await _getConfiguredFcAreasService.TryGetAdminHubAddress(application.AdministrativeRegion!, cancellationToken);
        var fcContactName = $"{application.AdministrativeRegion} Hub";

        var ownerAddress = new List<string>{
            woodlandOwner.Value.ContactAddress?.Line1 ?? string.Empty,
            woodlandOwner.Value.ContactAddress?.Line2 ?? string.Empty,
            woodlandOwner.Value.ContactAddress?.Line3 ?? string.Empty,
            woodlandOwner.Value.ContactAddress?.Line4 ?? string.Empty,
            woodlandOwner.Value.ContactAddress?.PostalCode ?? string.Empty
        };
        var ownerAddressFormatted = string.Join("\n", ownerAddress.Where(x => !string.IsNullOrEmpty(x)));

        // Expiry Date

        RecommendedLicenceDuration? licenseDuration = null;
        var approverReview = await _approverReviewService.GetApproverReviewAsync(application.Id, cancellationToken);
        if (approverReview.HasNoValue)
        {
            _logger.LogWarning("Could not retrieve confirmed licence duration on Approver Review for application with id {ApplicationId}", application.Id);
            var woodlandOfficerReview = await _getWoodlandOfficerReviewService.GetWoodlandOfficerReviewStatusAsync(
                application.Id, cancellationToken);
            
            if (woodlandOfficerReview.IsFailure)
            {
                _logger.LogError("Could not retrieve recommended licence duration on WO Review for application with id {ApplicationId}, error {Error}", application.Id, woodlandOfficerReview.Error);
            }
            else
            {
                licenseDuration = woodlandOfficerReview.Value.RecommendedLicenceDuration;
            }
        }
        else
        {
            licenseDuration = approverReview.Value.ApprovedLicenceDuration;
        }
        
        var expiryDate = licenseDuration.GetExpiryDateForRecommendedDuration(_clock);

        // Part 1 - Description of the trees to be felled
        // Part 2 - restocking Conditions & Set list of restocking and felling compartments for maps
        var restockingConditions = new List<string>();
        HashSet<Guid> restockingCompartments = [];
        HashSet<Guid> fellingCompartments = [];

        // For retrieving species names
        var speciesDictionary = TreeSpeciesFactory.SpeciesDictionary;

        var fellingDetails = new List<PDFGeneratorFellingDetails>();
        if (application.WoodlandOfficerReview is { ConfirmedFellingAndRestockingComplete: true })
        {
            // generate from confirmed felling details

            if (application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Select(x => x.ConfirmedFellingDetails).Any(x => x != null && x.Any()))
            {
                fellingDetails = application.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments!.SelectMany(x => x.ConfirmedFellingDetails).Select(felling =>
                        new PDFGeneratorFellingDetails
                        {
                            fellingSiteSubcompartment = $"{felling.SubmittedFlaPropertyCompartment!.CompartmentNumber} - {felling.SubmittedFlaPropertyCompartment.SubCompartmentName}",
                            typeOfOperation = felling!.OperationType.GetDisplayName(),
                            markingOfTrees = felling.TreeMarking?.ToString(),
                            digitisedArea = felling.AreaToBeFelled.ToString(CultureInfo.InvariantCulture),
                            totalNumberOfTrees = felling.NumberOfTrees.ToString(),
                            estimatedVolume = felling.EstimatedTotalFellingVolume.ToString(),
                            species = string.Join(" / ", felling.ConfirmedFellingSpecies!.Select(x =>
                                speciesDictionary.TryGetValue(x.Species, out var speciesModel) ? speciesModel.Name : x.Species))
                        })
                    .ToList();
            }

            foreach (var confirmedFelling in application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
                         .SelectMany(x => x.ConfirmedFellingDetails))
            {
                fellingCompartments.Add(confirmedFelling.SubmittedFlaPropertyCompartmentId);

                foreach (var restocking in confirmedFelling.ConfirmedRestockingDetails)
                {
                    restockingCompartments.Add(restocking.SubmittedFlaPropertyCompartmentId);
                }
            }

            // restocking conditions
            var conditionsStatus = await _getWoodlandOfficerReviewService.GetConditionsStatusAsync(application.Id, cancellationToken);
            if (conditionsStatus.IsFailure)
            {
                _logger.LogError("Could not retrieve conditional status for application with id {ApplicationId}, error {Error}", application.Id, conditionsStatus.Error);
                restockingConditions.Add("To be confirmed\n");
            }
            else
            {
                var restockingConditionsRetrieved = await _calculateConditionsService.RetrieveExistingConditionsAsync(application.Id,
                    cancellationToken);

                foreach (var condition in restockingConditionsRetrieved.Conditions)
                {
                    restockingConditions.Add(string.Join("\n", condition.ToFormattedArray()));
                }
            }
        }
        else
        {
            // generate from proposed felling details

            if (application.LinkedPropertyProfile.ProposedFellingDetails is not null && application.LinkedPropertyProfile.ProposedFellingDetails.Any())
            {
                fellingDetails = application.LinkedPropertyProfile.ProposedFellingDetails.Select(felling =>
                    {
                        var submittedCompartment = application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
                            .First(x => x.CompartmentId == felling.PropertyProfileCompartmentId);

                        return new PDFGeneratorFellingDetails
                        {
                            fellingSiteSubcompartment = $"{submittedCompartment.CompartmentNumber} - {submittedCompartment.SubCompartmentName}",
                            typeOfOperation = felling.OperationType.GetDisplayName(),
                            markingOfTrees = felling.TreeMarking?.ToString(),
                            digitisedArea = felling.AreaToBeFelled.ToString(CultureInfo.InvariantCulture),
                            totalNumberOfTrees = felling.NumberOfTrees.ToString(),
                            estimatedVolume = felling.EstimatedTotalFellingVolume.ToString(),
                            species = string.Join(" / ", felling.FellingSpecies!.Select(x =>
                                speciesDictionary.TryGetValue(x.Species, out var speciesModel)
                                    ? speciesModel.Name
                                    : x.Species))
                        };
                    })
                    .ToList();
            }

            foreach (var proposedFelling in application.LinkedPropertyProfile!.ProposedFellingDetails!)
            {
                var submittedCompartmentId = application
                    .SubmittedFlaPropertyDetail!
                    .SubmittedFlaPropertyCompartments!
                    .FirstOrDefault(x => x.CompartmentId == proposedFelling.PropertyProfileCompartmentId)?.Id;
                if (submittedCompartmentId.HasValue)
                {
                    fellingCompartments.Add(submittedCompartmentId.Value);
                }

                foreach (var restocking in proposedFelling.ProposedRestockingDetails!)
                {
                    var restockingSubmittedCompartment = application
                        .SubmittedFlaPropertyDetail!
                        .SubmittedFlaPropertyCompartments!
                        .FirstOrDefault(x => x.CompartmentId == restocking.PropertyProfileCompartmentId)?.Id;

                    if (restockingSubmittedCompartment.HasValue)
                    {
                        restockingCompartments.Add(restockingSubmittedCompartment.Value);
                    }
                }
            }

            restockingConditions.Add("To be confirmed\n");
        }

        // Part 3 - Supplementary point, *** this will need an update to match https://quicksilva.atlassian.net/browse/FLOV2-919
        string supplementPoints;

        if (licenseDuration == RecommendedLicenceDuration.TenYear)
        {
            var managementPlanText = propertyProfile.Value.WoodlandManagementPlanReference == null ? string.Empty : "Management Plan referenced ";
            supplementPoints =
                $"This license is issued in summary relating to the {managementPlanText}{propertyProfile.Value.WoodlandManagementPlanReference ?? ""} {propertyProfile.Value.Name} and associated {propertyProfile.Value.WoodlandManagementPlanReference ?? ""} {propertyProfile.Value.Name} Final Plan of Ops and {propertyProfile.Value.WoodlandManagementPlanReference ?? ""} {propertyProfile.Value.Name} Final Maps. Full details of the felling and restocking conditions agreed under this licence can be found in the above mentioned Plan of Operations and maps that must be attached to this licence at all times.";
        }
        else
        {
            supplementPoints = "To be confirmed\n";
        }
        // Part 4 - Felling maps ******

        _logger.LogDebug($"Generating Felling maps for application with id {application.Id}");

        var operationsMaps = new List<string>();
        if (fellingCompartments.Count > 0) {
            var fellingCompartmentMap = 
                application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
                    .Where(x => fellingCompartments.Contains(x.Id)).Select(compartment => new InternalCompartmentDetails<BaseShape> { 
                        CompartmentNumber = compartment.CompartmentNumber,
                        CompartmentLabel = compartment.CompartmentNumber,
                        ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(compartment.GISData!)!,
                        SubCompartmentNo = compartment.SubCompartmentName!
                    }).ToList();


            var generatedMainFellingMap = await _iForesterServices.GenerateImage_MultipleCompartmentsAsync(fellingCompartmentMap, cancellationToken, 3000);
            if (generatedMainFellingMap.IsFailure) {
                _logger.LogError("Unable to retrieve Generated Map of application with id {applicantId}, error: {error}", application.WoodlandOwnerId, generatedMainFellingMap.Error);
                return Result.Failure<PDFGeneratorRequest>("Failed to Generate Felling Map");
            }

            if (generatedMainFellingMap.Value.CanRead) {
                var readyMainFellingMap = Convert.ToBase64String(generatedMainFellingMap.Value.ConvertStreamToBytes());
                operationsMaps.Add(readyMainFellingMap);
            }
        }

        // Part 5 - Restocking maps ******

        _logger.LogDebug($"Generating Restocking maps for application with id {application.Id}");

 
        var restockingMaps = new List<string>();
        if (restockingCompartments.Count > 0) {
            var restockingCompartmentsMaps =
                application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
                    .Where(x => restockingCompartments.Contains(x.Id)).Select(compartment => new InternalCompartmentDetails<BaseShape>
                    {
                        CompartmentNumber = compartment.CompartmentNumber,
                        CompartmentLabel = compartment.CompartmentNumber,
                        ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(compartment.GISData!)!,
                        SubCompartmentNo = compartment.SubCompartmentName!
                    }).ToList();

            var generatedMainRestockingMap = await _iForesterServices.GenerateImage_MultipleCompartmentsAsync(restockingCompartmentsMaps, cancellationToken, 3000);
            if (generatedMainRestockingMap.IsFailure) {
                _logger.LogError("Unable to retrieve Generated Map of application with id {applicantId}, error: {error}", application.WoodlandOwnerId, generatedMainRestockingMap.Error);
                return Result.Failure<PDFGeneratorRequest>("Failed to Generate Restocking Map");
            }

            if (generatedMainRestockingMap.Value.CanRead) {
                var readyMainRestockingMap =
                    Convert.ToBase64String(generatedMainRestockingMap.Value.ConvertStreamToBytes());
                restockingMaps.Add(readyMainRestockingMap);
            }
        }

        _logger.LogDebug($"Creating Request for application with Id {application.Id}");
        var result = new PDFGeneratorRequest
        {
            templateName = _licencePdfOptions.TemplateName,
            watermarked = watermarked,
            data = new PDFGeneratorData
            {
                version = _licencePdfOptions.Version,
                date = _clock.GetCurrentInstant().ToDateTimeUtc().ToString("dd/MM/yyyy"),
                fcContactName = fcContactName,
                fcContactAddress = fcContactAddress,
                approveDate = approverDate,
                applicationRef = application.ApplicationReference,
                managementPlan = propertyProfile.Value.WoodlandManagementPlanReference ?? "",
                woodName = propertyProfile.Value.Name,
                ownerNameWithTitle = woodlandOwner.Value.ContactName,
                ownerAddress = ownerAddressFormatted,
                expiryDate = expiryDate,
                approverName = approverName,
                propertyName = propertyProfile.Value.Name,
                localAuthority = propertyProfile.Value.NearestTown,
                approvedFellingDetails = fellingDetails.OrderBy(x => x.fellingSiteSubcompartment).ToList(),
                restockingConditions = restockingConditions,
                operationsMaps = operationsMaps,
                restockingAdvisoryDetails = supplementPoints,
                restockingMaps = restockingMaps
            }
        };

        result.MakeSafeForPdfService();
        return result;
    }
}