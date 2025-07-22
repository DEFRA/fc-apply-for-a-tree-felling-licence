using CSharpFunctionalExtensions;
using Forestry.Flo.Services.DataImport.Models;
using Microsoft.AspNetCore.Http;

namespace Forestry.Flo.Services.DataImport.Services;

/// <summary>
/// Defines the contract for a service that reads an input <see cref="FormFileCollection"/>
/// for the data import process.
/// </summary>
public interface IReadImportFileCollections
{
    /// <summary>
    /// Attempt to read an input <see cref="FormFileCollection"/> and transform it into
    /// a <see cref="ImportFileSetContents"/> instance for processing.
    /// </summary>
    /// <param name="files">A <see cref="FormFileCollection"/> containing a set of files
    /// containing data to be imported into FLOv2.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ImportFileSetContents"/> instance if the files could
    /// be read and parsed correctly, otherwise a list of errors.</returns>
    Task<Result<ImportFileSetContents, List<string>>> ReadInputFormFileCollectionAsync(FormFileCollection files, CancellationToken cancellationToken);
}