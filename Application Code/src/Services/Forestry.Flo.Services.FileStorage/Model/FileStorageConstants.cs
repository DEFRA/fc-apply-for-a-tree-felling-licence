namespace Forestry.Flo.Services.FileStorage.Model;

/// <summary>
/// Static class to hold any constants related to file storage.
/// </summary>
public static class FileStorageConstants
{
    /// <summary>
    /// A collection of file extensions that have multiple possible values that are all considered valid for the same
    /// file content. For example, an image/jpeg file may have a "JPG" file extension or a "JPEG" file extension. This
    /// is required as the FileSignatures library we use can only return one valid file extension value for a given
    /// file's content.
    /// </summary>
    public static Dictionary<string, List<string>> InterchangeableFileExtensions => new()
    {
        { "JPG", ["JPEG", "JPG"] },
        { "TIF", ["TIF", "TIFF"] }
    };
}