using System.Security.Claims;

namespace Forestry.Flo.Internal.Web.Extensions
{
    public static class ClaimsPrincipleExtensions
    {
        public static string? GetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType)
        {
            return claimsPrincipal
                .Claims
                .FirstOrDefault(x => x.Type == claimType)
                ?.Value;
        }
    }
}
