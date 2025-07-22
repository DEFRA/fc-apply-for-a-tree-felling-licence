namespace Forestry.Flo.Services.FileStorage.Configuration;

/// <summary>
/// Configuration used to represent the restrictions on submitted requests received the web api .
/// </summary>
public class ApiFileUploadOptions
{
    /// <summary>
    /// The maximum byte size permitted by the File Storage <see cref="FileStorage.Services.FileValidator"/>
    /// service when processing a file received from another system.
    /// </summary>
    public int MaxFileSizeBytes { get; set; } = 33554432; //32MB
}