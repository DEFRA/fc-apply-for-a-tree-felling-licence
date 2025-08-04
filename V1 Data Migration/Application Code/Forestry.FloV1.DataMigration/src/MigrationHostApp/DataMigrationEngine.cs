using Ardalis.GuardClauses;
using CommandLine;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MigrationHostApp.CommandOptions;
using MigrationHostApp.MigrationModules;
using MigrationHostApp.Validation;
using MigrationService.Services;
using NodaTime;
using DataMigrationConfiguration = MigrationService.Configuration.DataMigrationConfiguration;

namespace MigrationHostApp;

public class DataMigrationEngine
{
    private readonly IClock _clock;
    private readonly DataMigrationConfiguration _configuration;
    private readonly ILogger<DataMigrationEngine> _logger;
    private readonly IServiceProvider _hostProvider;
    private readonly Parser _parser = new(with => { with.CaseInsensitiveEnumValues = true; });

    public DataMigrationEngine(
        IClock clock,
        IOptions<DataMigrationConfiguration> configuration,
        IServiceProvider hostProvider,
        ILogger<DataMigrationEngine> logger)
    {
        _clock = Guard.Against.Null(clock);
        _configuration = Guard.Against.Null(configuration.Value);
        _hostProvider = Guard.Against.Null(hostProvider);
        _logger = logger;
    }

