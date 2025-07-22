using Forestry.Flo.Services.FileStorage.Services;

namespace Forestry.Flo.Services.FileStorage.ResultModels;

/// <summary>
/// Enumeration representing the possible reasons for a file to not be successfully stored in the configured <see cref="IFileStorageService"/>.
/// </summary>
public enum StoreFileFailureResultReason
{
    Duplicate,
    Unknown,
    FailedValidation
}