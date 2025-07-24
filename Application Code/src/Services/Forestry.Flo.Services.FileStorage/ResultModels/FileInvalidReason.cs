using Forestry.Flo.Services.FileStorage.Services;

namespace Forestry.Flo.Services.FileStorage.ResultModels
{
    /// <summary>
    /// Enumeration representing the possible reasons for a file to be invalid when validated with the <see cref="FileValidator"/>.
    /// </summary>
    public enum FileInvalidReason
   {
       EmptyFile,
       ExtensionNotSupported,
       FileSignatureDoesNotMatchSuppliedFileExtension,
       FileTooLarge,
       FailedVirusScan,
       InternalError
   }
}