
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.DataImport.Models;

namespace Forestry.Flo.Services.DataImport.Services;

/// <summary>
/// Defines the contract for a service that imports data into the FLOv2 system.
/// </summary>
public interface IImportData
{
    /// <summary>
    /// Performs the import of data provided in a populated <see cref="DataImportRequest"/> instance.
    /// </summary>
    /// <param name="request">The <see cref="DataImportRequest"/> instance providing the data to import.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation with a list of imported application ids/references
    /// or error/validation error messages in a failure scenario.</returns>
    Task<Result<Dictionary<Guid, string>, List<string>>> ImportDataAsync(DataImportRequest request, CancellationToken cancellationToken);
}