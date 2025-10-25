using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using FileSignatures;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.FileStorage.Services;

/// <summary>
/// Class to validate a file against configured options and basic assertions.
/// </summary>
public class FileValidator
{
    private readonly IFileFormatInspector _fileFormatInspector;
    private readonly UserFileUploadOptions _userFileUploadOptions;
    private readonly ApiFileUploadOptions _apiFileUploadOptions;

    /// <summary>
    /// Valid a file against various assertions and site specific configured options in <see cref="UserFileUploadOptions"/>.
    /// </summary>
    /// <param name="fileFormatInspector"></param>
    /// <param name="userFileUploadOptions"></param>
    /// <param name="apiFileUploadOptions"></param>
    public FileValidator(
        IFileFormatInspector fileFormatInspector,
        IOptions<UserFileUploadOptions> userFileUploadOptions,
        IOptions<ApiFileUploadOptions> apiFileUploadOptions)
    {
        _fileFormatInspector = Guard.Against.Null(fileFormatInspector);
        _userFileUploadOptions = Guard.Against.Null(userFileUploadOptions.Value);
        _apiFileUploadOptions = Guard.Against.Null(apiFileUploadOptions.Value);
    }

    /// <summary>
    /// Performs a series of validation checks against an <see cref="IFormFile"/> submitted during a user request to upload a file.
    /// </summary>
    /// <returns></returns>
    public Result<bool, FileInvalidReason> Validate(
        byte[] fileBytes,
        string fileName,
        bool receivedByApi,
        FileUploadReason reason)
    {
        if (fileBytes.Length == 0)
        {
            return CreateInvalidFileResult(FileInvalidReason.EmptyFile);
        }

        if (!receivedByApi)
        {
            var maxFileSize = reason is FileUploadReason.WmpDocument
                ? _userFileUploadOptions.ServerMaxUploadSizeBytes
                : _userFileUploadOptions.MaxFileSizeBytes;

            if (fileBytes.Length > maxFileSize)
            {
                return CreateInvalidFileResult(FileInvalidReason.FileTooLarge);
            }
        }
        else
        {
            if (fileBytes.Length > _apiFileUploadOptions.MaxFileSizeBytes)
            {
                return CreateInvalidFileResult(FileInvalidReason.FileTooLarge);
            }
        }

        var permittedFileExtensions = GetPermittedFileExtensions(reason);

        if (TryGetFileExtension(fileName, out var extension))
        {
            if (!HasValidFileExtension(extension, permittedFileExtensions))
            {
                return CreateInvalidFileResult(FileInvalidReason.ExtensionNotSupported);
            }

            //check file signature
            using var stream = new MemoryStream();
            stream.Write(fileBytes, 0, fileBytes.Length);
            var format = _fileFormatInspector.DetermineFileFormat(stream);

            if (format is not null && !string.Equals(format.Extension, extension, StringComparison.CurrentCultureIgnoreCase))
            {
                //actual file content does not match it's extension.
                return CreateInvalidFileResult(FileInvalidReason.FileSignatureDoesNotMatchSuppliedFileExtension);
            }
        }
        return CreateValidFileResult();
    }

    private bool TryGetFileExtension(string fileName, out string extension)
    {
        extension = string.Empty;

        var filePartsArray = fileName.Split('.');

        if (filePartsArray.Length <= 0) return false;

        extension = filePartsArray[^1];
        return true;
    }

    private static bool HasValidFileExtension(string fileExtension, IEnumerable<string> permittedExtensions)
    {
        return !string.IsNullOrEmpty(fileExtension)
               && permittedExtensions.Contains(fileExtension, StringComparer.CurrentCultureIgnoreCase);
    }

    private static Result<bool, FileInvalidReason> CreateInvalidFileResult(FileInvalidReason reason)
    {
        return Result.Failure<bool, FileInvalidReason>(reason);
    }

    private static Result<bool, FileInvalidReason> CreateValidFileResult()
    {
        return Result.Success<bool, FileInvalidReason>(true);
    }

    private IEnumerable<string> GetPermittedFileExtensions(FileUploadReason reason)
    {
        var allowedExtensions = new List<string>();

        foreach (var allowedFileType in 
                 _userFileUploadOptions.AllowedFileTypes.Where(x => x.FileUploadReasons.Contains(reason)))
        {
            allowedExtensions.AddRange(allowedFileType.Extensions);
        }
        return allowedExtensions;
    }
}