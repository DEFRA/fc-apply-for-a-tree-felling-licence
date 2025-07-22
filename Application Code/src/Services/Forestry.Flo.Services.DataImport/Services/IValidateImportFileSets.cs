using CSharpFunctionalExtensions;
using FluentValidation.Results;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Services.PropertyProfiles.DataImports;

namespace Forestry.Flo.Services.DataImport.Services;

/// <summary>
/// Defines the contract for a service that validates <see cref="ImportFileSetContents"/> import
/// file sets.
/// </summary>
public interface IValidateImportFileSets
{
    /// <summary>
    /// Run the validation on the given import file set and return the validation results.
    /// </summary>
    /// <param name="woodlandOwnerId">The Id of the Woodland Owner that the data import is for.</param>
    /// <param name="properties">The set of properties for this woodland owner.</param>
    /// <param name="importFileSet">The populated <see cref="ImportFileSetContents"/> instance to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="UnitResult"/> with a list of validation errors if the validation has failed.</returns>
    Task<UnitResult<List<ValidationFailure>>> ValidateImportFileSetAsync(
        Guid woodlandOwnerId,
        IEnumerable<PropertyIds> properties,
        ImportFileSetContents importFileSet,
        CancellationToken cancellationToken);
}