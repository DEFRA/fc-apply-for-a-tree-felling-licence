using Forestry.Flo.Services.FileStorage.Services;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FileStorage.ResultModels
{
    /// <summary>
    /// Enumeration representing the possible reasons for a file to be invalid when validated with the <see cref="FileValidator"/>.
    /// </summary>
    public enum FileInvalidReason
    {
        [Display(Name="File is empty")]
        EmptyFile,

        [Display(Name = "File type is not supported")]
        ExtensionNotSupported,

        [Display(Name = "File signature does not match the supplied file extension")]
        FileSignatureDoesNotMatchSuppliedFileExtension,

        [Display(Name = "File exceeds the maximum allowed size")]
        FileTooLarge,

        [Display(Name = "File failed the virus scan")]
        FailedVirusScan,

        [Display(Name = "An internal error occurred during file validation")]
        InternalError
    }
}