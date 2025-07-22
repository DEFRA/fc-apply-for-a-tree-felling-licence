using System.Net;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

public static class FormFileExtensions
{
    /// <summary>
    /// HTML Encoding of the filename of the <see cref="FormFile"/> to make safe for use.
    /// </summary>
    /// <param name="formFile"></param>
    /// <returns></returns>
    public static string GetTrustedFileName(this IFormFile formFile)
    {
        // Don't trust the file name sent by the client.
        var trustedFileNameForDisplay = WebUtility.HtmlEncode(formFile.FileName);
        return trustedFileNameForDisplay;
    }

    /// <summary>
    /// Returns the byte array representation of the <see cref="IFormFile"/> content.
    /// </summary>
    /// <param name="formFile"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<byte[]> ReadFormFileBytesAsync(this IFormFile formFile, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Returns the byte array representation of the <see cref="IFormFile"/> content.
    /// </summary>
    /// <param name="formFile"></param>
    /// <returns></returns>
    public static byte[] ReadFormFileBytes(this IFormFile formFile)
    {
        using var memoryStream = new MemoryStream();
        formFile.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}