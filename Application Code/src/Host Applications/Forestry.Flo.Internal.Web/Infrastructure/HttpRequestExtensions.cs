namespace Forestry.Flo.Internal.Web.Infrastructure;

public static class HttpRequestExtensions
{
    /// <summary>
    /// Retrieves the raw body as a byte array from the Request.Body stream
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<byte[]> GetRawBodyBytesAsync(this HttpRequest request, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream(2048);
        await request.Body.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}