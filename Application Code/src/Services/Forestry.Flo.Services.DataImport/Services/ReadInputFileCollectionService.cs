using CSharpFunctionalExtensions;
using CsvHelper;
using CsvHelper.TypeConversion;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Forestry.Flo.Services.DataImport.Services;

/// <summary>
/// Implementation of <see cref="IReadImportFileCollections"/>.
/// </summary>
public class ReadInputFileCollectionService : IReadImportFileCollections
{
    private readonly IVirusScannerService _virusScanner;
    private readonly ILogger<ReadInputFileCollectionService> _logger;

    private readonly TypeConverterOptions _britishDateConverterOptions = new() { Formats = ["dd/MM/yyyy"] };

    /// <summary>
    /// Creates a new instance of <see cref="ReadInputFileCollectionService"/> with the specified dependencies.
    /// </summary>
    /// <param name="virusScanner"></param>
    /// <param name="logger"></param>
    public ReadInputFileCollectionService(
        IVirusScannerService virusScanner,
        ILogger<ReadInputFileCollectionService> logger)
    {
        _virusScanner = virusScanner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ImportFileSetContents, List<string>>> ReadInputFormFileCollectionAsync(FormFileCollection files, CancellationToken cancellationToken)
    {
        List<ApplicationSource> applications = [];
        List<ProposedFellingSource> fellingDetails = [];
        List<ProposedRestockingSource> restockingDetails = [];
        List<string> errors = [];

        foreach (var file in files)
        {
            var scanResult = await ScanFileContentAsync(file, cancellationToken);
            if (scanResult.IsFailure)
            {
                errors.Add(scanResult.Error);
                continue;
            }

            var name = file.FileName;

            if (TryReadCsv<ApplicationSource>(file, out var sourceAsApplications))
            {
                applications.AddRange(sourceAsApplications!);
                continue;
            }

            if (TryReadCsv<ProposedFellingSource>(file, out var sourceAsFelling))
            {
                fellingDetails.AddRange(sourceAsFelling!);
                continue;
            }

            if (TryReadCsv<ProposedRestockingSource>(file, out var sourceAsRestocking))
            {
                restockingDetails.AddRange(sourceAsRestocking!);
                continue;
            }

            errors.Add($"Failed to read file {name}. Ensure it is a valid CSV file with the correct headers.");
        }

        if (applications.NotAny())
        {
            _logger.LogError("No applications provided in the import source files");
            errors.Add("No applications provided in the import source files.");
            return Result.Failure<ImportFileSetContents, List<string>>(errors);
        }

        if (errors.Any())
        {
            _logger.LogError("At least one error was found with the provided files.");
            return Result.Failure<ImportFileSetContents, List<string>>(errors);
        }

        return Result.Success<ImportFileSetContents, List<string>>(new ImportFileSetContents
        {
            ApplicationSourceRecords = applications,
            ProposedFellingSourceRecords = fellingDetails,
            ProposedRestockingSourceRecords = restockingDetails
        });
    }

    private bool TryReadCsv<T>(IFormFile file, out List<T>? result)
    {
        try
        {
            result = [];
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.CurrentUICulture);
            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(_britishDateConverterOptions);
            result.AddRange(csv.GetRecords<T>());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught reading file {FileName}", file.FileName);
            result = null;
            return false;
        }
    }

    private async Task<Result> ScanFileContentAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);

        var bytes = ms.ToArray();
        var scanResult = await _virusScanner.ScanAsync(file.FileName, bytes, cancellationToken);

        return scanResult is not AntiVirusScanResult.VirusFree and not AntiVirusScanResult.DisabledInConfiguration
            ? Result.Failure($"Source file {file.FileName} failed virus scanning with result {scanResult}")
            : Result.Success();
    }
}