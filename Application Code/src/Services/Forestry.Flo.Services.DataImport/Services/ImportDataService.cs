using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.DataImport.Services;

/// <summary>
/// Implementation of <see cref="IImportData"/>
/// </summary>
public class ImportDataService : IImportData
{
    private readonly IImportApplications _applicationsImport;
    private readonly IValidateImportFileSets _importFileSetValidator;
    private readonly IReadImportFileCollections _importFilesReader;
    private readonly IAuditService<ImportDataService> _auditService;
    private readonly RequestContext _context;
    private readonly IGetPropertiesForWoodlandOwner _getPropertiesForWoodlandOwner;
    private readonly ILogger<ImportDataService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="ImportDataService"/>.
    /// </summary>
    /// <param name="importFilesReader">An <see cref="IReadImportFileCollections"/> import file reader service.</param>
    /// <param name="importFileSetValidator">An <see cref="IValidateImportFileSets"/> import set validator service.</param>
    /// <param name="getPropertiesForWoodlandOwner">An <see cref="IGetPropertiesForWoodlandOwner"/> properties retrieval service.</param>
    /// <param name="applicationsImport">An <see cref="IImportApplications"/> applications importing service.</param>
    /// <param name="auditService">An <see cref="IAuditService{T}"/>> auditing service.</param>
    /// <param name="context">The request context.</param>
    /// <param name="logger">A logging instance.</param>
    public ImportDataService(
        IReadImportFileCollections importFilesReader,
        IValidateImportFileSets importFileSetValidator,
        IGetPropertiesForWoodlandOwner getPropertiesForWoodlandOwner,
        IImportApplications applicationsImport, 
        IAuditService<ImportDataService> auditService,
        RequestContext context,
        ILogger<ImportDataService> logger)
    {
        _applicationsImport = Guard.Against.Null(applicationsImport);
        _importFileSetValidator = Guard.Against.Null(importFileSetValidator);
        _importFilesReader = Guard.Against.Null(importFilesReader);
        _auditService = Guard.Against.Null(auditService);
        _context = Guard.Against.Null(context);
        _getPropertiesForWoodlandOwner = getPropertiesForWoodlandOwner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ImportFileSetContents, List<string>>> ParseDataImportRequestAsync(
        DataImportRequest request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        _logger.LogDebug("Attempting to perform data parse for file collection containing {FileCount} files", request.ImportFileSet.Count);

        var importFilesResult = await _importFilesReader.ReadInputFormFileCollectionAsync(request.ImportFileSet, cancellationToken);

        if (importFilesResult.IsFailure)
        {
            _logger.LogError("Could not read provided import file collection: {Error}", importFilesResult.Error);
            await PublishFailureEventAsync(
                request.WoodlandOwnerId,
                request.UserAccessModel.UserAccountId,
                null,
                importFilesResult.Error,
                "Failed to read provided import file collection",
                cancellationToken);
            return Result.Failure<ImportFileSetContents, List<string>>(importFilesResult.Error);
        }

        _logger.LogDebug("Attempting to retrieve properties for woodland owner {WoodlandOwnerId}", request.WoodlandOwnerId);

        var propertiesResult = await _getPropertiesForWoodlandOwner.GetPropertiesForDataImport(
            request.UserAccessModel,
            request.WoodlandOwnerId,
            cancellationToken);

        if (propertiesResult.IsFailure)
        {
            _logger.LogError("Failed to retrieve properties for woodland owner {WoodlandOwnerId}", request.WoodlandOwnerId);
            await PublishFailureEventAsync(
                request.WoodlandOwnerId,
                request.UserAccessModel.UserAccountId,
                null,
                [propertiesResult.Error],
                "Failed to retrieve properties for woodland owner",
                cancellationToken);
            return Result.Failure<ImportFileSetContents, List<string>>(["Failed to retrieve properties for woodland owner"]);
        }

        var numberApplicationsInImportFile = importFilesResult.Value.ApplicationSourceRecords?.Count ?? 0;

        _logger.LogDebug("Attempting to validate the data in the provided import file collection");

        var validationResult = await _importFileSetValidator.ValidateImportFileSetAsync(
            request.WoodlandOwnerId,
            propertiesResult.Value,
            importFilesResult.Value,
            cancellationToken);

        if (validationResult.IsFailure)
        {
            _logger.LogError("Validation failed for provided import file collection");
            var messages = validationResult.Error.Select(x => x.ErrorMessage).ToList();
            await PublishFailureEventAsync(
                request.WoodlandOwnerId,
                request.UserAccessModel.UserAccountId,
                numberApplicationsInImportFile,
                messages,
                "Validation failed for provided import file collection",
                cancellationToken);
            return Result.Failure<ImportFileSetContents, List<string>>(messages);
        }

        _logger.LogDebug("Validation succeeded for provided import file collection, returning parsed data");
        return Result.Success<ImportFileSetContents, List<string>>(importFilesResult.Value);
    }

    /// <inheritdoc />
    public async Task<Result<Dictionary<Guid, string>, List<string>>> ImportDataAsync(
        UserAccessModel userAccessModel,
        Guid woodlandOwnerId,
        ImportFileSetContents parsedData, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(parsedData);
        _logger.LogDebug("Attempting to perform data import for import file set containing {ApplicationCount} applications", parsedData.ApplicationSourceRecords.Count);

        _logger.LogDebug("Attempting to retrieve properties for woodland owner {WoodlandOwnerId}", woodlandOwnerId);

        var propertiesResult = await _getPropertiesForWoodlandOwner.GetPropertiesForDataImport(
            userAccessModel,
            woodlandOwnerId,
            cancellationToken);

        if (propertiesResult.IsFailure)
        {
            _logger.LogError("Failed to retrieve properties for woodland owner {WoodlandOwnerId}", woodlandOwnerId);
            await PublishFailureEventAsync(
                woodlandOwnerId,
                userAccessModel.UserAccountId,
                null,
                [propertiesResult.Error],
                "Failed to retrieve properties for woodland owner",
                cancellationToken);
            return Result.Failure<Dictionary<Guid, string>, List<string>>(["Failed to retrieve properties for woodland owner"]);
        }

        var numberApplicationsInImportFile = parsedData.ApplicationSourceRecords?.Count ?? 0;

        _logger.LogDebug("Attempting to revalidate the data in the provided parsed data");

        var validationResult = await _importFileSetValidator.ValidateImportFileSetAsync(
            woodlandOwnerId,
            propertiesResult.Value,
            parsedData,
            cancellationToken);

        if (validationResult.IsFailure)
        {
            _logger.LogError("Validation failed for provided parsed data");
            var messages = validationResult.Error.Select(x => x.ErrorMessage).ToList();
            await PublishFailureEventAsync(
                woodlandOwnerId,
                userAccessModel.UserAccountId,
                numberApplicationsInImportFile,
                messages,
                "Validation failed for provided import file collection",
                cancellationToken);
            return Result.Failure<Dictionary<Guid, string>, List<string>>(messages);
        }

        var applicationsRequest = new ImportApplicationsRequest
        {
            UserId = userAccessModel.UserAccountId,
            WoodlandOwnerId = woodlandOwnerId,
            ApplicationRecords = parsedData.ApplicationSourceRecords ?? [],
            FellingRecords = parsedData.ProposedFellingSourceRecords ?? [],
            RestockingRecords = parsedData.ProposedRestockingSourceRecords ?? [],
            PropertyIds = propertiesResult.Value
        };

        _logger.LogDebug("Validation succeeded, attempting to import the data in the import file collection");

        var importApplicationsResult = await _applicationsImport.RunDataImportAsync(applicationsRequest, _context, cancellationToken);

        if (importApplicationsResult.IsFailure)
        {
            _logger.LogError("Failed to import applications provided: " + importApplicationsResult.Error);
            await PublishFailureEventAsync(
                woodlandOwnerId,
                userAccessModel.UserAccountId,
                numberApplicationsInImportFile,
                null,
                "Failed to import felling licence applications provided: " + importApplicationsResult.Error,
                cancellationToken);

            return Result.Failure<Dictionary<Guid, string>, List<string>>([importApplicationsResult.Error]);
        }

        var applicationsImported = importApplicationsResult.Value;

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ImportDataFromCsv,
                woodlandOwnerId,
                userAccessModel.UserAccountId,
                _context,
                new
                {
                    FellingLicenceApplicationsInImportFile = numberApplicationsInImportFile,
                    FellingLicenceApplicationsImported = applicationsImported
                }),
            cancellationToken);

        return Result.Success<Dictionary<Guid, string>, List<string>>(importApplicationsResult.Value);
    }

    private async Task PublishFailureEventAsync(
        Guid woodlandOwnerId, 
        Guid userId, 
        int? applicationsInImportFile, 
        List<string>? validationErrors, 
        string error, 
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ImportDataFromCsvFailure,
                woodlandOwnerId,
                userId,
                _context,
                new
                {
                    FellingLicenceApplicationsInImportFile = applicationsInImportFile,
                    ValidationErrors = validationErrors,
                    Error = error
                }),
            cancellationToken);
    }
}