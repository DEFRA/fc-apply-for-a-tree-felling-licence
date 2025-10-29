namespace Forestry.Flo.Services.Common.Infrastructure;

/// <summary>
/// Intercepts HTTP requests to ensure a User-Agent header is present.
/// </summary>
/// <remarks>
/// This is necessary to bypass One Login's Cloudfront restrictions which block requests without a User-Agent header.
/// </remarks>
public class UserAgentHandler : DelegatingHandler
{
    private const string UserAgentHeader = "FLOV2";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.UserAgent.Count == 0)
        {
            request.Headers.UserAgent.ParseAdd(UserAgentHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}