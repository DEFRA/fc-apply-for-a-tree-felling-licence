using Forestry.Flo.Services.FileStorage.Services;

namespace Forestry.Flo.Services.FileStorage.ResultModels;

/// <summary>
/// Class representing the result of a successfully retrieved file in the configured <see cref="IFileStorageService"/>.
/// </summary>
public class GetFileSuccessResult
{
    /// <summary>
    /// The number of bytes in the file saved.
    /// </summary>
    public byte[] FileBytes { get; }
    
    /// <summary>
    /// Creates a new instance of <see cref="GetFileSuccessResult"/> with the stored <see cref="FileBytes"/> of the file.
    /// </summary>
    /// <param name="bytes">The bytes for this file retrieved.</param>
    public GetFileSuccessResult(byte[] bytes)
    {
        FileBytes = bytes;
    }

    protected GetFileSuccessResult(){}
}
