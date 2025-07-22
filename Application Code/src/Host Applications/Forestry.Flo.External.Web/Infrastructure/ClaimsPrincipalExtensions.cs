using System.Security.Claims;

namespace Forestry.Flo.External.Web.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static bool IsLoggedIn(this ClaimsPrincipal principal)
        => principal.Identity is { IsAuthenticated: true };

    public static bool IsNotLoggedIn(this ClaimsPrincipal principal)
        => !IsLoggedIn(principal);
}