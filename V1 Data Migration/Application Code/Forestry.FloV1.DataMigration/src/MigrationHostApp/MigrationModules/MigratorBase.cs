using CSharpFunctionalExtensions;
using MigrationHostApp.CommandOptions;
using MigrationHostApp.Validation;

namespace MigrationHostApp.MigrationModules;

/// <summary>
/// This is an abstract base class that defines a common interface for all modules which
/// need to perform migration work, whether this is pre-checks, post checks, reporting or the actual
/// data migration itself.
/// </summary>
public abstract class MigratorBase : PreValidatorBase
{
    /// <summary>
    /// Checks on the FLOv2 system/db to ensure the requested module execution
    /// is in a state where the module is safe to be ran.
    /// </summary>
    /// <remarks>For example, Users have been cleared down before running the user import,
    /// by checking key table counts to ensure correct state, or that dependent modules
    /// were ran previously (i.e. user module before Woodland owners import module).</remarks>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public abstract Task<Result> CanBeExecutedAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Execute all required steps to pre-validate the incoming data before loaded
    /// </summary>
    /// <remarks>For example,
    /// <list type="bullet">
    /// <item>check a v1 data-set to make sure fields we know that</item>
    /// <item>check the distinct set of source enums in a data-set are convertable to v2 equivalents
    /// ahead of time.</item>
    /// </list></remarks>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result object, success denoting the passing of validation checks, otherwise a <see cref="PreValidationResultError"/> object</returns>
    public abstract Task<Result<PreValidationResultSuccess, PreValidationResultError>> PreValidateAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Perform the actual source data extraction, necessary transformation and load
    /// into target environment.
    /// </summary>
    /// <param name="migrationOptions">Migration options which may be required</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public abstract Task<Result> MigrateAsync(
        MigrationOptions migrationOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Execute necessary checks to detect any issues/inconsistencies post run,
    /// between expected and actual results. 
    /// </summary>
    /// <remarks>
    /// For example,
    /// <list type="bullet">
    /// <item>comparing FLOv2 DB table row counts, key columns data with that held in v1).</item>
    /// <item>comparing byte size of files in AZ, and/or meta data such mime-types</item>
    /// </list>
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public abstract Task<Result> VerifyAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Report final summary of the derived module's migration, this acts as supporting
    /// evidence/assurance for customer. 
    /// </summary>
    /// <remarks>
    /// Should include module timings, high level reporting, such as 1000/1000 (100%) of users imported successfully.</remarks>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public abstract Task<Result> SummariseAsync(
        CancellationToken cancellationToken);
}
