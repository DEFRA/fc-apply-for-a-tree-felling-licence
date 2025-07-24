namespace Forestry.Flo.Services.FileStorage.ResultModels;

/// <summary>
/// Enumeration representing the possible reasons for a file to not be successfully accessed,
/// for an operation such as removal or get.
/// </summary>
public enum FileAccessFailureReason
{
    NotFound,
    Unknown
}
