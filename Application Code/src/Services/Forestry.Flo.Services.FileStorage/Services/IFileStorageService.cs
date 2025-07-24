using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;

namespace Forestry.Flo.Services.FileStorage.Services;

/// <summary>
/// Defines the contract for a service that implements the management of a stored of a file.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Stores a file to configured storage
    /// </summary>
    /// <param name="originalFileName">The original name of the file.</param>
    /// <param name="fileBytes">The byte array representation of the file to be stored.</param>
    /// <param name="storeLocation">The location to store the file, e.g. a location of disk.</param>
    /// <param name="receivedByApi">Whether the file being stored was received via external system call to the FLO Api</param>
    /// <param name="reason">The intended reason for storing the file.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<StoreFileSuccessResult, StoreFileFailureResult>> StoreFileAsync(
        string originalFileName,
        byte[] fileBytes,
        string storeLocation,
        bool receivedByApi,
        FileUploadReason reason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Remove a file from configured storage.
    /// </summary>
    /// <param name="fileLocation">The location of the file to remove.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<UnitResult<FileAccessFailureReason>> RemoveFileAsync(
        string fileLocation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a file from configured storage
    /// </summary>
    /// <param name="fileLocation">The file location</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<GetFileSuccessResult, FileAccessFailureReason>> GetFileAsync(
        string fileLocation,
        CancellationToken cancellationToken);

}