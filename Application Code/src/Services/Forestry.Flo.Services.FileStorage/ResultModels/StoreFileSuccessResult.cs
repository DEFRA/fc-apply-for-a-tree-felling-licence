using Forestry.Flo.Services.FileStorage.Services;

namespace Forestry.Flo.Services.FileStorage.ResultModels;

/// <summary>
/// Class representing the result of a successfully stored file in the configured <see cref="IFileStorageService"/>.
/// </summary>
public class StoreFileSuccessResult
{
    /// <summary>
    /// The number of bytes in the file saved.
    /// </summary>
    public long FileSize { get; }

    /// <summary>
    /// The location to the successfully stored file.
    /// </summary>
    public string? Location { get; }

    /// <summary>
    /// Creates a new instance of <see cref="StoreFileSuccessResult"/> with the stored <see cref="Location"/> of the file.
    /// </summary>
    /// <param name="location">The location of the stored file.</param>
    /// <param name="fileSize"></param>
    public StoreFileSuccessResult(string location, int fileSize)
    {
        FileSize = fileSize;
        Location = location;
    }

    protected StoreFileSuccessResult(){}
}