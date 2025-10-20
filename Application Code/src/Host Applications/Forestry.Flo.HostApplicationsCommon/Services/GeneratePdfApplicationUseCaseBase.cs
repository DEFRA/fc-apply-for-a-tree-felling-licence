using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NodaTime;
using System.Globalization;

namespace Forestry.Flo.HostApplicationsCommon.Services;

/// <summary>
/// Handles the use case for an external user generating a pdf of the felling licence application and attaching it to the application.
/// </summary>
public abstract class GeneratePdfApplicationUseCaseBase
{
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IRetrieveUserAccountsService _externalAccountService;
    private readonly IUserAccountService _internalAccountService;
    private readonly IRetrieveWoodlandOwners _woodlandOwnerService;
    private readonly IPropertyProfileRepository _propertyProfileRepository;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService;
    private readonly ILogger<GeneratePdfApplicationUseCaseBase> _logger;
    private readonly IForesterServices _iForesterServices;
    private readonly IClock _clock;
    private readonly PDFGeneratorAPIOptions _licencePdfOptions;
    private readonly IApproverReviewService _approverReviewService;
    private readonly ICalculateConditions _calculateConditionsService;

    protected GeneratePdfApplicationUseCaseBase(
        IAuditService<GeneratePdfApplicationUseCaseBase> auditService,
        RequestContext requestContext,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IUserAccountService internalAccountService,
        IRetrieveUserAccountsService externalAccountService,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IPropertyProfileRepository propertyProfileRepository,
        IForesterServices iForesterServices,
        IGetConfiguredFcAreas getConfiguredFcAreasService, 
        IClock clock,
        IOptions<PDFGeneratorAPIOptions> licencePdfOptions,
        ICalculateConditions calculateConditionsService,
        IApproverReviewService approverReviewService,
        ILogger<GeneratePdfApplicationUseCaseBase> logger)
    {
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(getWoodlandOfficerReviewService);
        ArgumentNullException.ThrowIfNull(internalAccountService);
        ArgumentNullException.ThrowIfNull(externalAccountService);
        ArgumentNullException.ThrowIfNull(woodlandOwnerService);
        ArgumentNullException.ThrowIfNull(propertyProfileRepository);
        ArgumentNullException.ThrowIfNull(iForesterServices);
        ArgumentNullException.ThrowIfNull(getConfiguredFcAreasService);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(licencePdfOptions);
        ArgumentNullException.ThrowIfNull(calculateConditionsService);
        ArgumentNullException.ThrowIfNull(approverReviewService);
        ArgumentNullException.ThrowIfNull(logger);

        _getWoodlandOfficerReviewService = getWoodlandOfficerReviewService;
        _internalAccountService = internalAccountService;
        _externalAccountService = externalAccountService;
        _woodlandOwnerService = woodlandOwnerService;
        _propertyProfileRepository = propertyProfileRepository;
        _iForesterServices = iForesterServices;
        _clock = clock;
        _licencePdfOptions = licencePdfOptions.Value;
        _getConfiguredFcAreasService = getConfiguredFcAreasService;
        _logger = logger;
        _approverReviewService = approverReviewService;
        _calculateConditionsService = calculateConditionsService;
    }

    /// <summary>
    /// Creates a <see cref="PDFGeneratorRequest"/> to send to the PDF generator to generate a felling licence document for the supplied application.
    /// </summary>
    /// <param name="application">The relevant application to create the PDF for. </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns a <see cref="PDFGeneratorRequest"/> for use in generating a pdf of the application.</returns>
    protected async Task<Result<PDFGeneratorRequest>> CreateRequestModelAsync(
        FellingLicenceApplication application,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating a PDF Generator Request for the application with id {applicationId}", application.Id);
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
        if (application.GetCurrentStatus() is FellingLicenceStatus.Approved)
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

            if (application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Select(x => x.ConfirmedFellingDetails).Any())
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

            if (application.WoodlandOfficerReview.IsConditional is true)
            {
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
        if (fellingCompartments.Count > 0)
        {
            var fellingCompartmentMap =
                application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
                    .Where(x => fellingCompartments.Contains(x.Id)).Select(compartment => new InternalCompartmentDetails<BaseShape>
                    {
                        CompartmentNumber = compartment.CompartmentNumber,
                        CompartmentLabel = compartment.CompartmentNumber,
                        ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(compartment.GISData!)!,
                        SubCompartmentNo = compartment.SubCompartmentName!
                    }).ToList();


            var generatedMainFellingMap = await _iForesterServices.GenerateImage_MultipleCompartmentsAsync(fellingCompartmentMap, cancellationToken, 3000);
            if (generatedMainFellingMap.IsFailure)
            {
                _logger.LogError("Unable to retrieve Generated Map of application with id {applicantId}, error: {error}", application.WoodlandOwnerId, generatedMainFellingMap.Error);
                return Result.Failure<PDFGeneratorRequest>("Failed to Generate Felling Map");
            }

            if (generatedMainFellingMap.Value.CanRead)
            {
                var readyMainFellingMap = Convert.ToBase64String(generatedMainFellingMap.Value.ConvertStreamToBytes());
                operationsMaps.Add(readyMainFellingMap);
            }
        }

        // Part 5 - Restocking maps ******

        _logger.LogDebug($"Generating Restocking maps for application with id {application.Id}");


        var restockingMaps = new List<string>();
        if (restockingCompartments.Count > 0)
        {
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
            if (generatedMainRestockingMap.IsFailure)
            {
                _logger.LogError("Unable to retrieve Generated Map of application with id {applicantId}, error: {error}", application.WoodlandOwnerId, generatedMainRestockingMap.Error);
                return Result.Failure<PDFGeneratorRequest>("Failed to Generate Restocking Map");
            }

            if (generatedMainRestockingMap.Value.CanRead)
            {
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