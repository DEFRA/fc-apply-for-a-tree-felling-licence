using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Services.DataImport.Services;

namespace Forestry.Flo.External.Web.Services;

public class DataImportUseCase
{
    private readonly IImportData _dataImport;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly ILogger<DataImportUseCase> _logger;

    public DataImportUseCase(
        IImportData dataImport, 
        IRetrieveUserAccountsService retrieveUserAccountsService,
        ILogger<DataImportUseCase> logger)
    {
        _dataImport = Guard.Against.Null(dataImport);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _logger = logger;
    }

    public static readonly string ParsedDataKey = "ImportFileSetContents";

    public async Task<Result<ImportFileSetContents, List<string>>> ParseImportDataFilesAsync(
        ExternalApplicant user,
        Guid woodlandOwnerId,
        FormFileCollection importFiles,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request to perform data import for files: {Files}", string.Join(", ", importFiles.Select(x => x.FileName)));

        var uam = await _retrieveUserAccountsService.RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken);
        if (uam.IsFailure)
        {
            _logger.LogError("Failed to retrieve user access for user {UserId}: {Error}", user.UserAccountId, uam.Error);
            return Result.Failure<ImportFileSetContents, List<string>>(["Failed to retrieve user access"]);
        }

        var request = new DataImportRequest
        {
            UserAccessModel = uam.Value,
            WoodlandOwnerId = woodlandOwnerId,
            ImportFileSet = importFiles
        };
        
        var result = await _dataImport.ParseDataImportRequestAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Data import failed with errors: {Errors}", string.Join(", ", result.Error));
        }

        return result;
    }

    public async Task<Result<Dictionary<Guid, string>, List<string>>> ImportDataAsync(
        ExternalApplicant user,
        Guid woodlandOwnerId,
        ImportFileSetContents parsedData,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request to perform data import for parsed data containing {ApplicationCount} applications", parsedData.ApplicationSourceRecords?.Count);
        var uam = await _retrieveUserAccountsService.RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken);
        if (uam.IsFailure)
        {
            _logger.LogError("Failed to retrieve user access for user {UserId}: {Error}", user.UserAccountId, uam.Error);
            return Result.Failure<Dictionary<Guid, string>, List<string>>(["Failed to retrieve user access"]);
        }

        var result = await _dataImport.ImportDataAsync(uam.Value, woodlandOwnerId, parsedData, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError("Data import failed with errors: {Errors}", string.Join(", ", result.Error));
        }

        return result;
    }
}