    public async Task ExecuteAsync(
        string[] args, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entered.");
        _logger.LogInformation("Max degree of parallelism set to {maxDegreeOfParallelism}", _configuration.ParallelismSettings.MaxDegreeOfParallelism);
        _logger.LogInformation("v1 Db Connection: {v1dbConnection}", _configuration.DatabaseStorage.V1DefaultConnection);
        _logger.LogInformation("v2 Db Connection: {v2dbConnection}", _configuration.DatabaseStorage.V2DefaultConnection);
        _logger.LogInformation("AzBlobStorage Connection: {azBlobConnection}", _configuration.AzureBlobStorage.ConnectionString);
        _logger.LogInformation("AzBlobStorage Container: {AzBlobContainerName}", _configuration.AzureBlobStorage.ContainerName);
        _logger.LogInformation("Fixed DateTime = {executionDateTime}", _clock.GetCurrentInstant().ToDateTimeUtc());

        try
        {
            await _parser.ParseArguments<ResetFloV2Options, PrevalidationOptions, MigrationOptions>(args)
                .MapResult(
                    async (ResetFloV2Options opts) => await ResetFloV2Async(opts, cancellationToken),
                    async (PrevalidationOptions opts) => await ExecutePreValidateAsync(opts, cancellationToken),
                    async (MigrationOptions opts) => await ExecuteMigrationAsync(opts, cancellationToken),
//                    async (VerifyDbOptions opts) => await PostRunDbVerification(opts, cancellationToken),
                    HandleCommandLineArgsParseErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete the requested command");
            throw;
        }
    }

    private async Task<int> ResetFloV2Async(
        ResetFloV2Options options, 
        CancellationToken cancellationToken)
    {
        var service = _hostProvider.GetService<ResetFloV2Service>();
        _logger.LogInformation("Request to reset FLOv2 environment received");

        var dbClearDownResult = await service!.ResetDatabaseAsync(cancellationToken);
        //todo clear/delete AZ blob storage files uploaded, if poss.

        if (dbClearDownResult.IsSuccess)
        {
            _logger.LogInformation("FLOv2 environment clear down complete");
            return Constants.SuccessExitCode;
        }
        
        _logger.LogWarning("Could not reset FLOv2 environment");
        return Constants.FailureExitCode;
    }

    private async Task<int> ExecutePreValidateAsync(
        PrevalidationOptions options,
        CancellationToken cancellationToken)
    {
        var handler = GetMigrationHandler(options.SourceDataType);

        var result = await handler.PreValidateAsync(cancellationToken);

        _logger.LogInformation("Executing pre-validation process for migration of type {handlerType}", handler.ToString());

        return HandleValidationResult(result, options);
    }

    private int HandleValidationResult(
        Result<PreValidationResultSuccess, PreValidationResultError> result, 
        PrevalidationOptions options)
    {
        if (result.IsSuccess)
        {
            _logger.LogInformation("No validation issues were found");
            _logger.LogInformation("Successfully completed request to validate source data for {sourceDataType}",
                options.SourceDataType);

            return Constants.SuccessExitCode;
        }

        foreach (var errorValidationResultFailure in result.Error.ValidationResultFailures)
        {
            if (errorValidationResultFailure.IsItemIssue)
            {
                _logger.LogWarning("EntityId {rowId}, Field {fieldName}, Issue {issue}, Actual value {value}, Message {message}",
                    errorValidationResultFailure.RowId, 
                    errorValidationResultFailure.FieldName,
                    errorValidationResultFailure.ItemValidationIssue,
                    errorValidationResultFailure.ActualValue,
                    errorValidationResultFailure.Message);
            }
            else
            {
                _logger.LogWarning("Validation result: {message}", errorValidationResultFailure.Message);
            }
        }

        _logger.LogWarning("Validation failed for {SourceDataType}", options.SourceDataType);
        _logger.LogWarning("Found {count} validation issues", result.Error.ValidationResultFailures.Count);

        if (!string.IsNullOrEmpty(result.Error.ExceptionMessage))
        {
            _logger.LogError("Exception found during validation {errorMessage}", result.Error.ExceptionMessage);
        }
        return Constants.FailureExitCode;
    }

    private async Task<int> ExecuteMigrationAsync(
        MigrationOptions migrationOptions,
        CancellationToken cancellationToken)
    {
        var handler = GetMigrationHandler(migrationOptions.SourceDataType);
        _logger.LogInformation("Processing migration for type {handlerType}", handler.ToString());
        _logger.LogInformation("Test Mode set to {testMode}", migrationOptions.EnableTestMode);
        _logger.LogInformation("Disable PreValidation set to {disablePreValidation}", migrationOptions.DisablePreValidation);

        if (!string.IsNullOrEmpty(migrationOptions.EmailAddressToUse))
        {
            _logger.LogInformation("Using specified email address of {emailAddress} to construct FLOv2 UserNames",
                migrationOptions.EmailAddressToUse);
        }

        var canExecuteResult = await handler.CanBeExecutedAsync(cancellationToken);

        if (canExecuteResult.IsSuccess)
        {
            _logger.LogInformation("Migration process can proceed, FLOv2 is in the correct state for this migration module");

            if (migrationOptions.DisablePreValidation)
            {
                _logger.LogInformation("Not performing pre-validation checks, as per command request.");
            }
            else
            {
                var outcome = await ExecutePreValidateAsync(new PrevalidationOptions(), cancellationToken);
                HandleDefaultPreValidationStepOutcome(outcome);
            }

            var result = await handler.MigrateAsync(migrationOptions, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Success");
                return Constants.SuccessExitCode;
            }

            _logger.LogError("Import did not run successfully, error was {errors}", result.Error);
        }
        else
        {
            _logger.LogCritical("Migration process cannot proceed: {canExecuteResult}, FLOv2 is NOT in the correct state for this migration module", canExecuteResult.Error);
        }

        return Constants.FailureExitCode;
    }
    
    private Task<int> HandleCommandLineArgsParseErrors(
        IEnumerable<Error> errs)
    {
        _logger.LogWarning("Failed to parse the supplied command line parameters");

        foreach (var error in errs)
        {
            _logger.LogWarning("Error {error}", error.ToString());
        }
        _logger.LogInformation("Exiting early");
        return Task.FromResult(Constants.FailureExitCode);
    }

    private MigratorBase GetMigrationHandler(SourceDataType sourceType)
    {
        return sourceType switch
        {
            SourceDataType.ExternalUsers 
                => _hostProvider.GetRequiredService<ExternalUsersMigrator>(),
            SourceDataType.ManagedOwners
                => _hostProvider.GetRequiredService<ManagedOwnersMigrator>(),
            //todo etc
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType))
        };
    }

    private void HandleDefaultPreValidationStepOutcome(int stepOutcome)
    {
        if (stepOutcome == Constants.SuccessExitCode) 
            return;
        
        Console.WriteLine("");
        Console.WriteLine("Validation issues were found, do you want to continue with the migration?");
        Console.WriteLine("Press 'Y' to continue, or any other key to exit now.");

        var x = Console.ReadKey();
        if (x.Key != ConsoleKey.Y)
        {
            _logger.LogInformation("Process exited at user's request.");
            Environment.Exit(Constants.FailureExitCode);
        }
        else
        {
            _logger.LogInformation("Process is continuing at user's request.");
        }
    }
}