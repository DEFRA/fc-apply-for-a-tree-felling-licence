using Microsoft.Net.Http.Headers;

namespace Forestry.Flo.Tests.Common.Testing
{
    public static class CookieHelper
    {
        public static SetCookieHeaderValue? GetCookieFromResponse(string cookieNameStartsWith, HttpResponseMessage response)
        {
            SetCookieHeaderValue? cookie = null;
            if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> values))
            {
                cookie = SetCookieHeaderValue.ParseList(values.ToList()).SingleOrDefault(c =>
                    c.Name.StartsWith(cookieNameStartsWith, StringComparison.InvariantCultureIgnoreCase));
            }
            
            return cookie;
        }

        public static SetCookieHeaderValue? GetCookieFromRequest(string cookieNameStartsWith, HttpRequestMessage request)
        {
            SetCookieHeaderValue? cookie = null;
            if (request.Headers.TryGetValues("Cookie", out IEnumerable<string> values))
            {
                cookie = SetCookieHeaderValue.ParseList(values.ToList()).SingleOrDefault(c =>
                    c.Name.StartsWith(cookieNameStartsWith, StringComparison.InvariantCultureIgnoreCase));
            }

            return cookie;
        }
    }
}
