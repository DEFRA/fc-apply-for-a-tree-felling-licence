using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Services.DataImport.Services;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Use case class for the Import a felling licence application functionality.
/// </summary>
public class DataImportUseCase
{
    private readonly IImportData _dataImport;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly ILogger<DataImportUseCase> _logger;

    public static readonly string ParsedDataKey = "ImportFileSetContents";

    /// <summary>
    /// Create a new instance of the <see cref="DataImportUseCase"/> class.
    /// </summary>
    /// <param name="dataImport">An implementation of <see cref="IImportData"/> to perform the import.</param>
    /// <param name="retrieveUserAccountsService">A <see cref="IRetrieveUserAccountsService"/> to retrieve the user's access.</param>
    /// <param name="logger">A logging implementation.</param>
    public DataImportUseCase(
        IImportData dataImport, 
        IRetrieveUserAccountsService retrieveUserAccountsService,
        ILogger<DataImportUseCase> logger)
    {
        _dataImport = Guard.Against.Null(dataImport);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _logger = logger;
    }

    /// <summary>
    /// Parses the import data files provided, ensuring they meet the structural and business rules for a
    /// data import file set.
    /// </summary>
    /// <param name="user">The user performing the import.</param>
    /// <param name="woodlandOwnerId">The ID of the woodland owner the data is importing for.</param>
    /// <param name="importFiles">The set of files to parse.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct, along with either a <see cref="ImportFileSetContents"/> model
    /// of the parsed files if successful or a list of error/validation messages if unsuccessful.</returns>
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

    /// <summary>
    /// Performs the import of data provided in a populated <see cref="ImportFileSetContents"/> instance.
    /// </summary>
    /// <param name="user">The user performing the data import.</param>
    /// <param name="woodlandOwnerId">The ID of the woodland owner the data is importing for.</param>
    /// <param name="parsedData">The <see cref="ImportFileSetContents"/> model of the data to be imported.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct, along with a dictionary of imported application IDs and references
    /// if successful, or a list of error/validation messages if unsuccessful.</returns>
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