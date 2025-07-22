using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using FluentValidation.Results;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Services.DataImport.Validators;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.DataImport.Services;

public class ValidateImportFileSetsService: IValidateImportFileSets
{
    private readonly IClock _clock;
    private readonly ILogger<ValidateImportFileSetsService> _logger;
    private readonly List<string> _speciesCodes;

    public ValidateImportFileSetsService(
        IClock clock,
        ILogger<ValidateImportFileSetsService> logger)
    {
        _clock = Guard.Against.Null(clock);
        _logger = logger;
        _speciesCodes = TreeSpeciesFactory.SpeciesDictionary.Select(x => x.Value.Code).ToList();
    }

    /// <inheritdoc />
    public async Task<UnitResult<List<ValidationFailure>>> ValidateImportFileSetAsync(
        Guid woodlandOwnerId,
        IEnumerable<PropertyIds> properties,
        ImportFileSetContents importFileSet,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant().ToDateTimeUtc();
        var results = new List<ValidationFailure>();
        var propertiesList = properties.ToList();

        _logger.LogDebug("Validation running against set of import files data");

        _logger.LogDebug("Running application data validation");
        var applicationsValidationResult = await new ApplicationSourcesValidator()
            .ValidateAsync(importFileSet.ApplicationSourceRecords!, cancellationToken);
        ProcessValidationResult(results, applicationsValidationResult, "Applications");

        foreach (var application in importFileSet.ApplicationSourceRecords!)
        {
            var applicationValidationResult = await new ApplicationSourceValidator(propertiesList, now)
                .ValidateAsync(application, cancellationToken);
            ProcessValidationResult(results, applicationValidationResult, "Application");
        }

        _logger.LogDebug("Running felling data validation");

        var proposedFellingRecords = importFileSet.ProposedFellingSourceRecords ?? [];
        var proposedFellingRecordsValidationResult = await new ProposedFellingSourcesValidator()
                .ValidateAsync(proposedFellingRecords, cancellationToken);
        ProcessValidationResult(results, proposedFellingRecordsValidationResult, "Fellings");

        foreach (var proposedFelling in proposedFellingRecords)
        {
            var application = importFileSet.ApplicationSourceRecords!
                .FirstOrDefault(x => x.ApplicationId == proposedFelling.ApplicationId);
            var property = propertiesList
                .FirstOrDefault(x => x.Name.Equals(application?.Flov2PropertyName, StringComparison.InvariantCultureIgnoreCase));
            var restockingForThisFelling = importFileSet.ProposedRestockingSourceRecords?
                .Where(x => x.ProposedFellingId == proposedFelling.ProposedFellingId)
                .ToList() ?? [];

            var proposedFellingValidationResult = await new ProposedFellingSourceValidator(application, property, restockingForThisFelling, _speciesCodes)
                .ValidateAsync(proposedFelling, cancellationToken);
            ProcessValidationResult(results, proposedFellingValidationResult, "Felling");
        }

        _logger.LogDebug("Running restocking data validation");

        var proposedRestockingRecords = importFileSet.ProposedRestockingSourceRecords ?? [];
        var proposedRestockingRecordsValidationResult = await new ProposedRestockingSourcesValidator()
            .ValidateAsync(proposedRestockingRecords, cancellationToken);
        ProcessValidationResult(results, proposedRestockingRecordsValidationResult, "Restockings");

        foreach (var proposedRestocking in proposedRestockingRecords)
        {
            var linkedProposedFelling = proposedFellingRecords
                .FirstOrDefault(x => x.ProposedFellingId == proposedRestocking.ProposedFellingId);

            var application = importFileSet.ApplicationSourceRecords!
                .FirstOrDefault(x => x.ApplicationId == linkedProposedFelling?.ApplicationId);

            var property = propertiesList
                .FirstOrDefault(x => x.Name.Equals(application?.Flov2PropertyName, StringComparison.InvariantCultureIgnoreCase));

            var proposedRestockingValidationResult = await new ProposedRestockingSourceValidator(linkedProposedFelling, property, _speciesCodes)
                .ValidateAsync(proposedRestocking, cancellationToken);
            ProcessValidationResult(results, proposedRestockingValidationResult, "Restocking");
        }

        if (results.Any())
        {
            _logger.LogWarning("{Count} validation failures detected in import file set", results.Count);
            return UnitResult.Failure(results);
        }

        return UnitResult.Success<List<ValidationFailure>>();
    }

    private void ProcessValidationResult(List<ValidationFailure> currentErrors, ValidationResult? validationResult, string source)
    {
        if (validationResult is { IsValid: false })
        {
            _logger.LogWarning("{Count} validation failures detected in source {Source}", validationResult.Errors.Count, source);
            currentErrors.AddRange(validationResult.Errors);
        }
    }
}